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
            var products = await _context
                .Products.Include(p => p.ProductType)
                .Include(p => p.Variants)
                .Include(p => p.ProductPictures)
                    .ThenInclude(pp => pp.Picture)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    ProductType = p.ProductType.TypeName,
                    RequiresArtwork = p.RequiresArtwork,
                    PrimaryImageLink = p
                        .ProductPictures.Where(pp => pp.IsPrimary)
                        .Select(pp => pp.Picture.Link)
                        .FirstOrDefault(),
                    Variants = p
                        .Variants.Select(v => new ProductVariantDto
                        {
                            VariantId = v.VariantId,
                            Size = v.Size,
                            Price = v.Price,
                        })
                        .ToList(),
                    Pictures = p
                        .ProductPictures.Select(pp => new ProductPictureDto
                        {
                            PictureId = pp.PictureId,
                            Link = pp.Picture.Link,
                            IsPrimary = pp.IsPrimary,
                        })
                        .ToList(),
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET api/products/types - public
        [HttpGet("types")]
        public async Task<IActionResult> GetTypes()
        {
            var types = await _context
                .ProductTypes.OrderBy(t => t.TypeName)
                .Select(t => new { t.ProductTypeId, t.TypeName })
                .ToListAsync();

            return Ok(types);
        }

        // GET api/products/1 - public
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context
                .Products.Include(p => p.ProductType)
                .Include(p => p.Variants)
                .Include(p => p.ProductPictures)
                    .ThenInclude(pp => pp.Picture)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            var dto = new ProductDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description,
                ProductType = product.ProductType.TypeName,
                RequiresArtwork = product.RequiresArtwork,
                PrimaryImageLink = product
                    .ProductPictures.Where(pp => pp.IsPrimary)
                    .Select(pp => pp.Picture.Link)
                    .FirstOrDefault(),
                Variants = product
                    .Variants.Select(v => new ProductVariantDto
                    {
                        VariantId = v.VariantId,
                        Size = v.Size,
                        Price = v.Price,
                    })
                    .ToList(),
                Pictures = product
                    .ProductPictures.Select(pp => new ProductPictureDto
                    {
                        PictureId = pp.PictureId,
                        Link = pp.Picture.Link,
                        IsPrimary = pp.IsPrimary,
                    })
                    .ToList(),
            };

            return Ok(dto);
        }

        // POST api/products - admin only
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            var productType = await _context.ProductTypes.FindAsync(dto.ProductTypeId);
            if (productType == null)
                return BadRequest("Invalid product type.");

            var product = new Product
            {
                ProductName = dto.ProductName,
                Description = dto.Description,
                ProductTypeId = dto.ProductTypeId,
                RequiresArtwork = dto.RequiresArtwork,
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { product.ProductId, product.ProductName });
        }

        // PUT api/products/1 - admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            if (dto.ProductName != null)
                product.ProductName = dto.ProductName;
            if (dto.Description != null)
                product.Description = dto.Description;
            if (dto.RequiresArtwork.HasValue)
                product.RequiresArtwork = dto.RequiresArtwork.Value;
            if (dto.ProductTypeId.HasValue)
            {
                var productType = await _context.ProductTypes.FindAsync(dto.ProductTypeId.Value);
                if (productType == null)
                    return BadRequest("Invalid product type.");
                product.ProductTypeId = dto.ProductTypeId.Value;
            }

            await _context.SaveChangesAsync();
            return Ok(new { product.ProductId, product.ProductName });
        }

        // DELETE api/products/1 - admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST api/products/1/variants - admin only
        [HttpPost("{id}/variants")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddVariant(int id, CreateVariantDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            var variant = new ProductVariant
            {
                ProductId = id,
                Size = dto.Size,
                Price = dto.Price,
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return Ok(
                new ProductVariantDto
                {
                    VariantId = variant.VariantId,
                    Size = variant.Size,
                    Price = variant.Price,
                }
            );
        }

        // PUT api/products/variants/1 - admin only
        [HttpPut("variants/{variantId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateVariant(int variantId, UpdateVariantDto dto)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null)
                return NotFound();

            if (dto.Size != null)
                variant.Size = dto.Size;
            if (dto.Price.HasValue)
                variant.Price = dto.Price.Value;

            await _context.SaveChangesAsync();
            return Ok(
                new ProductVariantDto
                {
                    VariantId = variant.VariantId,
                    Size = variant.Size,
                    Price = variant.Price,
                }
            );
        }

        // DELETE api/products/variants/1 - admin only
        [HttpDelete("variants/{variantId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteVariant(int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null)
                return NotFound();

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
