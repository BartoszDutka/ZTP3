using MediatR;
using MongoDB.Driver;
using ShoppingCart.Api.Data;

namespace ShoppingCart.Api.Commands
{
    public class RemoveProductFromCartCommandHandler : IRequestHandler<RemoveProductFromCartCommand, bool>
    {
        private readonly ShoppingCartDbContext _context;

        public RemoveProductFromCartCommandHandler(ShoppingCartDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(RemoveProductFromCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _context.Carts
                .Find(c => c.Id == request.CartId)
                .FirstOrDefaultAsync(cancellationToken);

            if (cart == null || cart.IsCheckedOut)
            {
                return false;
            }

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item == null)
            {
                return false;
            }

            if (request.Quantity.HasValue && request.Quantity.Value > 0)
            {
                item.Quantity -= request.Quantity.Value;
                
                if (item.Quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
            }
            else
            {
                cart.Items.Remove(item);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<Models.Cart>.Filter.Eq(c => c.Id, cart.Id);
            await _context.Carts.ReplaceOneAsync(filter, cart, cancellationToken: cancellationToken);

            return true;
        }
    }
}
