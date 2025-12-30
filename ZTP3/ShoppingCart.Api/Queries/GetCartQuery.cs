using MediatR;
using ShoppingCart.Api.DTOs;

namespace ShoppingCart.Api.Queries
{
    public class GetCartQuery : IRequest<CartDto?>
    {
        public Guid CartId { get; set; }
    }
}
