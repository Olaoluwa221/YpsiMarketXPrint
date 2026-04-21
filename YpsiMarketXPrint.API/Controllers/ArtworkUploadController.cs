using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.Models;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/artwork-upload")]
    [AllowAnonymous]
    public class ArtworkUploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public ArtworkUploadController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // A token is valid iff it's not used, not manually invalidated, and the order
        // isn't in a "done" state (shipped/delivered/pickedup/cancelled).
        private static readonly OrderStatus[] DoneStatuses = new[]
        {
            OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.PickedUp, OrderStatus.Cancelled
        };

        // GET api/artwork-upload/{token}
        // Returns the product info the upload page needs, or a reason why the token isn't usable.
        [HttpGet("{token}")]
        public async Task<IActionResult> GetTokenInfo(string token)
        {
            var tokenRow = await _context.ArtworkUploadTokens
                .Include(t => t.OrderItem).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                .Include(t => t.OrderItem).ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (tokenRow == null)
                return NotFound(new { status = "notfound", message = "This upload link is not valid." });

            if (tokenRow.UsedAt != null)
                return Ok(new { status = "used", message = "Artwork for this item has already been uploaded." });

            if (tokenRow.InvalidatedAt != null)
                return Ok(new { status = "invalidated", message = "This upload link is no longer active. Check your email for a newer link." });

            if (DoneStatuses.Contains(tokenRow.OrderItem.Order.OrderStatus))
                return Ok(new { status = "orderclosed", message = "This order has already shipped or is closed, so artwork can no longer be uploaded." });

            return Ok(new
            {
                status = "valid",
                orderId = tokenRow.OrderId,
                productName = tokenRow.OrderItem.Variant.Product.ProductName,
                size = tokenRow.OrderItem.Variant.Size,
                quantity = tokenRow.OrderItem.Quantity,
            });
        }

        // POST api/artwork-upload/{token}
        [HttpPost("{token}")]
        public async Task<IActionResult> Upload(string token, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            if (file.Length > 20 * 1024 * 1024)
                return BadRequest("File size cannot exceed 20MB.");

            var allowedTypes = new[] {
                "image/jpeg", "image/png", "image/webp", "image/gif",
                "application/pdf"
            };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG, PNG, WebP, GIF and PDF files are allowed.");

            var tokenRow = await _context.ArtworkUploadTokens
                .Include(t => t.OrderItem).ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (tokenRow == null)
                return NotFound("Upload link is not valid.");

            if (tokenRow.UsedAt != null)
                return BadRequest("This upload link has already been used.");

            if (tokenRow.InvalidatedAt != null)
                return BadRequest("This upload link is no longer active.");

            if (DoneStatuses.Contains(tokenRow.OrderItem.Order.OrderStatus))
                return BadRequest("This order has already shipped or is closed.");

            var connectionString = _config["Azure:StorageConnectionString"]!;
            var containerName = _config["Azure:ArtworkContainerName"] ?? "customer-artwork";
            var serviceClient = new BlobServiceClient(connectionString);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var fileName = $"order-{tokenRow.OrderId}-variant-{tokenRow.VariantId}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            var artworkUrl = blobClient.Uri.ToString()
                .Replace("http://azurite:10000", "http://localhost:10000");

            // Guest-accessible endpoint, so uploader is unknown.
            var picture = new Picture { UploaderId = null, Link = artworkUrl };
            _context.Pictures.Add(picture);
            await _context.SaveChangesAsync();

            tokenRow.OrderItem.ArtworkId = picture.PictureId;
            tokenRow.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Artwork uploaded successfully." });
        }
    }
}
