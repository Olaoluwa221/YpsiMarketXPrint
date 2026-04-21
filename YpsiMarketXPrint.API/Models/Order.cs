namespace YpsiMarketXPrint.API.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int? UserId { get; set; }
        public string? GuestEmail { get; set; }
        public DateTime DateOrdered { get; set; } = DateTime.UtcNow;
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Shipping;
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public decimal ShippingCost { get; set; } = 0;
        public User? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = [];
    }
}
