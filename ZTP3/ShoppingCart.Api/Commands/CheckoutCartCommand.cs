using MediatR;

namespace ShoppingCart.Api.Commands
{
    public class CheckoutCartCommand : IRequest<Guid?>
    {
        public Guid CartId { get; set; }
    }
}
