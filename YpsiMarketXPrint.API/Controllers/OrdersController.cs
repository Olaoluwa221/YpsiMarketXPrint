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

        public OrdersController(AppDbContext context, EmailService? emailService = null)
        {
            _context = context;
            _emailService = emailService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(new { message = "Order placed successfully.", orderId = order.OrderId });
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
                        ArtworkUrl = oi.ArtworkUrl,
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

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

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
    }
}