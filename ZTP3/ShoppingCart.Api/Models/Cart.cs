using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShoppingCart.Api.Models
{
    public class Cart
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        
        [BsonRequired]
        public string UserId { get; set; } = string.Empty;
        
        public List<CartItem> Items { get; set; } = new();
        
        [BsonRequired]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonRequired]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsCheckedOut { get; set; } = false;
    }
}
