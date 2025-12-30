using MediatR;

namespace ShoppingCart.Application.Commands
{
    public class CreateCartCommand : IRequest<Guid>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
