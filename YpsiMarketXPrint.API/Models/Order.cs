namespace YpsiMarketXPrint.API.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime DateOrdered { get; set; } = DateTime.UtcNow;
        public string OrderStatus { get; set; } = "pending";

        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = [];
    }
}
