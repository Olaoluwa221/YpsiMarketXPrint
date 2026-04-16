namespace YpsiMarketXPrint.API.Models
{
    public class ProductType
    {
        public int ProductTypeId { get; set; }
        public string TypeName { get; set; } = null!;

        public ICollection<Product> Products { get; set; } = [];
    }
}