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
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        public ShoppingCartDbContext(string connectionString, string databaseName)
        {
            // Configure MongoDB serialization settings once
            if (!_isInitialized)
            {
                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        ConfigureMongoDb();
                        _isInitialized = true;
                    }
                }
            }

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        private static void ConfigureMongoDb()
        {
            // Register Guid serializer with Standard representation
            try
            {
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            }
            catch (BsonSerializationException)
            {
                // Serializer already registered, ignore
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
