using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcoLilly.Services
{
    public class CashfreeCreateTokenRequest
    {
        public string? orderId { get; set; }
        public string? orderAmount { get; set; }
        public string orderCurrency { get; set; } = "INR";
        public string? customerName { get; set; }
        public string? customerPhone { get; set; }
        public string? customerEmail { get; set; }
        public string? returnUrl { get; set; }
    }

    public class CashfreeCreateTokenResponse
    {
        public string? status { get; set; }
        public string? message { get; set; }
        public string? cftoken { get; set; }
    }

    public class CashfreeService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly ILogger<CashfreeService> _logger;

        public CashfreeService(IConfiguration config, HttpClient http, ILogger<CashfreeService> logger)
        {
            _config = config;
            _http = http;
            _logger = logger;
        }

        private (string apiBase, string checkoutUrl) GetUrls()
        {
            var env = _config["Cashfree:Environment"]?.ToUpper() ?? "TEST";
            if (env == "PROD" || env == "PRODUCTION")
                return ("https://api.cashfree.com", "https://www.cashfree.com/checkout/post/submit");

            return ("https://test.cashfree.com", "https://test.cashfree.com/checkout/post/submit");
        }

        public async Task<string?> CreateCftokenAsync(string orderId, decimal amount, string customerName, string phone, string email, string returnUrl)
        {
            var (apiBase, _) = GetUrls();

            var req = new CashfreeCreateTokenRequest
            {
                orderId = orderId,
                orderAmount = amount.ToString("0.00"),
                customerName = customerName,
                customerPhone = phone,
                customerEmail = email,
                returnUrl = returnUrl
            };

            var json = JsonSerializer.Serialize(req);
            using var request = new HttpRequestMessage(HttpMethod.Post, apiBase + "/api/v2/cftoken/order")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Accept", "application/json");

            var appId = _config["Cashfree:AppId"];
            var secret = _config["Cashfree:SecretKey"];
            if (!string.IsNullOrEmpty(appId)) request.Headers.Add("x-client-id", appId);
            if (!string.IsNullOrEmpty(secret)) request.Headers.Add("x-client-secret", secret);

            try
            {
                var resp = await _http.SendAsync(request);

                var respJson = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    // Log the response body so we know why token creation failed
                    _logger.LogError("Cashfree CreateCftoken failed. Status: {StatusCode}. Response: {Response}", resp.StatusCode, respJson);
                    return null;
                }

                var parsed = JsonSerializer.Deserialize<CashfreeCreateTokenResponse>(respJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed == null)
                {
                    _logger.LogError("Cashfree CreateCftoken: unable to parse response: {Response}", respJson);
                    return null;
                }

                if (!string.Equals(parsed.status, "OK", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(parsed.status, "SUCCESS", System.StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Cashfree CreateCftoken returned status {Status} message {Message} body: {Response}", parsed.status, parsed.message, respJson);
                }

                _logger.LogInformation("Cashfree CreateCftoken succeeded for order {OrderId} token present: {HasToken}", orderId, !string.IsNullOrEmpty(parsed.cftoken));
                return parsed?.cftoken;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Cashfree CreateCftoken exception for order {OrderId}", orderId);
                return null;
            }
        }

        public string GetCheckoutPostUrl() => GetUrls().checkoutUrl;
    }
}