using MediatR;

namespace ShoppingCart.Application.Commands
{
    public class RemoveProductFromCartCommand : IRequest<bool>
    {
        public Guid CartId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int? Quantity { get; set; }
    }
}
