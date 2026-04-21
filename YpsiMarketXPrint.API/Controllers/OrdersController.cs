using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.DTOs;
using YpsiMarketXPrint.API.Models;
using YpsiMarketXPrint.API.Services;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService? _emailService;
        private readonly IConfiguration _config;

        public OrdersController(AppDbContext context, IConfiguration config, EmailService? emailService = null)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string FrontendBaseUrl => _config["Frontend:BaseUrl"] ?? "http://localhost:5173";

        // POST api/orders/checkout
        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromBody] GuestCheckoutDto dto)
        {
            int? userId = null;
            string? guestEmail = null;
            Cart? cart = null;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                userId = int.Parse(userIdClaim);
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Variant)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto?.GuestEmail))
                    return BadRequest("Email is required for guest checkout.");
                guestEmail = dto.GuestEmail;

                if (dto.CartItems == null || !dto.CartItems.Any())
                    return BadRequest("Your cart is empty.");
            }

            if (userId != null && (cart == null || !cart.CartItems.Any()))
                return BadRequest("Your cart is empty.");

            var deliveryMethod = Enum.TryParse<DeliveryMethod>(dto?.DeliveryMethod, true, out var dm)
                ? dm
                : DeliveryMethod.Shipping;

            var order = new Order
            {
                UserId = userId,
                GuestEmail = guestEmail,
                DateOrdered = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                DeliveryMethod = deliveryMethod,
                ShippingCost = deliveryMethod == DeliveryMethod.Shipping ? 8.00m : 0m,
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            List<OrderItem> orderItems;

            if (userId != null && cart != null)
            {
                orderItems = cart.CartItems.Select(ci => new OrderItem
                {
                    OrderId = order.OrderId,
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Variant.Price,
                }).ToList();

                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var variantIds = dto!.CartItems!.Select(ci => ci.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .Where(v => variantIds.Contains(v.VariantId))
                    .ToListAsync();

                orderItems = dto.CartItems.Select(ci =>
                {
                    var variant = variants.First(v => v.VariantId == ci.VariantId);
                    return new OrderItem
                    {
                        OrderId = order.OrderId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        UnitPrice = variant.Price,
                    };
                }).ToList();
            }

            _context.OrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();

            try
            {
                var emailTo = guestEmail ?? (await _context.Users.FindAsync(userId))?.Email;
                if (_emailService != null && emailTo != null)
                {
                    var total = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity) + order.ShippingCost;
                    await _emailService.SendOrderConfirmationAsync(emailTo, order.OrderId, total);

                    await GenerateAndSendArtworkTokensAsync(order.OrderId, emailTo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(new { message = "Order placed successfully.", orderId = order.OrderId });
        }

        // Creates upload tokens for every order item whose product requires artwork,
        // then sends one email listing all the links.
        private async Task GenerateAndSendArtworkTokensAsync(int orderId, string emailTo)
        {
            var itemsNeedingArtwork = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId && oi.Variant.Product.RequiresArtwork)
                .Include(oi => oi.Variant).ThenInclude(v => v.Product)
                .ToListAsync();

            if (itemsNeedingArtwork.Count == 0) return;

            // uses FrontendBaseUrl property
            var emailItems = new List<(string, string, string)>();

            foreach (var oi in itemsNeedingArtwork)
            {
                var token = new ArtworkUploadToken
                {
                    Token = Guid.NewGuid().ToString("N"),
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.ArtworkUploadTokens.Add(token);

                emailItems.Add((
                    oi.Variant.Product.ProductName,
                    oi.Variant.Size,
                    $"{FrontendBaseUrl}/upload-artwork/{token.Token}"
                ));
            }

            await _context.SaveChangesAsync();

            if (_emailService != null)
                await _emailService.SendArtworkUploadRequestAsync(emailTo, orderId, emailItems);
        }

        // Invalidates any unused tokens and issues fresh ones for items that still need artwork.
        // Used when an admin reverts an order from a "done" status back to an active one,
        // and by the admin "regenerate link" endpoint.
        private async Task RegenerateMissingArtworkTokensAsync(int orderId, string emailTo)
        {
            var itemsNeedingArtwork = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId
                             && oi.Variant.Product.RequiresArtwork
                             && oi.ArtworkId == null)
                .Include(oi => oi.Variant).ThenInclude(v => v.Product)
                .ToListAsync();

            if (itemsNeedingArtwork.Count == 0) return;

            var variantIds = itemsNeedingArtwork.Select(oi => oi.VariantId).ToList();

            var existingUnused = await _context.ArtworkUploadTokens
                .Where(t => t.OrderId == orderId
                            && variantIds.Contains(t.VariantId)
                            && t.UsedAt == null
                            && t.InvalidatedAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var t in existingUnused) t.InvalidatedAt = now;

            // uses FrontendBaseUrl property
            var emailItems = new List<(string, string, string)>();

            foreach (var oi in itemsNeedingArtwork)
            {
                var token = new ArtworkUploadToken
                {
                    Token = Guid.NewGuid().ToString("N"),
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    CreatedAt = now,
                };
                _context.ArtworkUploadTokens.Add(token);

                emailItems.Add((
                    oi.Variant.Product.ProductName,
                    oi.Variant.Size,
                    $"{FrontendBaseUrl}/upload-artwork/{token.Token}"
                ));
            }

            await _context.SaveChangesAsync();

            if (_emailService != null)
                await _emailService.SendArtworkUploadRequestAsync(emailTo, orderId, emailItems);
        }

        // GET api/orders
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(o => o.DateOrdered)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    DateOrdered = o.DateOrdered,
                    OrderStatus = o.OrderStatus.ToString().ToLower(),
                    DeliveryMethod = o.DeliveryMethod.ToString().ToLower(),
                    ShippingCost = o.ShippingCost,
                    Items = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        VariantId = oi.VariantId,
                        ProductId = oi.Variant.ProductId,
                        ProductName = oi.Variant.Product.ProductName,
                        Size = oi.Variant.Size,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        ArtworkId = oi.ArtworkId,
                        ArtworkUrl = oi.Artwork != null ? oi.Artwork.Link : null,
                        RequiresArtwork = oi.Variant.Product.RequiresArtwork,
                    }).ToList(),
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET api/orders/1
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Artwork)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            if (order.UserId != userId && !User.IsInRole("admin"))
                return Forbid();

            var dto = new OrderDto
            {
                OrderId = order.OrderId,
                DateOrdered = order.DateOrdered,
                OrderStatus = order.OrderStatus.ToString().ToLower(),
                DeliveryMethod = order.DeliveryMethod.ToString().ToLower(),
                ShippingCost = order.ShippingCost,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    VariantId = oi.VariantId,
                    ProductId = oi.Variant.ProductId,
                    ProductName = oi.Variant.Product.ProductName,
                    Size = oi.Variant.Size,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ArtworkId = oi.ArtworkId,
                    ArtworkUrl = oi.Artwork != null ? oi.Artwork.Link : null,
                    RequiresArtwork = oi.Variant.Product.RequiresArtwork,
                }).ToList(),
            };

            return Ok(dto);
        }

        // GET api/orders/all - admin only
        [HttpGet("all")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(o => o.DateOrdered)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    DateOrdered = o.DateOrdered,
                    OrderStatus = o.OrderStatus.ToString().ToLower(),
                    DeliveryMethod = o.DeliveryMethod.ToString().ToLower(),
                    ShippingCost = o.ShippingCost,
                    Items = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        VariantId = oi.VariantId,
                        ProductId = oi.Variant.ProductId,
                        ProductName = oi.Variant.Product.ProductName,
                        Size = oi.Variant.Size,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        ArtworkId = oi.ArtworkId,
                        ArtworkUrl = oi.Artwork != null ? oi.Artwork.Link : null,
                        RequiresArtwork = oi.Variant.Product.RequiresArtwork,
                    }).ToList(),
                })
                .ToListAsync();

            return Ok(orders);
        }

        // PUT api/orders/1/status - admin only
        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            if (!Enum.TryParse<OrderStatus>(dto.OrderStatus, true, out var newStatus))
                return BadRequest("Invalid order status.");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            var oldStatus = order.OrderStatus;
            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

            // If reverting from a "done" status back to an active one, regenerate artwork tokens
            // for any items that still need artwork uploaded.
            var doneStatuses = new[] { OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.PickedUp, OrderStatus.Cancelled };
            if (doneStatuses.Contains(oldStatus) && !doneStatuses.Contains(newStatus))
            {
                var emailToRecipient = order.UserId.HasValue
                    ? (await _context.Users.FindAsync(order.UserId))?.Email
                    : order.GuestEmail;

                if (emailToRecipient != null)
                {
                    try
                    {
                        await RegenerateMissingArtworkTokensAsync(order.OrderId, emailToRecipient);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Artwork token regeneration failed: {ex.Message}");
                    }
                }
            }

            try
            {
                var emailTo = order.UserId.HasValue
                    ? (await _context.Users.FindAsync(order.UserId))?.Email
                    : order.GuestEmail;

                if (_emailService != null && emailTo != null)
                {
                    var subtotal = await _context.OrderItems
                        .Where(oi => oi.OrderId == order.OrderId)
                        .SumAsync(oi => oi.UnitPrice * oi.Quantity);
                    var total = subtotal + order.ShippingCost;

                    await _emailService.SendOrderStatusUpdateAsync(
                        emailTo,
                        order.OrderId,
                        order.OrderStatus.ToString().ToLower(),
                        total
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(new
            {
                message = "Order status updated.",
                orderId = order.OrderId,
                status = order.OrderStatus.ToString().ToLower(),
            });
        }
        // POST api/orders/{id}/regenerate-artwork-token/{variantId} - admin only
        [HttpPost("{id}/regenerate-artwork-token/{variantId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RegenerateArtworkToken(int id, int variantId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Variant).ThenInclude(v => v.Product)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderId == id && oi.VariantId == variantId);

            if (orderItem == null)
                return NotFound("Order item not found.");

            if (!orderItem.Variant.Product.RequiresArtwork)
                return BadRequest("This item does not require artwork.");

            var emailTo = orderItem.Order.UserId.HasValue
                ? (await _context.Users.FindAsync(orderItem.Order.UserId))?.Email
                : orderItem.Order.GuestEmail;

            if (emailTo == null)
                return BadRequest("No email address on file for this order.");

            var now = DateTime.UtcNow;

            var existingUnused = await _context.ArtworkUploadTokens
                .Where(t => t.OrderId == id
                            && t.VariantId == variantId
                            && t.UsedAt == null
                            && t.InvalidatedAt == null)
                .ToListAsync();
            foreach (var t in existingUnused) t.InvalidatedAt = now;

            var newToken = new ArtworkUploadToken
            {
                Token = Guid.NewGuid().ToString("N"),
                OrderId = id,
                VariantId = variantId,
                CreatedAt = now,
            };
            _context.ArtworkUploadTokens.Add(newToken);
            await _context.SaveChangesAsync();

            // uses FrontendBaseUrl property
            try
            {
                if (_emailService != null)
                {
                    await _emailService.SendArtworkUploadRequestAsync(
                        emailTo,
                        id,
                        new List<(string, string, string)>
                        {
                            (
                                orderItem.Variant.Product.ProductName,
                                orderItem.Variant.Size,
                                $"{FrontendBaseUrl}/upload-artwork/{newToken.Token}"
                            )
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(new { message = "New artwork upload link emailed.", token = newToken.Token });
        }
    }
}