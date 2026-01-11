using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using ShoppingCart.Domain.Entities;

namespace ShoppingCart.Infrastructure.Persistence
{
    public class ShoppingCartDbContext
    {
        private readonly IMongoDatabase _database;
        private static bool _isConfigured = false;

        public ShoppingCartDbContext(string connectionString, string databaseName)
        {
            if (!_isConfigured)
            {
                ConfigureMongoDb();
                _isConfigured = true;
            }

            MongoClient client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        private static void ConfigureMongoDb()
        {
            try
            {
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            }
            catch (BsonSerializationException)
            {
            }
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
