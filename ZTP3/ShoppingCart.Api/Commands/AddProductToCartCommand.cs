using MediatR;

namespace ShoppingCart.Api.Commands
{
    public class AddProductToCartCommand : IRequest<bool>
    {
        public Guid CartId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
