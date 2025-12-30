using MediatR;
using ShoppingCart.Api.Data;
using ShoppingCart.Api.Models;

namespace ShoppingCart.Api.Commands
{
    public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, Guid>
    {
        private readonly ShoppingCartDbContext _context;

        public CreateCartCommandHandler(ShoppingCartDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateCartCommand request, CancellationToken cancellationToken)
        {
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.InsertOneAsync(cart, cancellationToken: cancellationToken);

            return cart.Id;
        }
    }
}
