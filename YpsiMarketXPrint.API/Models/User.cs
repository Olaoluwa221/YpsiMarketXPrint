namespace YpsiMarketXPrint.API.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string UserType { get; set; } = "customer";

        public ICollection<Picture> Pictures { get; set; } = [];
        public ICollection<Order> Orders { get; set; } = [];
        public Cart? Cart { get; set; }
    }
}
