namespace YpsiMarketXPrint.API.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public int ProductTypeId { get; set; }

        public ProductType ProductType { get; set; } = null!;
        public ICollection<ProductVariant> Variants { get; set; } = [];
        public ICollection<ProductPicture> ProductPictures { get; set; } = [];
    }
}