namespace YpsiMarketXPrint.API.Models
{
    public class ProductVariant
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string Size { get; set; } = null!;
        public decimal Price { get; set; }

        public Product Product { get; set; } = null!;
        public ICollection<CartItem> CartItems { get; set; } = [];
        public ICollection<OrderItem> OrderItems { get; set; } = [];
    }
}