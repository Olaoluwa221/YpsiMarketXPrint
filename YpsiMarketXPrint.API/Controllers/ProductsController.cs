using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.DTOs;
using YpsiMarketXPrint.API.Models;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/products - public
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.ProductPictures)
                    .ThenInclude(pp => pp.Picture)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductType = p.ProductType,
                    ProductSize = p.ProductSize,
                    Price = p.Price,
                    PrimaryImageLink = p.ProductPictures
                        .Where(pp => pp.IsPrimary)
                        .Select(pp => pp.Picture.Link)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET api/products/1 - public
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductPictures)
                    .ThenInclude(pp => pp.Picture)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            var dto = new ProductDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ProductType = product.ProductType,
                ProductSize = product.ProductSize,
                Price = product.Price,
                PrimaryImageLink = product.ProductPictures
                    .Where(pp => pp.IsPrimary)
                    .Select(pp => pp.Picture.Link)
                    .FirstOrDefault()
            };

            return Ok(dto);
        }

        // POST api/products - admin only
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            var product = new Product
            {
                ProductName = dto.ProductName,
                ProductType = dto.ProductType,
                ProductSize = dto.ProductSize,
                Price = dto.Price
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }

        // PUT api/products/1 - admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (dto.ProductName != null) product.ProductName = dto.ProductName;
            if (dto.ProductType != null) product.ProductType = dto.ProductType;
            if (dto.ProductSize != null) product.ProductSize = dto.ProductSize;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // DELETE api/products/1 - admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}