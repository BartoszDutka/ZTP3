namespace ShoppingCart.Domain.Entities
{
    public class Cart
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<CartItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsCheckedOut { get; set; } = false;
        public DateTime? LastActivityAt { get; set; }
        public long Version { get; set; } = 0;
    }
}
