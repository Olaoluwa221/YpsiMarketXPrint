namespace YpsiMarketXPrint.API.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int ProductTypeId { get; set; }
        public string ProductSize { get; set; } = null!;
        public decimal Price { get; set; }

        public ProductType ProductType { get; set; } = null!;
        public ICollection<ProductPicture> ProductPictures { get; set; } = [];
        public ICollection<OrderItem> OrderItems { get; set; } = [];
        public ICollection<CartItem> CartItems { get; set; } = [];
    }
}