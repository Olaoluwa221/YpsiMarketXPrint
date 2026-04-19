namespace YpsiMarketXPrint.API.DTOs
{
    public class RegisterDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool MarketingOptIn { get; set; } = false;
    }

    public class SendPromotionalDto
    {
        public string Subject { get; set; } = null!;
        public string HtmlBody { get; set; } = null!;
    }

    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserType { get; set; } = null!;
    }
}
