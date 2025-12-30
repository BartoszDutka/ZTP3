using MongoDB.Driver;
using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Infrastructure.Persistence
{
    public class ShoppingCartDbContext
    {
        private readonly IMongoDatabase _database;

        public ShoppingCartDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Cart> Carts => _database.GetCollection<Cart>("Carts");
        public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");
    }

    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "ShoppingCartDb";
    }
}
