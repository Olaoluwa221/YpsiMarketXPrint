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
        public int? ArtworkId { get; set; }
        public bool RequiresArtwork { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderStatus { get; set; } = null!;
        public string DeliveryMethod { get; set; } = "shipping";
        public decimal ShippingCost { get; set; } = 0;
        public decimal Subtotal => Items.Sum(i => i.Subtotal);
        public decimal Total => Subtotal + ShippingCost;
        public List<OrderItemDto> Items { get; set; } = [];
    }

    public class UpdateOrderStatusDto
    {
        public string OrderStatus { get; set; } = null!;
    }

    public class GuestCheckoutDto
    {
        public string? GuestEmail { get; set; }
        public List<GuestCartItemDto>? CartItems { get; set; }
        public string DeliveryMethod { get; set; } = "Shipping";
    }

    public class CreateIntentDto
    {
        public List<GuestCartItemDto>? CartItems { get; set; }
        public string? DeliveryMethod { get; set; } = "Shipping";
    }

    public class GuestCartItemDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
