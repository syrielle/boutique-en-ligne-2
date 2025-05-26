using System.Text.Json;
using ECommerceBoutique.Models;

namespace ECommerceBoutique.Services
{
    public class FakeStoreService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://fakestoreapi.com";

        public FakeStoreService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var response = await _httpClient.GetAsync("/products");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            
            var fakeProducts = JsonSerializer.Deserialize<List<FakeStoreProduct>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return fakeProducts.Select(fp => new Product
            {
                Name = fp.Title,
                Description = fp.Description,
                Price = (decimal)fp.Price,
                ImageUrl = fp.Image,
                Category = fp.Category,
                StockQuantity = new Random().Next(10, 100) // Simulation d'un stock aléatoire
            }).ToList();
        }

        private class FakeStoreProduct
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public double Price { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public string Image { get; set; }
        }
    }
} 