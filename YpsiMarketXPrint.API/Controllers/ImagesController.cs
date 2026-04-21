using System.Security.Claims;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public ImagesController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<BlobContainerClient> GetContainerClient()
        {
            var connectionString = _config["Azure:StorageConnectionString"];
            var containerName = _config["Azure:ContainerName"];

            var serviceClient = new BlobServiceClient(connectionString);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return containerClient;
        }

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

            var container = await GetContainerClient();
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = container.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            var imageUrl = blobClient.Uri.ToString()
                .Replace("http://azurite:10000", "http://localhost:10000");

            var picture = new Picture { UploaderId = GetUserId(), Link = imageUrl };
            _context.Pictures.Add(picture);
            await _context.SaveChangesAsync();

            return Ok(new { pictureId = picture.PictureId, link = imageUrl });
        }

        // POST api/images/products/{productId}/assign
        [HttpPost("products/{productId}/assign")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AssignToProduct(int productId, [FromBody] AssignImageDto dto)
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

            if (dto.IsPrimary)
            {
                var currentPrimary = _context.ProductPictures
                    .Where(pp => pp.ProductId == productId && pp.IsPrimary)
                    .ToList();
                currentPrimary.ForEach(pp => pp.IsPrimary = false);
            }

            _context.ProductPictures.Add(new ProductPicture
            {
                ProductId = productId,
                PictureId = dto.PictureId,
                IsPrimary = dto.IsPrimary,
            });

            await _context.SaveChangesAsync();
            return Ok("Image assigned to product.");
        }

        // PUT api/images/products/{productId}/primary/{pictureId}
        [HttpPut("products/{productId}/primary/{pictureId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SetPrimary(int productId, int pictureId)
        {
            var currentPrimaries = _context.ProductPictures
                .Where(pp => pp.ProductId == productId && pp.IsPrimary)
                .ToList();
            currentPrimaries.ForEach(pp => pp.IsPrimary = false);

            var target = await _context.ProductPictures.FindAsync(productId, pictureId);
            if (target == null)
                return NotFound("Image not assigned to this product.");

            target.IsPrimary = true;
            await _context.SaveChangesAsync();
            return Ok("Primary image updated.");
        }

        private string GenerateArtworkSasUrl(string blobUrl)
        {
            var connectionString = _config["Azure:StorageConnectionString"]!;
            var containerName = "customer-artwork";

            var fileName = Path.GetFileName(new Uri(blobUrl).AbsolutePath);

            var serviceClient = new BlobServiceClient(connectionString);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString()
                .Replace("http://azurite:10000", "http://localhost:10000");
        }

        // GET api/images/orders/{orderId}/artwork/{variantId}/url
        [HttpGet("orders/{orderId}/artwork/{variantId}/url")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetArtworkUrl(int orderId, int variantId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Artwork)
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.VariantId == variantId);

            if (orderItem == null || orderItem.Artwork == null)
                return NotFound("No artwork found for this order item.");

            try
            {
                var sasUrl = GenerateArtworkSasUrl(orderItem.Artwork.Link);
                return Ok(new { url = sasUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to generate artwork URL: {ex.Message}");
            }
        }

        // DELETE api/images/products/{productId}/pictures/{pictureId}
        [HttpDelete("products/{productId}/pictures/{pictureId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> RemoveFromProduct(int productId, int pictureId)
        {
            var pp = await _context.ProductPictures.FindAsync(productId, pictureId);
            if (pp == null)
                return NotFound();

            var picture = await _context.Pictures.FindAsync(pictureId);
            if (picture != null)
            {
                try
                {
                    var container = await GetContainerClient();
                    var fileName = Path.GetFileName(new Uri(picture.Link).AbsolutePath);
                    var blobClient = container.GetBlobClient(fileName);
                    await blobClient.DeleteIfExistsAsync();
                }
                catch { }

                _context.Pictures.Remove(picture);
            }

            _context.ProductPictures.Remove(pp);
            await _context.SaveChangesAsync();
            return Ok("Image removed.");
        }
    }
}