namespace YpsiMarketXPrint.API.DTOs
{
    public class ProductPictureDto
    {
        public int PictureId { get; set; }
        public string Link { get; set; } = null!;
        public bool IsPrimary { get; set; }
    }

    public class ProductVariantDto
    {
        public int VariantId { get; set; }
        public string Size { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public string ProductType { get; set; } = null!;
        public string? PrimaryImageLink { get; set; }
        public List<ProductVariantDto> Variants { get; set; } = [];
        public List<ProductPictureDto> Pictures { get; set; } = [];
    }

    public class CreateProductDto
    {
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public int ProductTypeId { get; set; }
    }

    public class UpdateProductDto
    {
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int? ProductTypeId { get; set; }
    }

    public class CreateVariantDto
    {
        public string Size { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class UpdateVariantDto
    {
        public string? Size { get; set; }
        public decimal? Price { get; set; }
    }
}
