using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using g4api.DTOs;

namespace g4api.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoPagoService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MercadoPagoService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<MercadoPagoService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri("https://api.mercadopago.com");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _configuration["MercadoPago:AccessToken"]);

        // Configurar JSON options para snake_case
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }

    public string GetPublicKey()
    {
        return _configuration["MercadoPago:PublicKey"] ?? throw new InvalidOperationException("MercadoPago PublicKey not configured");
    }

    public async Task<MercadoPagoPaymentResponse> CreateCardPaymentAsync(MercadoPagoPaymentRequest request)
    {
        try
        {
            // Adicionar URL de webhook se configurada
            var webhookUrl = _configuration["MercadoPago:WebhookUrl"];
            if (!string.IsNullOrEmpty(webhookUrl))
            {
                request.NotificationUrl = webhookUrl;
            }

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogInformation("[MercadoPago] ===== REQUEST TO MP API =====");
            _logger.LogInformation("[MercadoPago] Request JSON: {Json}", json);
            _logger.LogInformation("[MercadoPago] ==============================");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Adicionar header de idempotência
            var idempotencyKey = Guid.NewGuid().ToString();
            content.Headers.Add("X-Idempotency-Key", idempotencyKey);

            var response = await _httpClient.PostAsync("/v1/payments", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[MercadoPago] ===== RESPONSE FROM MP API =====");
            _logger.LogInformation("[MercadoPago] StatusCode: {StatusCode}", response.StatusCode);
            _logger.LogInformation("[MercadoPago] Response: {Content}", responseContent);
            _logger.LogInformation("[MercadoPago] ================================");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[MercadoPago] Error creating payment: {Content}", responseContent);
                throw new Exception($"Erro ao criar pagamento: {responseContent}");
            }

            var paymentResponse = JsonSerializer.Deserialize<MercadoPagoPaymentResponse>(responseContent, _jsonOptions);
            return paymentResponse ?? throw new Exception("Resposta inválida do Mercado Pago");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoPago] Exception creating card payment");
            throw;
        }
    }

    public async Task<MercadoPagoPaymentResponse> CreatePixPaymentAsync(MercadoPagoPixRequest request)
    {
        try
        {
            // Adicionar URL de webhook se configurada
            var webhookUrl = _configuration["MercadoPago:WebhookUrl"];
            if (!string.IsNullOrEmpty(webhookUrl))
            {
                request.NotificationUrl = webhookUrl;
            }

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogInformation("[MercadoPago] Creating PIX payment: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Adicionar header de idempotência
            var idempotencyKey = Guid.NewGuid().ToString();
            content.Headers.Add("X-Idempotency-Key", idempotencyKey);

            var response = await _httpClient.PostAsync("/v1/payments", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[MercadoPago] PIX payment response: {StatusCode} - {Content}", 
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[MercadoPago] Error creating PIX payment: {Content}", responseContent);
                throw new Exception($"Erro ao criar pagamento PIX: {responseContent}");
            }

            var paymentResponse = JsonSerializer.Deserialize<MercadoPagoPaymentResponse>(responseContent, _jsonOptions);
            return paymentResponse ?? throw new Exception("Resposta inválida do Mercado Pago");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoPago] Exception creating PIX payment");
            throw;
        }
    }

    public async Task<MercadoPagoPaymentResponse?> GetPaymentAsync(long paymentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v1/payments/{paymentId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[MercadoPago] Get payment response: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[MercadoPago] Error getting payment: {Content}", responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<MercadoPagoPaymentResponse>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoPago] Exception getting payment {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<List<MetodoPagamentoDto>> GetPaymentMethodsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/payment_methods");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[MercadoPago] Error getting payment methods: {Content}", responseContent);
                return new List<MetodoPagamentoDto>();
            }

            return JsonSerializer.Deserialize<List<MetodoPagamentoDto>>(responseContent, _jsonOptions) 
                ?? new List<MetodoPagamentoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoPago] Exception getting payment methods");
            return new List<MetodoPagamentoDto>();
        }
    }

    public async Task<ParcelasDto?> GetInstallmentsAsync(decimal amount, string? paymentMethodId = null, string? bin = null)
    {
        try
        {
            // Formatar amount com ponto decimal (InvariantCulture)
            var amountStr = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var url = $"/v1/payment_methods/installments?amount={amountStr}";
            if (!string.IsNullOrEmpty(paymentMethodId))
            {
                url += $"&payment_method_id={paymentMethodId}";
            }
            if (!string.IsNullOrEmpty(bin))
            {
                url += $"&bin={bin}";
            }

            _logger.LogInformation("[MercadoPago] Getting installments: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[MercadoPago] Error getting installments: {Content}", responseContent);
                return null;
            }

            var installments = JsonSerializer.Deserialize<List<ParcelasDto>>(responseContent, _jsonOptions);
            return installments?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoPago] Exception getting installments");
            return null;
        }
    }
}
