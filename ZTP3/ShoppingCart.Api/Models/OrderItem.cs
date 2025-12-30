using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShoppingCart.Api.Models
{
    public class OrderItem
    {
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        
        [BsonRequired]
        public string ProductId { get; set; } = string.Empty;
        
        [BsonRequired]
        public string ProductName { get; set; } = string.Empty;
        
        [BsonRequired]
        public int Quantity { get; set; }
        
        [BsonRequired]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal UnitPrice { get; set; }
        
        [BsonRequired]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalPrice { get; set; }
    }
}
