using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.DTOs;
using YpsiMarketXPrint.API.Models;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ImagesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST api/images/upload
        [HttpPost("upload")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG, PNG and WebP images are allowed.");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size cannot exceed 5MB.");

            // Save to wwwroot/images
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "images");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";

            // Save to Pictures table
            var picture = new Picture { UploaderId = GetUserId(), Link = imageUrl };

            _context.Pictures.Add(picture);
            await _context.SaveChangesAsync();

            return Ok(new { pictureId = picture.PictureId, link = imageUrl });
        }

        // POST api/images/products/{productId}/assign
        [HttpPost("products/{productId}/assign")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AssignToProduct(
            int productId,
            [FromBody] AssignImageDto dto
        )
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound("Product not found.");

            var picture = await _context.Pictures.FindAsync(dto.PictureId);
            if (picture == null)
                return NotFound("Picture not found.");

            var existing = await _context.ProductPictures.FindAsync(productId, dto.PictureId);
            if (existing != null)
                return BadRequest("Image already assigned to this product.");

            // If setting as primary, unset existing primary
            if (dto.IsPrimary)
            {
                var currentPrimary = _context
                    .ProductPictures.Where(pp => pp.ProductId == productId && pp.IsPrimary)
                    .ToList();
                currentPrimary.ForEach(pp => pp.IsPrimary = false);
            }

            _context.ProductPictures.Add(
                new ProductPicture
                {
                    ProductId = productId,
                    PictureId = dto.PictureId,
                    IsPrimary = dto.IsPrimary,
                }
            );

            await _context.SaveChangesAsync();
            return Ok("Image assigned to product.");
        }

        // PUT api/images/products/{productId}/primary/{pictureId}
        [HttpPut("products/{productId}/primary/{pictureId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SetPrimary(int productId, int pictureId)
        {
            var currentPrimaries = _context
                .ProductPictures.Where(pp => pp.ProductId == productId && pp.IsPrimary)
                .ToList();
            currentPrimaries.ForEach(pp => pp.IsPrimary = false);

            var target = await _context.ProductPictures.FindAsync(productId, pictureId);
            if (target == null)
                return NotFound("Image not assigned to this product.");

            target.IsPrimary = true;
            await _context.SaveChangesAsync();
            return Ok("Primary image updated.");
        }

        // DELETE api/images/products/{productId}/pictures/{pictureId}
        [HttpDelete("products/{productId}/pictures/{pictureId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RemoveFromProduct(int productId, int pictureId)
        {
            var pp = await _context.ProductPictures.FindAsync(productId, pictureId);
            if (pp == null)
                return NotFound();

            _context.ProductPictures.Remove(pp);
            await _context.SaveChangesAsync();
            return Ok("Image removed from product.");
        }
    }
}
