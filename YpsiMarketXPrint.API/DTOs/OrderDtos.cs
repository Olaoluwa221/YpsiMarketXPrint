namespace YpsiMarketXPrint.API.DTOs
{
    public class OrderItemDto
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Size { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Math.Round(UnitPrice * Quantity, 2);
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderStatus { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = [];
        public decimal Total => Math.Round(Items.Sum(i => i.Subtotal), 2);
    }

    public class UpdateOrderStatusDto
    {
        public string OrderStatus { get; set; } = null!;
    }

    public class GuestCheckoutDto
    {
        public string? GuestEmail { get; set; }
        public List<GuestCartItemDto>? CartItems { get; set; }
    }

    public class CreateIntentDto
    {
        public List<GuestCartItemDto>? CartItems { get; set; }
    }

    public class GuestCartItemDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
