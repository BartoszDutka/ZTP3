using MediatR;

namespace ShoppingCart.Api.Commands
{
    public class CreateCartCommand : IRequest<Guid>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
