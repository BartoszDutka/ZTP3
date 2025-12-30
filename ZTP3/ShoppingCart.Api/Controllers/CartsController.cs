using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.Application.Commands;
using ShoppingCart.Application.Queries;
using ShoppingCart.Domain.Exceptions;

namespace ShoppingCart.Api.Controllers
{
    [ApiController]
    [Route("api/v1/carts")]
    public class CartsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CartsController> _logger;

        public CartsController(IMediator mediator, ILogger<CartsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
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
            try
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
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict when adding product to cart {CartId}", cartId);
                return Conflict(new { message = "The cart was modified by another request. Please try again." });
            }
        }

        [HttpDelete("{cartId}/items/{productId}")]
        public async Task<ActionResult> RemoveProductFromCart(Guid cartId, string productId, [FromQuery] int? quantity)
        {
            try
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
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict when removing product from cart {CartId}", cartId);
                return Conflict(new { message = "The cart was modified by another request. Please try again." });
            }
        }

        [HttpPost("{cartId}/checkout")]
        public async Task<ActionResult> CheckoutCart(Guid cartId)
        {
            try
            {
                var command = new CheckoutCartCommand { CartId = cartId };
                var orderId = await _mediator.Send(command);

                if (orderId == null)
                {
                    return BadRequest(new { message = "Failed to checkout cart. Cart may be empty or already checked out." });
                }

                return Ok(new { orderId, message = "Cart checked out successfully" });
            }
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict when checking out cart {CartId}", cartId);
                return Conflict(new { message = "The cart was modified during checkout. Please review and try again." });
            }
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
