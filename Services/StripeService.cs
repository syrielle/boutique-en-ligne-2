using Stripe;
using ECommerceBoutique.Models;

namespace ECommerceBoutique.Services
{
    public class StripeService
    {
        private readonly IConfiguration _configuration;

        public StripeService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreatePaymentIntent(ECommerceBoutique.Models.Order order)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(order.Total * 100), 
                Currency = "eur",
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() },
                    { "UserId", order.UserId }
                }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return intent.ClientSecret;
        }

        public async Task<PaymentIntent> GetPaymentIntent(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            return await service.GetAsync(paymentIntentId);
        }

        public async Task<PaymentIntent> UpdatePaymentIntent(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentUpdateOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "Status", "Paid" }
                }
            };
            return await service.UpdateAsync(paymentIntentId, options);
        }
    }
} 