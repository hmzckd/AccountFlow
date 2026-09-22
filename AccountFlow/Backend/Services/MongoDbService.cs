using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using AccountFlow.Backend.Entities;

namespace AccountFlow.Backend.Services
{
    // Manages MongoDB database connections and provides access to collections
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;

        // Initializes the MongoDB client and establishes a connection to the specified database
        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DbConnection")
                ?? throw new InvalidOperationException("Connection string 'DbConnection' is not configured.");
            var mongoUrl = MongoUrl.Create(connectionString);
            var mongoClient = new MongoClient(mongoUrl);
            _database = mongoClient.GetDatabase(mongoUrl.DatabaseName);

            EnsureIndexes();
        }

        public IMongoDatabase Database => _database;

        // Generic helper method to retrieve a specific collection by name
        public IMongoCollection<T> GetCollection<T>(string name) =>
            _database.GetCollection<T>(name);

        // Provides direct access to the 'users' collection
        public IMongoCollection<User> Users => GetCollection<User>("users");

        // Enforces a unique index on email so two concurrent registrations can't create duplicates.
        private void EnsureIndexes()
        {
            var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var indexModel = new CreateIndexModel<User>(indexKeys, new CreateIndexOptions { Unique = true });
            var createdAtIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.CreatedAt));
            Users.Indexes.CreateMany([indexModel, createdAtIndex]);
        }
    }
}
