using MongoDB.Driver;
using ShoppingCart.Api.Models;

namespace ShoppingCart.Api.Data
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "ShoppingCartDb";
    }

    public class ShoppingCartDbContext
    {
        private readonly IMongoDatabase _database;

        public ShoppingCartDbContext(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public IMongoCollection<Cart> Carts => _database.GetCollection<Cart>("Carts");
        public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");
    }
}
