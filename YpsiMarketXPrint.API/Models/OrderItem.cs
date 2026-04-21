namespace YpsiMarketXPrint.API.Models
{
    public class OrderItem
    {
        public int OrderId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? ArtworkId { get; set; }
        public Order Order { get; set; } = null!;
        public ProductVariant Variant { get; set; } = null!;
        public Picture? Artwork { get; set; }
    }
}