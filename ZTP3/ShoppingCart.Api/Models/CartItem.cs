using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShoppingCart.Api.Models
{
    public class CartItem
    {
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        
        [BsonRequired]
        public string ProductId { get; set; } = string.Empty;
        
        [BsonRequired]
        public int Quantity { get; set; }
    }
}
