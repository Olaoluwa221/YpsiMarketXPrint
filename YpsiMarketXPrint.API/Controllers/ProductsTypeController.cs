using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.Models;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductTypesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductTypesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/producttypes - public
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var types = await _context.ProductTypes
                .OrderBy(t => t.TypeName)
                .Select(t => new { t.ProductTypeId, t.TypeName })
                .ToListAsync();

            return Ok(types);
        }

        // POST api/producttypes - admin only
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return BadRequest("Type name cannot be empty.");

            if (await _context.ProductTypes.AnyAsync(t => t.TypeName == typeName))
                return BadRequest("Product type already exists.");

            var productType = new ProductType { TypeName = typeName };
            _context.ProductTypes.Add(productType);
            await _context.SaveChangesAsync();

            return Ok(new { productType.ProductTypeId, productType.TypeName });
        }

        // PUT api/producttypes/1 - admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] string typeName)
        {
            var productType = await _context.ProductTypes.FindAsync(id);
            if (productType == null) return NotFound();

            if (string.IsNullOrWhiteSpace(typeName))
                return BadRequest("Type name cannot be empty.");

            if (await _context.ProductTypes.AnyAsync(t => t.TypeName == typeName && t.ProductTypeId != id))
                return BadRequest("Product type already exists.");

            productType.TypeName = typeName;
            await _context.SaveChangesAsync();

            return Ok(new { productType.ProductTypeId, productType.TypeName });
        }

        // DELETE api/producttypes/1 - admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var productType = await _context.ProductTypes.FindAsync(id);
            if (productType == null) return NotFound();

            if (await _context.Products.AnyAsync(p => p.ProductTypeId == id))
                return BadRequest("Cannot delete a product type that has products assigned to it.");

            _context.ProductTypes.Remove(productType);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}