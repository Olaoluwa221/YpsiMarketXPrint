namespace YpsiMarketXPrint.API.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductType { get; set; } = null!;
        public string ProductSize { get; set; } = null!;
        public decimal Price { get; set; }
        public string? PrimaryImageLink { get; set; }
    }

    public class CreateProductDto
    {
        public string ProductName { get; set; } = null!;
        public int ProductTypeId { get; set; }
        public string ProductSize { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class UpdateProductDto
    {
        public string? ProductName { get; set; }
        public int? ProductTypeId { get; set; }
        public string? ProductSize { get; set; }
        public decimal? Price { get; set; }
    }
}