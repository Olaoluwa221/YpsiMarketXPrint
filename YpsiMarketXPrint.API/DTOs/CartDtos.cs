namespace YpsiMarketXPrint.API.DTOs
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductSize { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Math.Round(Price * Quantity, 2);
        public string? ImageLink { get; set; }
    }

    public class CartDto
    {
        public int CartId { get; set; }
        public List<CartItemDto> Items { get; set; } = [];
        public decimal Total => Math.Round(Items.Sum(i => i.Subtotal), 2);
    }

    public class AddCartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemDto
    {
        public int Quantity { get; set; }
    }
}