using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YpsiMarketXPrint.API.Data;
using YpsiMarketXPrint.API.DTOs;
using YpsiMarketXPrint.API.Models;
using YpsiMarketXPrint.API.Services;

namespace YpsiMarketXPrint.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailService? _emailService;

        public AuthController(
            AppDbContext context,
            IConfiguration config,
            EmailService? emailService = null
        )
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already in use.");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                UserType = "customer",
                MarketingOptIn = dto.MarketingOptIn,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Registration successful.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            var token = GenerateToken(user);

            return Ok(
                new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    UserType = user.UserType,
                }
            );
        }

        // PUT api/auth/marketing-opt-in
        [HttpPut("marketing-opt-in")]
        [Authorize]
        public async Task<IActionResult> UpdateMarketingOptIn([FromBody] bool optIn)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.MarketingOptIn = optIn;
            await _context.SaveChangesAsync();

            return Ok(new { marketingOptIn = user.MarketingOptIn });
        }

        // GET api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(
                new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.UserType,
                    user.MarketingOptIn,
                }
            );
        }

        // GET api/auth/opted-in-count
        [HttpGet("opted-in-count")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetOptedInCount()
        {
            var count = await _context.Users.CountAsync(u => u.MarketingOptIn);
            return Ok(new { count });
        }

        // POST api/auth/send-promotional
        [HttpPost("send-promotional")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendPromotional([FromBody] SendPromotionalDto dto)
        {
            if (_emailService == null)
                return StatusCode(500, "Email service is not configured.");

            var emails = await _context
                .Users.Where(u => u.MarketingOptIn)
                .Select(u => u.Email)
                .ToListAsync();

            if (!emails.Any())
                return BadRequest("No opted-in users found.");

            try
            {
                await _emailService.SendPromotionalEmailAsync(emails, dto.Subject, dto.HtmlBody);
                return Ok(new { message = $"Emails sent to {emails.Count} subscribers." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send emails: {ex.Message}");
            }
        }

        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.UserType),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
