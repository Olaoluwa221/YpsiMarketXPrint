namespace YpsiMarketXPrint.API.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int? UserId { get; set; }
        public string? GuestEmail { get; set; }
        public string? PaymentIntentId { get; set; }
        public DateTime DateOrdered { get; set; } = DateTime.UtcNow;
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Shipping;
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public decimal ShippingCost { get; set; } = 0;

        // Contact + shipping info captured at checkout. All nullable so pickup orders
        // (or future order types) don't require them.
        public string? ContactFirstName { get; set; }
        public string? ContactLastName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingZip { get; set; }

        public User? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = [];
    }
}
