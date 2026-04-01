namespace YpsiMarketXPrint.API.Models
{
    public class ProductPicture
    {
        public int ProductId { get; set; }
        public int PictureId { get; set; }
        public bool IsPrimary { get; set; } = false;

        public Product Product { get; set; } = null!;
        public Picture Picture { get; set; } = null!;
    }
}
