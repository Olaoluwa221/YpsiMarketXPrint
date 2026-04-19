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
    [Authorize]
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
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();

            var cart = await _context
                .Carts.Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest("Your cart is empty.");

            var order = new Order
            {
                UserId = userId,
                DateOrdered = DateTime.UtcNow,
                OrderStatus = "pending",
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderItems = cart
                .CartItems.Select(ci => new OrderItem
                {
                    OrderId = order.OrderId,
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Variant.Price,
                })
                .ToList();

            _context.OrderItems.AddRange(orderItems);
            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send confirmation email
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (_emailService != null && user != null)
                {
                    var total = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
                    await _emailService.SendOrderConfirmationAsync(
                        user.Email,
                        order.OrderId,
                        total
                    );
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
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();

            var orders = await _context
                .Orders.Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(o => o.DateOrdered)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    DateOrdered = o.DateOrdered,
                    OrderStatus = o.OrderStatus,
                    Items = o
                        .OrderItems.Select(oi => new OrderItemDto
                        {
                            VariantId = oi.VariantId,
                            ProductId = oi.Variant.ProductId,
                            ProductName = oi.Variant.Product.ProductName,
                            Size = oi.Variant.Size,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                        })
                        .ToList(),
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET api/orders/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();

            var order = await _context
                .Orders.Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            if (order.UserId != userId && !User.IsInRole("admin") && !User.IsInRole("staff"))
                return Forbid();

            var dto = new OrderDto
            {
                OrderId = order.OrderId,
                DateOrdered = order.DateOrdered,
                OrderStatus = order.OrderStatus,
                Items = order
                    .OrderItems.Select(oi => new OrderItemDto
                    {
                        VariantId = oi.VariantId,
                        ProductId = oi.Variant.ProductId,
                        ProductName = oi.Variant.Product.ProductName,
                        Size = oi.Variant.Size,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                    })
                    .ToList(),
            };

            return Ok(dto);
        }

        // GET api/orders/all - admin and staff only
        [HttpGet("all")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context
                .Orders.Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(o => o.DateOrdered)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    DateOrdered = o.DateOrdered,
                    OrderStatus = o.OrderStatus,
                    Items = o
                        .OrderItems.Select(oi => new OrderItemDto
                        {
                            VariantId = oi.VariantId,
                            ProductId = oi.Variant.ProductId,
                            ProductName = oi.Variant.Product.ProductName,
                            Size = oi.Variant.Size,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                        })
                        .ToList(),
                })
                .ToListAsync();

            return Ok(orders);
        }

        // PUT api/orders/1/status - admin and staff only
        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var validStatuses = new[]
            {
                "pending",
                "processing",
                "shipped",
                "delivered",
                "cancelled",
            };
            if (!validStatuses.Contains(dto.OrderStatus.ToLower()))
                return BadRequest("Invalid order status.");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.OrderStatus = dto.OrderStatus.ToLower();
            await _context.SaveChangesAsync();

            // Send status update email
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (_emailService != null && user != null)
                    await _emailService.SendOrderStatusUpdateAsync(
                        user.Email,
                        order.OrderId,
                        order.OrderStatus
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(
                new
                {
                    message = "Order status updated.",
                    orderId = order.OrderId,
                    status = order.OrderStatus,
                }
            );
        }
    }
}
