using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.DTOs;
using YpsiMarketXPrint.API.Models;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<Cart> GetOrCreateCart(int userId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // GET api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await GetOrCreateCart(userId);

            var items = await _context.CartItems
                .Where(ci => ci.CartId == cart.CartId)
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.ProductPictures)
                        .ThenInclude(pp => pp.Picture)
                .Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductName,
                    ProductSize = ci.Product.ProductSize,
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity,
                    ImageLink = ci.Product.ProductPictures
                    .Where(pp => pp.IsPrimary)
                    .Select(pp => pp.Picture.Link)
                    .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new CartDto
            {
                CartId = cart.CartId,
                Items = items
            });
        }

        // POST api/cart/items
        [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemDto dto)
        {
            var userId = GetUserId();
            var cart = await GetOrCreateCart(userId);

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound("Product not found.");

            var existing = await _context.CartItems
                .FindAsync(cart.CartId, dto.ProductId);

            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Item added to cart.");
        }

        // PUT api/cart/items/1
        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateItem(int productId, UpdateCartItemDto dto)
        {
            var userId = GetUserId();
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound("Cart not found.");

            var item = await _context.CartItems.FindAsync(cart.CartId, productId);
            if (item == null) return NotFound("Item not found in cart.");

            if (dto.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = dto.Quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Cart updated.");
        }

        // DELETE api/cart/items/1
        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var userId = GetUserId();
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound("Cart not found.");

            var item = await _context.CartItems.FindAsync(cart.CartId, productId);
            if (item == null) return NotFound("Item not found in cart.");

            _context.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Item removed from cart.");
        }

        // DELETE api/cart
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found.");

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Cart cleared.");
        }
    }
}