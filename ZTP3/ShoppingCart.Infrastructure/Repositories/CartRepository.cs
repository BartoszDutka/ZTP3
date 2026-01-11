using MongoDB.Driver;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Domain.Exceptions;
using ShoppingCart.Domain.Interfaces;
using ShoppingCart.Infrastructure.Persistence;

namespace ShoppingCart.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ShoppingCartDbContext _context;

        public CartRepository(ShoppingCartDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Carts
                .Find(c => c.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Guid> CreateAsync(Cart cart, CancellationToken cancellationToken = default)
        {
            cart.Version = 0;
            await _context.Carts.InsertOneAsync(cart, cancellationToken: cancellationToken);
            return cart.Id;
        }

        public async Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
        {
            long oldVersion = cart.Version;
            cart.Version++;
            cart.UpdatedAt = DateTime.UtcNow;

            FilterDefinition<Cart> filter = Builders<Cart>.Filter.And(
                Builders<Cart>.Filter.Eq(c => c.Id, cart.Id),
                Builders<Cart>.Filter.Eq(c => c.Version, oldVersion)
            );

            ReplaceOneResult result = await _context.Carts.ReplaceOneAsync(
                filter, cart, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
                throw new ConcurrencyException($"Cart {cart.Id} was modified. Retry.");
        }

        public async Task<List<Cart>> GetInactiveCartsAsync(DateTime threshold, CancellationToken cancellationToken = default)
        {
            return await _context.Carts
                .Find(c => !c.IsCheckedOut && 
                           (c.LastActivityAt == null || c.LastActivityAt < threshold))
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Cart>.Filter.Eq(c => c.Id, id);
            await _context.Carts.DeleteOneAsync(filter, cancellationToken);
        }
    }
}
