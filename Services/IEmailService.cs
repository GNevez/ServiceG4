using g4api.Models;

namespace g4api.Services;

public interface IEmailService
{
    /// <summary>
    /// Envia email de atualização de status do pedido
    /// </summary>
    Task EnviarEmailStatusPedidoAsync(Pedido pedido, StatusPedido novoStatus, string? observacoes = null);
    
    /// <summary>
    /// Envia email genérico
    /// </summary>
    Task EnviarEmailAsync(string destinatario, string assunto, string corpoHtml);
    
    /// <summary>
    /// Envia email de confirmação de pedido (quando o pedido é criado)
    /// </summary>
    Task EnviarEmailConfirmacaoPedidoAsync(Pedido pedido);
    
    /// <summary>
    /// Envia email com QR Code do PIX
    /// </summary>
    Task EnviarEmailPixAsync(Pedido pedido, string qrCode, string qrCodeBase64);

    /// <summary>
    /// Envia email de atualização de status da devolução
    /// </summary>
    Task EnviarEmailStatusDevolucaoAsync(Devolucao devolucao, DevolucaoStatus novoStatus, string? observacoes = null);
}
