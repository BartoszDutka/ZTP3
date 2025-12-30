using MediatR;

namespace ShoppingCart.Application.Commands
{
    public class CheckoutCartCommand : IRequest<Guid?>
    {
        public Guid CartId { get; set; }
    }
}
