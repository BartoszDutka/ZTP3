using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShoppingCart.Api.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        
        [BsonRepresentation(BsonType.String)]
        public Guid CartId { get; set; }
        
        [BsonRequired]
        public string UserId { get; set; } = string.Empty;
        
        public List<OrderItem> Items { get; set; } = new();
        
        [BsonRequired]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalAmount { get; set; }
        
        [BsonRequired]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
