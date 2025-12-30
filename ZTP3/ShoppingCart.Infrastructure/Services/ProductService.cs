using Microsoft.Extensions.Logging;
using ShoppingCart.Domain.Interfaces;
using ShoppingCart.Domain.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingCart.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;

        public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Product?> GetProductByIdAsync(string productId)
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
                
                if (apiResponse?.Data == null)
                {
                    return null;
                }

                return new Product
                {
                    Id = apiResponse.Data.Id,
                    Name = apiResponse.Data.Name,
                    Price = apiResponse.Data.Price,
                    Description = apiResponse.Data.Description ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", productId);
                return null;
            }
        }

        private class ApiResponse<T>
        {
            public T? Data { get; set; }
        }

        private class ProductDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string? Description { get; set; }
        }
    }
}
