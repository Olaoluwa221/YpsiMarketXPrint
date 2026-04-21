namespace YpsiMarketXPrint.API.Models
{
    public class ArtworkUploadToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public int OrderId { get; set; }
        public int VariantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? InvalidatedAt { get; set; }

        public OrderItem OrderItem { get; set; } = null!;
    }
}
