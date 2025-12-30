using MediatR;
using ShoppingCart.Application.DTOs;

namespace ShoppingCart.Application.Queries
{
    public class GetCartQuery : IRequest<CartDto?>
    {
        public Guid CartId { get; set; }
    }
}
