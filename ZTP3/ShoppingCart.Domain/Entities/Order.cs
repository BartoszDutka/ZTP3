namespace ShoppingCart.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid CartId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
