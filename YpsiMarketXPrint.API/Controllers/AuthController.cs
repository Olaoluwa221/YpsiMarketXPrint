using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        [EnableRateLimiting("auth-standard")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email))
                return BadRequest("A valid email is required.");

            var passwordError = ValidatePassword(dto.Password);
            if (passwordError != null)
                return BadRequest(passwordError);

            // Generic success response either way, so an attacker cannot probe for
            // existing emails. We still create the user silently if the email is new.
            var genericResponse = Ok(new { message = "If the details are valid, your account has been created. Please sign in." });

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailTaken)
                return genericResponse;

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                UserType = UserType.Customer,
                MarketingOptIn = dto.MarketingOptIn,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return genericResponse;
        }

        private static string? ValidatePassword(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return "Password must be at least 8 characters.";
            if (password.Length > 128)
                return "Password must be 128 characters or fewer.";
            if (!password.Any(char.IsLetter))
                return "Password must contain at least one letter.";
            if (!password.Any(char.IsDigit))
                return "Password must contain at least one number.";
            return null;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth-strict")]
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
                    UserType = user.UserType.ToString().ToLower(),
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
                    UserType = user.UserType.ToString().ToLower(),
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
                .Select(u => new { u.UserId, u.Email })
                .ToListAsync();

            if (!emails.Any())
                return BadRequest("No opted-in users found.");

            try
            {
                var recipients = emails.Select(u => (u.UserId, u.Email)).ToList();
                await _emailService.SendPromotionalEmailAsync(recipients, dto.Subject, dto.HtmlBody);
                return Ok(new { message = $"Emails sent to {emails.Count} subscribers." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send emails: {ex.Message}");
            }
        }

        // POST api/auth/forgot-password
        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Ok(new { message = "If that email exists, a reset link has been sent." });

            var existingTokens = _context.PasswordResetTokens.Where(t =>
                t.UserId == user.UserId && !t.Used
            );
            _context.PasswordResetTokens.RemoveRange(existingTokens);

            var token = Convert
                .ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            _context.PasswordResetTokens.Add(
                new PasswordResetToken
                {
                    UserId = user.UserId,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    Used = false,
                }
            );

            await _context.SaveChangesAsync();

            try
            {
                if (_emailService != null)
                    await _emailService.SendPasswordResetAsync(user.Email, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(new { message = "If that email exists, a reset link has been sent." });
        }

        // POST api/auth/reset-password
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth-strict")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var passwordError = ValidatePassword(dto.NewPassword);
            if (passwordError != null)
                return BadRequest(passwordError);

            var resetToken = await _context
                .PasswordResetTokens.Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == dto.Token && !t.Used);

            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired reset token.");

            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            resetToken.Used = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully." });
        }

        // PUT api/auth/update-profile
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            await _context.SaveChangesAsync();

            return Ok(new { user.FirstName, user.LastName });
        }

        // PUT api/auth/update-password
        [HttpPut("update-password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var passwordError = ValidatePassword(dto.NewPassword);
            if (passwordError != null)
                return BadRequest(passwordError);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully." });
        }

        // GET api/auth/users
        [HttpGet("users")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context
                .Users.Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    UserType = u.UserType.ToString().ToLower(),
                    u.MarketingOptIn,
                    OrderCount = _context.Orders.Count(o => o.UserId == u.UserId),
                })
                .OrderBy(u => u.Email)
                .ToListAsync();

            return Ok(users);
        }

        // PUT api/auth/users/{id}/role
        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!Enum.TryParse<UserType>(dto.Role, true, out var newUserType))
                return BadRequest("Invalid role.");

            user.UserType = newUserType;
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    user.UserId,
                    user.Email,
                    UserType = user.UserType.ToString().ToLower(),
                }
            );
        }

        // POST api/auth/unsubscribe/{token}
        [HttpPost("unsubscribe/{token}")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-standard")]
        public async Task<IActionResult> Unsubscribe(string token)
        {
            var userId = VerifyUnsubscribeToken(token);
            if (userId == null)
                return BadRequest(new { message = "This unsubscribe link is not valid." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return BadRequest(new { message = "This unsubscribe link is not valid." });

            user.MarketingOptIn = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "You have been unsubscribed from marketing emails.", email = user.Email });
        }

        // Generate a deterministic HMAC-signed unsubscribe token. No DB row required.
        public static string GenerateUnsubscribeToken(int userId, string secret)
        {
            var payload = userId.ToString();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var raw = $"{payload}.{Convert.ToBase64String(sig)}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private int? VerifyUnsubscribeToken(string token)
        {
            try
            {
                // Reverse the base64url encoding
                var padded = token.Replace("-", "+").Replace("_", "/");
                switch (padded.Length % 4)
                {
                    case 2: padded += "=="; break;
                    case 3: padded += "="; break;
                }
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));

                var parts = decoded.Split('.', 2);
                if (parts.Length != 2) return null;
                if (!int.TryParse(parts[0], out var userId)) return null;

                var secret = _config["Jwt:Key"]!;
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0])));

                // Constant-time comparison
                var a = Encoding.UTF8.GetBytes(expected);
                var b = Encoding.UTF8.GetBytes(parts[1]);
                if (!CryptographicOperations.FixedTimeEquals(a, b)) return null;

                return userId;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.UserType.ToString().ToLower()),
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
