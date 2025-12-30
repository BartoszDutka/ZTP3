namespace ShoppingCart.Domain.Entities
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReservationExpiresAt { get; set; }
    }
}
