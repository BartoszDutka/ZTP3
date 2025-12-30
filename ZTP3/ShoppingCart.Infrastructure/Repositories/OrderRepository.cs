using MongoDB.Driver;
using ShoppingCart.Domain.Entities;
using ShoppingCart.Domain.Interfaces;
using ShoppingCart.Infrastructure.Persistence;

namespace ShoppingCart.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ShoppingCartDbContext _context;

        public OrderRepository(ShoppingCartDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Find(o => o.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Guid> CreateAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _context.Orders.InsertOneAsync(order, cancellationToken: cancellationToken);
            return order.Id;
        }
    }
}
