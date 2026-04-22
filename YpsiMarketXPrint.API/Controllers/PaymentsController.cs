using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;
using YpsiMarketXPrint.API.Data;
using Microsoft.EntityFrameworkCore;
using YpsiMarketXPrint.API.DTOs;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public PaymentsController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST api/payments/create-intent
        [HttpPost("create-intent")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateIntent([FromBody] CreateIntentDto dto)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            decimal subtotal = 0;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                var userId = int.Parse(userIdClaim);
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Variant)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return BadRequest("Cart is empty.");

                subtotal = cart.CartItems.Sum(ci => ci.Variant.Price * ci.Quantity);
            }
            else
            {
                if (dto.CartItems == null || !dto.CartItems.Any())
                    return BadRequest("Cart is empty.");

                var variantIds = dto.CartItems.Select(ci => ci.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .Where(v => variantIds.Contains(v.VariantId))
                    .ToListAsync();

                subtotal = dto.CartItems.Sum(ci =>
                {
                    var variant = variants.FirstOrDefault(v => v.VariantId == ci.VariantId);
                    return variant != null ? variant.Price * ci.Quantity : 0;
                });
            }

            // Add shipping cost if applicable
            var shippingCost = string.Equals(dto.DeliveryMethod, "Pickup", StringComparison.OrdinalIgnoreCase)
                ? 0m
                : 8.00m;

            var total = subtotal + shippingCost;
            var amount = (long)(total * 100);

            if (amount <= 0)
                return BadRequest("Invalid order amount.");

            var options = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return Ok(new { clientSecret = intent.ClientSecret, paymentIntentId = intent.Id });
        }
    }
}