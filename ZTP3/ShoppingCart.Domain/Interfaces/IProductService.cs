using ShoppingCart.Domain.Models;

namespace ShoppingCart.Domain.Interfaces
{
    public interface IProductService
    {
        Task<Product?> GetProductByIdAsync(string productId);
    }
}
