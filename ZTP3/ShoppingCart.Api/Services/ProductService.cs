using ShoppingCart.Api.DTOs;

namespace ShoppingCart.Api.Services
{
    public interface IProductService
    {
        Task<ProductDto?> GetProductByIdAsync(string productId);
    }

    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;

        public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ProductDto?> GetProductByIdAsync(string productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/products/{productId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get product {ProductId}. Status: {Status}", 
                        productId, response.StatusCode);
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
                return apiResponse?.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", productId);
                return null;
            }
        }
    }
}
