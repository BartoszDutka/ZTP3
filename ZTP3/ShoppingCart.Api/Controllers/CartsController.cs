using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.Api.Commands;
using ShoppingCart.Api.Queries;

namespace ShoppingCart.Api.Controllers
{
    [ApiController]
    [Route("api/v1/carts")]
    public class CartsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CartsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateCart([FromBody] CreateCartCommand command)
        {
            var cartId = await _mediator.Send(command);
            return Ok(cartId);
        }

        [HttpGet("{cartId}")]
        public async Task<ActionResult> GetCart(Guid cartId)
        {
            var cart = await _mediator.Send(new GetCartQuery { CartId = cartId });
            
            if (cart == null)
            {
                return NotFound(new { message = "Cart not found" });
            }

            return Ok(cart);
        }

        [HttpPost("{cartId}/items")]
        public async Task<ActionResult> AddProductToCart(Guid cartId, [FromBody] AddProductRequest request)
        {
            var command = new AddProductToCartCommand
            {
                CartId = cartId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                return BadRequest(new { message = "Failed to add product to cart" });
            }

            return Ok(new { message = "Product added to cart successfully" });
        }

        [HttpDelete("{cartId}/items/{productId}")]
        public async Task<ActionResult> RemoveProductFromCart(Guid cartId, string productId, [FromQuery] int? quantity)
        {
            var command = new RemoveProductFromCartCommand
            {
                CartId = cartId,
                ProductId = productId,
                Quantity = quantity
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                return BadRequest(new { message = "Failed to remove product from cart" });
            }

            return Ok(new { message = "Product removed from cart successfully" });
        }

        [HttpPost("{cartId}/checkout")]
        public async Task<ActionResult> CheckoutCart(Guid cartId)
        {
            var command = new CheckoutCartCommand { CartId = cartId };
            var orderId = await _mediator.Send(command);

            if (orderId == null)
            {
                return BadRequest(new { message = "Failed to checkout cart" });
            }

            return Ok(new { orderId, message = "Cart checked out successfully" });
        }
    }

    public class CreateCartRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class AddProductRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
