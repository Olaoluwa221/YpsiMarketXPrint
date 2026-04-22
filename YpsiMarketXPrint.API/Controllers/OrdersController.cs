using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Stripe;
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
        [EnableRateLimiting("auth-standard")]
        public async Task<IActionResult> Checkout([FromBody] GuestCheckoutDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.PaymentIntentId))
                return BadRequest("Payment intent is required.");

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

                if (cart == null || !cart.CartItems.Any())
                    return BadRequest("Your cart is empty.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.GuestEmail))
                    return BadRequest("Email is required for guest checkout.");
                guestEmail = dto.GuestEmail;

                if (dto.CartItems == null || !dto.CartItems.Any())
                    return BadRequest("Your cart is empty.");
            }

            var deliveryMethod = Enum.TryParse<DeliveryMethod>(dto.DeliveryMethod, true, out var dm)
                ? dm
                : DeliveryMethod.Shipping;
            var shippingCost = deliveryMethod == DeliveryMethod.Shipping ? 8.00m : 0m;

            if (deliveryMethod == DeliveryMethod.Shipping)
            {
                if (string.IsNullOrWhiteSpace(dto.FirstName) ||
                    string.IsNullOrWhiteSpace(dto.LastName) ||
                    string.IsNullOrWhiteSpace(dto.Address) ||
                    string.IsNullOrWhiteSpace(dto.City) ||
                    string.IsNullOrWhiteSpace(dto.State) ||
                    string.IsNullOrWhiteSpace(dto.Zip))
                {
                    return BadRequest("Shipping address is required.");
                }
            }

            // ---- Compute line items + server-side total BEFORE trusting the client ----
            List<(int VariantId, int Quantity, decimal UnitPrice)> lineItems;

            if (userId != null && cart != null)
            {
                lineItems = cart.CartItems
                    .Select(ci => (ci.VariantId, ci.Quantity, ci.Variant.Price))
                    .ToList();
            }
            else
            {
                var variantIds = dto.CartItems!.Select(ci => ci.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .Where(v => variantIds.Contains(v.VariantId))
                    .ToDictionaryAsync(v => v.VariantId);

                var built = new List<(int, int, decimal)>();
                foreach (var ci in dto.CartItems)
                {
                    if (ci.Quantity <= 0 || ci.Quantity > 1000)
                        return BadRequest("Invalid item quantity.");
                    if (!variants.TryGetValue(ci.VariantId, out var v))
                        return BadRequest("Invalid variant in cart.");
                    built.Add((ci.VariantId, ci.Quantity, v.Price));
                }
                lineItems = built;
            }

            var expectedSubtotal = lineItems.Sum(li => li.UnitPrice * li.Quantity);
            var expectedTotalCents = (long)((expectedSubtotal + shippingCost) * 100);

            // ---- Verify payment with Stripe ----
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            PaymentIntent intent;
            try
            {
                intent = await new PaymentIntentService().GetAsync(dto.PaymentIntentId);
            }
            catch (StripeException)
            {
                return BadRequest("Payment could not be verified.");
            }

            if (intent.Status != "succeeded")
                return BadRequest("Payment has not been completed.");

            if (intent.Currency != "usd" || intent.Amount != expectedTotalCents)
                return BadRequest("Payment amount does not match order total.");

            // Prevent replay: one intent, one order.
            var alreadyClaimed = await _context.Orders
                .AnyAsync(o => o.PaymentIntentId == dto.PaymentIntentId);
            if (alreadyClaimed)
                return BadRequest("This payment has already been used for another order.");

            // ---- Payment verified. Now create the order. ----
            var isShipping = deliveryMethod == DeliveryMethod.Shipping;
            var order = new Order
            {
                UserId = userId,
                GuestEmail = guestEmail,
                PaymentIntentId = dto.PaymentIntentId,
                DateOrdered = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                DeliveryMethod = deliveryMethod,
                ShippingCost = shippingCost,

                ContactFirstName = dto.FirstName,
                ContactLastName = dto.LastName,
                ContactPhone = dto.Phone,
                ShippingAddress = isShipping ? dto.Address : null,
                ShippingCity = isShipping ? dto.City : null,
                ShippingState = isShipping ? dto.State : null,
                ShippingZip = isShipping ? dto.Zip : null,
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderItems = lineItems.Select(li => new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = li.VariantId,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
            }).ToList();
            _context.OrderItems.AddRange(orderItems);

            if (userId != null && cart != null)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            try
            {
                var emailTo = guestEmail ?? (await _context.Users.FindAsync(userId))?.Email;
                if (_emailService != null && emailTo != null)
                {
                    var total = expectedSubtotal + shippingCost;
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
                    ContactFirstName = o.ContactFirstName,
                    ContactLastName = o.ContactLastName,
                    ContactPhone = o.ContactPhone,
                    ContactEmail = o.User != null ? o.User.Email : o.GuestEmail,
                    ShippingAddress = o.ShippingAddress,
                    ShippingCity = o.ShippingCity,
                    ShippingState = o.ShippingState,
                    ShippingZip = o.ShippingZip,
                    Items = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        VariantId = oi.VariantId,
                        ProductId = oi.Variant.ProductId,
                        ProductName = oi.Variant.Product.ProductName,
                        Size = oi.Variant.Size,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        ArtworkId = oi.ArtworkId,
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
                .Include(o => o.User)
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
                ContactFirstName = order.ContactFirstName,
                ContactLastName = order.ContactLastName,
                ContactPhone = order.ContactPhone,
                ContactEmail = order.User?.Email ?? order.GuestEmail,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingZip = order.ShippingZip,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    VariantId = oi.VariantId,
                    ProductId = oi.Variant.ProductId,
                    ProductName = oi.Variant.Product.ProductName,
                    Size = oi.Variant.Size,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ArtworkId = oi.ArtworkId,
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
                    ContactFirstName = o.ContactFirstName,
                    ContactLastName = o.ContactLastName,
                    ContactPhone = o.ContactPhone,
                    ContactEmail = o.User != null ? o.User.Email : o.GuestEmail,
                    ShippingAddress = o.ShippingAddress,
                    ShippingCity = o.ShippingCity,
                    ShippingState = o.ShippingState,
                    ShippingZip = o.ShippingZip,
                    Items = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        VariantId = oi.VariantId,
                        ProductId = oi.Variant.ProductId,
                        ProductName = oi.Variant.Product.ProductName,
                        Size = oi.Variant.Size,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        ArtworkId = oi.ArtworkId,
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