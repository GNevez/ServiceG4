namespace g4api.DTOs;

#region Request DTOs

/// <summary>
/// DTO para criar um pagamento no Mercado Pago
/// </summary>
public class CriarPagamentoDto
{
    // Dados do cliente
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    
    // Endereço
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    
    // Pagamento
    public string MetodoPagamento { get; set; } = "credit_card"; 
    public string? CardToken { get; set; } 
    public int? Parcelas { get; set; } = 1;
    public int? ParcelasNum { get; set; } 
    public int GetParcelas() => ParcelasNum ?? Parcelas ?? 1;
    public string? PaymentMethodId { get; set; } // Ex: "visa", "master", "amex", etc.
    public decimal? TotalAmountMercadoPago { get; set; } // Total calculado pelo MP com juros
    
    // Frete
    public decimal? PrecoFrete { get; set; }
    public string? CodigoServicoFrete { get; set; }
    public int? PrazoEntregaFrete { get; set; }
}

/// <summary>
/// Request para a API do Mercado Pago - Criar Pagamento
/// </summary>
public class MercadoPagoPaymentRequest
{
    public decimal TransactionAmount { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Installments { get; set; } = 1;
    public string PaymentMethodId { get; set; } = string.Empty;
    public MercadoPagoPayer Payer { get; set; } = new();
    public string? ExternalReference { get; set; }
    public MercadoPagoAdditionalInfo? AdditionalInfo { get; set; }
    public string? NotificationUrl { get; set; }
}

/// <summary>
/// Request para pagamento via PIX
/// </summary>
public class MercadoPagoPixRequest
{
    public decimal TransactionAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = "pix";
    public MercadoPagoPayer Payer { get; set; } = new();
    public string? ExternalReference { get; set; }
    public string? NotificationUrl { get; set; }
}

public class MercadoPagoPayer
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public MercadoPagoIdentification? Identification { get; set; }
    public MercadoPagoAddress? Address { get; set; }
}

public class MercadoPagoIdentification
{
    public string Type { get; set; } = "CPF";
    public string Number { get; set; } = string.Empty;
}

public class MercadoPagoAddress
{
    public string ZipCode { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? FederalUnit { get; set; }
}

public class MercadoPagoAdditionalInfo
{
    public List<MercadoPagoItem>? Items { get; set; }
    public MercadoPagoShipments? Shipments { get; set; }
}

public class MercadoPagoItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class MercadoPagoShipments
{
    public MercadoPagoReceiverAddress? ReceiverAddress { get; set; }
}

public class MercadoPagoReceiverAddress
{
    public string ZipCode { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
    public string? Apartment { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Resposta do pagamento do Mercado Pago
/// </summary>
public class MercadoPagoPaymentResponse
{
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDetail { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string PaymentTypeId { get; set; } = string.Empty;
    public decimal TransactionAmount { get; set; }
    public int Installments { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateApproved { get; set; }
    public MercadoPagoTransactionDetails? TransactionDetails { get; set; }
    public MercadoPagoPointOfInteraction? PointOfInteraction { get; set; }
}

public class MercadoPagoTransactionDetails
{
    public decimal? NetReceivedAmount { get; set; }
    public decimal? TotalPaidAmount { get; set; }
    public decimal? InstallmentAmount { get; set; }
}

public class MercadoPagoPointOfInteraction
{
    public MercadoPagoTransactionData? TransactionData { get; set; }
}

public class MercadoPagoTransactionData
{
    public string? QrCode { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? TicketUrl { get; set; }
}

/// <summary>
/// Resposta simplificada para o frontend
/// </summary>
public class PagamentoResponseDto
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public long MercadoPagoPaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDetail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Parcelas { get; set; }
    public PixInfoDto? Pix { get; set; }
}

public class PixInfoDto
{
    public string? QrCode { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? TicketUrl { get; set; }
}

#endregion

#region Webhook DTOs

public class MercadoPagoWebhookNotification
{
    public long Id { get; set; }
    public bool LiveMode { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public MercadoPagoWebhookData? Data { get; set; }
}

public class MercadoPagoWebhookData
{
    public string? Id { get; set; }
}

#endregion

#region Métodos de Pagamento

/// <summary>
/// Informações sobre métodos de pagamento disponíveis
/// </summary>
public class MetodoPagamentoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PaymentTypeId { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public int? MinAllowedAmount { get; set; }
    public int? MaxAllowedAmount { get; set; }
}

/// <summary>
/// Informações sobre parcelas
/// </summary>
public class ParcelasDto
{
    public string PaymentMethodId { get; set; } = string.Empty;
    public string PaymentTypeId { get; set; } = string.Empty;
    public List<ParcelaOpcaoDto> PayerCosts { get; set; } = new();
}

public class ParcelaOpcaoDto
{
    public int Installments { get; set; }
    public decimal InstallmentRate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string RecommendedMessage { get; set; } = string.Empty;
}

#endregion
