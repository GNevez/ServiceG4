using g4api.DTOs;

namespace g4api.Services;

public interface IMercadoPagoService
{
    /// <summary>
    /// Obtém a Public Key para usar no frontend
    /// </summary>
    string GetPublicKey();
    
    /// <summary>
    /// Cria um pagamento com cartão de crédito
    /// </summary>
    Task<MercadoPagoPaymentResponse> CreateCardPaymentAsync(MercadoPagoPaymentRequest request);
    
    /// <summary>
    /// Cria um pagamento via PIX
    /// </summary>
    Task<MercadoPagoPaymentResponse> CreatePixPaymentAsync(MercadoPagoPixRequest request);
    
    /// <summary>
    /// Busca informações de um pagamento
    /// </summary>
    Task<MercadoPagoPaymentResponse?> GetPaymentAsync(long paymentId);
    
    /// <summary>
    /// Busca métodos de pagamento disponíveis
    /// </summary>
    Task<List<MetodoPagamentoDto>> GetPaymentMethodsAsync();
    
    /// <summary>
    /// Busca opções de parcelamento para um valor
    /// </summary>
    Task<ParcelasDto?> GetInstallmentsAsync(decimal amount, string? paymentMethodId = null, string? bin = null);
}
