namespace YpsiMarketXPrint.API.Models
{
    public class Picture
    {
        public int PictureId { get; set; }
        public int UploaderId { get; set; }
        public string Link { get; set; } = null!;

        public User Uploader { get; set; } = null!;
        public ICollection<ProductPicture> ProductPictures { get; set; } = [];
    }
}
