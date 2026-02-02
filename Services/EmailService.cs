using System.Text;
using g4api.Data;
using g4api.Models;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace g4api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly G4DbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        G4DbContext context,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task EnviarEmailStatusPedidoAsync(Pedido pedido, StatusPedido novoStatus, string? observacoes = null)
    {
        _logger.LogInformation("[Email] Iniciando envio de email para pedido #{Id}, novo status: {Status}", pedido.Id, novoStatus);
        
        try
        {
            // Buscar pedido completo com relacionamentos
            var pedidoCompleto = await _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .FirstOrDefaultAsync(p => p.Id == pedido.Id);

            if (pedidoCompleto == null)
            {
                _logger.LogWarning("[Email] Pedido #{Id} não encontrado no banco de dados", pedido.Id);
                return;
            }

            if (string.IsNullOrEmpty(pedidoCompleto.EmailCliente))
            {
                _logger.LogWarning("[Email] Email do cliente não encontrado para o pedido #{Id}", pedido.Id);
                return;
            }

            _logger.LogInformation("[Email] Pedido encontrado - Cliente: {Email}, Código: {Codigo}", 
                pedidoCompleto.EmailCliente, pedidoCompleto.CodigoPedido);

            var assunto = ObterAssuntoPorStatus(novoStatus, pedidoCompleto.CodigoPedido);
            _logger.LogInformation("[Email] Assunto gerado: {Assunto}", assunto);

            var corpoHtml = GerarTemplateEmailPorStatus(pedidoCompleto, novoStatus, observacoes);
            _logger.LogInformation("[Email] Template HTML gerado com sucesso ({Length} caracteres)", corpoHtml.Length);

            await EnviarEmailAsync(pedidoCompleto.EmailCliente, assunto, corpoHtml);
            
            _logger.LogInformation("[Email] ✅ Email enviado com sucesso para {Email} - Pedido: {Codigo}, Status: {Status}", 
                pedidoCompleto.EmailCliente, pedidoCompleto.CodigoPedido, novoStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] ❌ Erro ao enviar email para pedido #{Id}", pedido.Id);
            // Não falhar a operação principal se o email falhar
        }
    }

    public async Task EnviarEmailConfirmacaoPedidoAsync(Pedido pedido)
    {
        await EnviarEmailStatusPedidoAsync(pedido, StatusPedido.AguardandoConfirmacao);
    }

    public async Task EnviarEmailPixAsync(Pedido pedido, string qrCode, string qrCodeBase64)
    {
        _logger.LogInformation("[Email] Enviando email com QR Code PIX para pedido #{Id}", pedido.Id);
        
        try
        {
            var pedidoCompleto = await _context.Pedidos
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == pedido.Id);

            if (pedidoCompleto == null || string.IsNullOrEmpty(pedidoCompleto.EmailCliente))
            {
                _logger.LogWarning("[Email] Pedido ou email não encontrado para envio de PIX");
                return;
            }

            var assunto = $"PIX - Pedido #{pedidoCompleto.CodigoPedido} - G4 Motocenter";
            var corpoHtml = GerarTemplateEmailPix(pedidoCompleto, qrCode, qrCodeBase64);

            await EnviarEmailAsync(pedidoCompleto.EmailCliente, assunto, corpoHtml);
            
            _logger.LogInformation("[Email] ✅ Email PIX enviado com sucesso para {Email}", pedidoCompleto.EmailCliente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] ❌ Erro ao enviar email PIX para pedido #{Id}", pedido.Id);
        }
    }

    public async Task EnviarEmailAsync(string destinatario, string assunto, string corpoHtml)
    {
        _logger.LogInformation("[Email] Configurando SMTP para envio...");
        
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
        var fromEmail = _configuration["Email:FromEmail"];
        var fromName = _configuration["Email:FromName"] ?? "G4 Motocenter";

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogWarning("[Email] ⚠️ Configurações SMTP incompletas - Host: {Host}, User: {User}", 
                string.IsNullOrEmpty(smtpHost) ? "VAZIO" : "OK",
                string.IsNullOrEmpty(smtpUser) ? "VAZIO" : "OK");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail ?? smtpUser));
            message.To.Add(new MailboxAddress("", destinatario));
            message.Subject = assunto;

            var bodyBuilder = new BodyBuilder { HtmlBody = corpoHtml };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            _logger.LogInformation("[Email] Conectando ao servidor SMTP {Host}:{Port}...", smtpHost, smtpPort);
            
            var secureSocketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            
            await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("[Email] ✅ Email enviado com sucesso para {Destinatario}", destinatario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] ❌ Erro ao enviar via SMTP");
            throw;
        }
    }

    private string ObterAssuntoPorStatus(StatusPedido status, string codigoPedido)
    {
        return status switch
        {
            StatusPedido.AguardandoConfirmacao => $"Pedido #{codigoPedido} - Aguardando Confirmação",
            StatusPedido.EmSeparacao => $"✅ Pagamento Confirmado - Pedido #{codigoPedido}",
            StatusPedido.ACaminho => $"🚚 Pedido #{codigoPedido} foi Enviado!",
            StatusPedido.Finalizado => $"🎉 Pedido #{codigoPedido} foi Entregue!",
            StatusPedido.Cancelado => $"❌ Pedido #{codigoPedido} foi Cancelado",
            _ => $"Atualização do Pedido #{codigoPedido}"
        };
    }

    private string GerarTemplateEmailPorStatus(Pedido pedido, StatusPedido status, string? observacoes)
    {
        // Cores da identidade visual G4 Motocenter - Tema Claro
        const string corVermelho = "#E63946";        // Vermelho G4
        const string corVermelhoEscuro = "#c52d39";  // Vermelho hover
        const string corFundo = "#f5f5f5";           // Fundo claro
        const string corBranco = "#ffffff";          // Cards brancos
        const string corTexto = "#1a1a1a";           // Texto escuro
        const string corTextoSecundario = "#666666"; // Texto secundário
        const string corBorda = "#e0e0e0";           // Bordas claras

        var frontendUrl = _configuration["Frontend:Url"] ?? "https://g4motocenter.com.br";
        var apiUrl = _configuration["Api:Url"] ?? "https://api.g4motocenter.com.br";
        var logoUrl = $"{apiUrl}/g4motocenter/Logo%20Loja.png";
        var contactEmail = _configuration["Email:ContactEmail"] ?? "contato@g4motocenter.com.br";
        
        var primeiroNome = pedido.NomeCliente?.Split(' ').FirstOrDefault() ?? "Cliente";
        var mensagemStatus = ObterMensagemPorStatus(status, pedido.CodigoRastreamento);
        var metodoPagamentoFormatado = FormatarMetodoPagamento(pedido.MetodoPagamento);

        var itensHtml = new StringBuilder();
        if (pedido.Carrinho?.Itens != null)
        {
            foreach (var item in pedido.Carrinho.Itens)
            {
                var nomeProduto = item.Produto?.TituloEcommerceProduto ?? "Produto";
                var cor = item.Grade?.CorPredominanteProduto ?? "";
                var tamanho = item.Grade?.TamanhoProduto ?? "";
                var variacao = !string.IsNullOrEmpty(cor) || !string.IsNullOrEmpty(tamanho) 
                    ? $" ({cor}{(!string.IsNullOrEmpty(cor) && !string.IsNullOrEmpty(tamanho) ? " / " : "")}{tamanho})" 
                    : "";
                
                itensHtml.Append($@"
                    <tr>
                        <td style=""padding: 16px 20px; border-bottom: 1px solid {corBorda};"">
                            <div style=""font-weight: 600; color: {corTexto}; margin-bottom: 4px; font-size: 15px;"">{nomeProduto}{variacao}</div>
                            <div style=""color: {corTextoSecundario}; font-size: 13px;"">Qtd: {item.Quantidade}</div>
                        </td>
                        <td style=""padding: 16px 20px; border-bottom: 1px solid {corBorda}; text-align: right; color: {corTexto}; font-weight: 700; font-size: 15px;"">
                            R$ {(item.Quantidade * item.PrecoUnitario):F2}
                        </td>
                    </tr>
                ");
            }
        }

        // Seção de rastreamento (se aplicável)
        var trackingSection = "";
        if (status == StatusPedido.ACaminho && !string.IsNullOrEmpty(pedido.CodigoRastreamento))
        {
            trackingSection = $@"
                <tr>
                    <td style=""padding: 0 30px 25px 30px;"">
                        <div style=""background: {corBranco}; padding: 24px; border-radius: 12px; border: 1px solid {corBorda}; text-align: center;"">
                            <p style=""margin: 0 0 8px 0; color: {corTextoSecundario}; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;"">Código de Rastreamento</p>
                            <p style=""margin: 0 0 16px 0; font-size: 28px; font-weight: 700; color: {corTexto}; letter-spacing: 2px; font-family: monospace;"">{pedido.CodigoRastreamento}</p>
                            <a href=""https://www.linkcorreios.com.br/{pedido.CodigoRastreamento}"" 
                               style=""display: inline-block; background: {corVermelho}; color: {corBranco}; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">
                                Rastrear Pedido
                            </a>
                        </div>
                    </td>
                </tr>
            ";
        }

        // Seção de observações
        var observacoesSection = "";
        if (!string.IsNullOrEmpty(observacoes))
        {
            observacoesSection = $@"
                <tr>
                    <td style=""padding: 0 30px 25px 30px;"">
                        <div style=""background: {corBranco}; border-left: 4px solid {corVermelho}; padding: 16px 20px; border-radius: 0 8px 8px 0;"">
                            <p style=""margin: 0; color: {corTexto}; font-size: 14px;""><strong>Obs:</strong> {observacoes}</p>
                        </div>
                    </td>
                </tr>
            ";
        }

        return $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>G4 Motocenter - Atualização do Pedido</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; background-color: {corFundo};"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: {corFundo}; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: {corBranco}; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);"">
                    
                    <!-- Header com gradiente vermelho -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, {corVermelho} 0%, {corVermelhoEscuro} 100%); padding: 30px 30px; text-align: center;"">
                            <img src=""{logoUrl}"" alt=""G4 Motocenter"" style=""max-width: 200px; height: auto;"" />
                        </td>
                    </tr>
                    
                    <!-- Status Badge -->
                    <tr>
                        <td style=""padding: 40px 30px 20px 30px; text-align: center;"">
                            <div style=""display: inline-block; background: {corVermelho}; color: {corBranco}; padding: 12px 32px; border-radius: 50px; font-weight: 700; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;"">
                                {ObterTextoStatus(status)}
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Saudação + Mensagem -->
                    <tr>
                        <td style=""padding: 20px 30px 30px 30px; text-align: center;"">
                            <h2 style=""margin: 0 0 12px 0; color: {corTexto}; font-size: 24px; font-weight: 600;"">
                                Olá, {primeiroNome}!
                            </h2>
                            <p style=""color: {corTextoSecundario}; font-size: 15px; line-height: 1.7; margin: 0; max-width: 480px; margin: 0 auto;"">
                                {mensagemStatus}
                            </p>
                        </td>
                    </tr>
                    
                    {trackingSection}
                    {observacoesSection}
                    
                    <!-- Card de Detalhes do Pedido -->
                    <tr>
                        <td style=""padding: 0 30px 25px 30px;"">
                            <div style=""background: {corFundo}; border-radius: 12px; overflow: hidden; border: 1px solid {corBorda};"">
                                <div style=""background: {corVermelho}; padding: 16px 20px;"">
                                    <h3 style=""margin: 0; color: {corBranco}; font-size: 16px; font-weight: 600;"">📦 Detalhes do Pedido</h3>
                                </div>
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding: 20px;"">
                                    <tr>
                                        <td style=""padding: 10px 20px; color: {corTextoSecundario}; font-size: 14px;"">Pedido</td>
                                        <td style=""padding: 10px 20px; color: {corTexto}; font-weight: 600; text-align: right; font-size: 14px; font-family: monospace;"">#{pedido.CodigoPedido}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 10px 20px; color: {corTextoSecundario}; font-size: 14px;"">Data</td>
                                        <td style=""padding: 10px 20px; color: {corTexto}; font-weight: 600; text-align: right; font-size: 14px;"">{pedido.DataPedido:dd/MM/yyyy HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 10px 20px; color: {corTextoSecundario}; font-size: 14px;"">Pagamento</td>
                                        <td style=""padding: 10px 20px; color: {corTexto}; font-weight: 600; text-align: right; font-size: 14px;"">{metodoPagamentoFormatado}</td>
                                    </tr>
                                    <tr>
                                        <td style=""padding: 16px 20px 10px 20px; color: {corTextoSecundario}; font-size: 14px; border-top: 1px solid {corBorda};"">Total</td>
                                        <td style=""padding: 16px 20px 10px 20px; color: {corVermelho}; font-weight: 700; text-align: right; font-size: 24px; border-top: 1px solid {corBorda};"">R$ {pedido.TotalPedido:F2}</td>
                                    </tr>
                                </table>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Itens do Pedido -->
                    <tr>
                        <td style=""padding: 0 30px 25px 30px;"">
                            <div style=""background: {corFundo}; border-radius: 12px; overflow: hidden; border: 1px solid {corBorda};"">
                                <div style=""background: {corVermelho}; padding: 16px 20px;"">
                                    <h3 style=""margin: 0; color: {corBranco}; font-size: 16px; font-weight: 600;"">🛒 Itens</h3>
                                </div>
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                    {itensHtml}
                                </table>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- CTA Button -->
                    <tr>
                        <td style=""padding: 0 30px 40px 30px; text-align: center;"">
                            <a href=""{frontendUrl}/minha-conta/pedidos/{pedido.CodigoPedido}"" 
                               style=""display: inline-block; background: {corVermelho}; color: {corBranco}; padding: 16px 48px; border-radius: 10px; text-decoration: none; font-weight: 700; font-size: 15px;"">
                                Ver Pedido Completo
                            </a>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background: {corFundo}; padding: 30px; text-align: center; border-top: 1px solid {corBorda};"">
                            <p style=""margin: 0 0 8px 0; color: {corTextoSecundario}; font-size: 13px;"">
                                Dúvidas? Fale com a gente
                            </p>
                            <a href=""mailto:{contactEmail}"" style=""color: {corVermelho}; font-weight: 600; font-size: 14px; text-decoration: none;"">
                                {contactEmail}
                            </a>
                            
                            <p style=""margin: 24px 0 0 0; color: {corTextoSecundario}; font-size: 11px;"">
                                © {DateTime.Now.Year} G4 Motocenter. Todos os direitos reservados.
                            </p>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
        ";
    }

    private string GerarTemplateEmailPix(Pedido pedido, string qrCode, string qrCodeBase64)
    {
        // Cores da identidade visual G4 Motocenter - Tema Claro
        const string corVermelho = "#E63946";
        const string corVermelhoEscuro = "#c52d39";
        const string corFundo = "#f5f5f5";           // Fundo claro
        const string corBranco = "#ffffff";          // Cards brancos
        const string corTexto = "#1a1a1a";           // Texto escuro
        const string corTextoSecundario = "#666666"; // Texto secundário
        const string corBorda = "#e0e0e0";           // Bordas claras

        var apiUrl = _configuration["Api:Url"] ?? "https://api.g4motocenter.com.br";
        var logoUrl = $"{apiUrl}/g4motocenter/Logo%20Loja.png";
        var primeiroNome = pedido.NomeCliente?.Split(' ').FirstOrDefault() ?? "Cliente";

        return $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>G4 Motocenter - Pagamento PIX</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; background-color: {corFundo};"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: {corFundo}; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: {corBranco}; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);"">
                    
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, {corVermelho} 0%, {corVermelhoEscuro} 100%); padding: 30px 30px; text-align: center;"">
                            <img src=""{logoUrl}"" alt=""G4 Motocenter"" style=""max-width: 200px; height: auto; margin-bottom: 10px;"" />
                            <p style=""margin: 0; color: rgba(255,255,255,0.9); font-size: 14px; font-weight: 600;"">
                                Pagamento via PIX
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Saudação -->
                    <tr>
                        <td style=""padding: 40px 30px 20px 30px; text-align: center;"">
                            <h2 style=""margin: 0 0 12px 0; color: {corTexto}; font-size: 24px; font-weight: 600;"">
                                Olá, {primeiroNome}!
                            </h2>
                            <p style=""color: {corTextoSecundario}; font-size: 15px; line-height: 1.7; margin: 0;"">
                                Seu pedido foi criado! Escaneie o QR Code abaixo para realizar o pagamento.
                            </p>
                        </td>
                    </tr>

                    <!-- QR Code -->
                    <tr>
                        <td style=""padding: 0 30px 25px 30px; text-align: center;"">
                            <div style=""background: {corFundo}; padding: 24px; border-radius: 16px; display: inline-block; border: 1px solid {corBorda};"">
                                <img src=""data:image/png;base64,{qrCodeBase64}"" alt=""QR Code PIX"" style=""width: 200px; height: 200px;"" />
                            </div>
                        </td>
                    </tr>

                    <!-- Pedido Info -->
                    <tr>
                        <td style=""padding: 0 30px 25px 30px;"">
                            <div style=""background: {corFundo}; padding: 20px; border-radius: 12px; border: 1px solid {corBorda}; text-align: center;"">
                                <p style=""margin: 0 0 8px 0; color: {corTextoSecundario}; font-size: 12px; text-transform: uppercase;"">Pedido</p>
                                <p style=""margin: 0 0 16px 0; color: {corTexto}; font-size: 18px; font-weight: 700; font-family: monospace;"">#{pedido.CodigoPedido}</p>
                                <p style=""margin: 0 0 8px 0; color: {corTextoSecundario}; font-size: 12px; text-transform: uppercase;"">Valor</p>
                                <p style=""margin: 0; color: {corVermelho}; font-size: 28px; font-weight: 700;"">R$ {pedido.TotalPedido:F2}</p>
                            </div>
                        </td>
                    </tr>

                    <!-- Código PIX Copia e Cola -->
                    <tr>
                        <td style=""padding: 0 30px 25px 30px;"">
                            <div style=""background: {corFundo}; padding: 20px; border-radius: 12px; border: 1px solid {corBorda};"">
                                <p style=""margin: 0 0 12px 0; color: {corTexto}; font-size: 14px; font-weight: 600;"">Código PIX (Copia e Cola):</p>
                                <p style=""margin: 0; padding: 12px; background: {corBranco}; border-radius: 8px; color: {corTextoSecundario}; font-size: 11px; word-break: break-all; font-family: monospace; border: 1px solid {corBorda};"">{qrCode}</p>
                            </div>
                        </td>
                    </tr>

                    <!-- Aviso -->
                    <tr>
                        <td style=""padding: 0 30px 30px 30px;"">
                            <div style=""background: rgba(230, 57, 70, 0.1); border-left: 4px solid {corVermelho}; padding: 16px 20px; border-radius: 0 8px 8px 0;"">
                                <p style=""margin: 0; color: {corTexto}; font-size: 14px;"">
                                    ⏰ <strong>Atenção:</strong> O QR Code expira em 30 minutos. Após o pagamento, você receberá a confirmação por email.
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""background: {corFundo}; padding: 30px; text-align: center; border-top: 1px solid {corBorda};"">
                            <p style=""margin: 0; color: {corTextoSecundario}; font-size: 11px;"">
                                © {DateTime.Now.Year} G4 Motocenter. Todos os direitos reservados.
                            </p>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
        ";
    }

    private string FormatarMetodoPagamento(string? metodoPagamento)
    {
        if (string.IsNullOrEmpty(metodoPagamento)) return "-";
        
        return metodoPagamento.ToLower() switch
        {
            "cartao_de_credito" or "credit_card" => "Cartão de Crédito",
            "cartao_de_debito" or "debit_card" => "Cartão de Débito",
            "pix" => "PIX",
            "boleto" => "Boleto",
            _ => metodoPagamento.Replace("_", " ")
        };
    }

    private string ObterMensagemPorStatus(StatusPedido status, string? codigoRastreamento = null)
    {
        return status switch
        {
            StatusPedido.AguardandoConfirmacao => 
                "Recebemos seu pedido e estamos aguardando a confirmação do pagamento. Assim que for confirmado, você receberá uma atualização.",
            
            StatusPedido.EmSeparacao => 
                "Pagamento confirmado! 🎉 Estamos preparando seu pedido com todo carinho. Em breve você receberá o código de rastreamento.",
            
            StatusPedido.ACaminho => 
                "Seu pedido está a caminho! 🚚 Use o código de rastreamento abaixo para acompanhar a entrega em tempo real.",
            
            StatusPedido.Finalizado => 
                "Pedido entregue! 🏍️ Esperamos que você ame seus produtos. Obrigado por escolher a G4 Motocenter!",
            
            StatusPedido.Cancelado => 
                "Infelizmente seu pedido foi cancelado. Se precisar de ajuda ou tiver dúvidas, estamos à disposição.",
            
            _ => "Houve uma atualização no status do seu pedido."
        };
    }

    private string ObterTextoStatus(StatusPedido status)
    {
        return status switch
        {
            StatusPedido.AguardandoConfirmacao => "Aguardando Pagamento",
            StatusPedido.EmSeparacao => "Pagamento Confirmado",
            StatusPedido.ACaminho => "Pedido Enviado",
            StatusPedido.Finalizado => "Entregue",
            StatusPedido.Cancelado => "Cancelado",
            _ => status.ToString()
        };
    }

    #region Email de Devolução

    public async Task EnviarEmailStatusDevolucaoAsync(Devolucao devolucao, DevolucaoStatus novoStatus, string? observacoes = null)
    {
        _logger.LogInformation("[Email] Iniciando envio de email de devolução #{Id}, status: {Status}", devolucao.Id, novoStatus);

        try
        {
            if (string.IsNullOrEmpty(devolucao.Email))
            {
                _logger.LogWarning("[Email] Email não encontrado para devolução #{Id}", devolucao.Id);
                return;
            }

            // Carregar itens se necessário
            if (!devolucao.Itens.Any())
            {
                var devolucaoCompleta = await _context.Devolucoes
                    .Include(d => d.Itens)
                    .Include(d => d.Pedido)
                    .FirstOrDefaultAsync(d => d.Id == devolucao.Id);

                if (devolucaoCompleta != null)
                {
                    devolucao = devolucaoCompleta;
                }
            }

            var assunto = ObterAssuntoDevolucaoPorStatus(novoStatus, devolucao.Id);
            var corpo = GerarCorpoEmailDevolucao(devolucao, novoStatus, observacoes);

            await EnviarEmailAsync(devolucao.Email, assunto, corpo);

            _logger.LogInformation("[Email] ✅ Email de devolução enviado com sucesso para {Email} - Devolução #{Id}, Status: {Status}",
                devolucao.Email, devolucao.Id, novoStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] ❌ Erro ao enviar email de devolução #{Id}", devolucao.Id);
            // Não lançar exceção para não interromper o fluxo principal
        }
    }

    private string ObterAssuntoDevolucaoPorStatus(DevolucaoStatus status, int devolucaoId)
    {
        return status switch
        {
            DevolucaoStatus.Solicitado => $"📦 Solicitação de Devolução #{devolucaoId} Recebida - G4 Motocenter",
            DevolucaoStatus.SolicitacaoEnviada => $"✅ Devolução #{devolucaoId} Aprovada - Código de Postagem Disponível!",
            DevolucaoStatus.Enviado => $"🚚 Devolução #{devolucaoId} em Trânsito - G4 Motocenter",
            DevolucaoStatus.EmAnalise => $"🔍 Devolução #{devolucaoId} Recebida e em Análise - G4 Motocenter",
            DevolucaoStatus.ReembolsoEmitido => $"💰 Reembolso Aprovado! Devolução #{devolucaoId} - G4 Motocenter",
            DevolucaoStatus.Rejeitado => $"❌ Devolução #{devolucaoId} Não Aprovada - G4 Motocenter",
            DevolucaoStatus.Reembolsado => $"🎉 Reembolso Concluído! Devolução #{devolucaoId} - G4 Motocenter",
            _ => $"Atualização da Devolução #{devolucaoId} - G4 Motocenter"
        };
    }

    private string GerarCorpoEmailDevolucao(Devolucao devolucao, DevolucaoStatus status, string? observacoes)
    {
        var mensagem = ObterMensagemDevolucaoPorStatus(status);
        var corStatus = ObterCorStatusDevolucao(status);
        var textoStatus = ObterTextoDevolucaoStatus(status);
        var iconeStatus = ObterIconeStatusDevolucao(status);

        var itensHtml = new StringBuilder();
        var valorTotal = 0m;

        foreach (var item in devolucao.Itens)
        {
            var valorItem = item.PrecoUnitario * item.Quantidade;
            valorTotal += valorItem;
            
            itensHtml.Append($@"
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; vertical-align: middle;'>
                        <strong>{item.ProdutoNome}</strong>
                        {(!string.IsNullOrEmpty(item.GradeDescricao) ? $"<br><span style='color: #666; font-size: 13px;'>{item.GradeDescricao}</span>" : "")}
                    </td>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: center; vertical-align: middle;'>{item.Quantidade}</td>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: right; vertical-align: middle;'>R$ {valorItem:N2}</td>
                </tr>");
        }

        // Timeline do processo
        var timelineHtml = GerarTimelineDevolucao(status);

        // Seção de instruções de postagem (quando aprovado)
        var infoPostagemHtml = "";
        if (status == DevolucaoStatus.SolicitacaoEnviada && !string.IsNullOrEmpty(devolucao.CodigoRastreamento))
        {
            infoPostagemHtml = $@"
                <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 25px; border-radius: 12px; margin: 25px 0; color: white;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <span style='font-size: 48px;'>📦</span>
                        <h3 style='margin: 10px 0 0 0; font-size: 20px;'>Código de Postagem Autorizado</h3>
                    </div>
                    
                    <div style='background-color: rgba(255,255,255,0.95); padding: 20px; border-radius: 8px; text-align: center;'>
                        <p style='margin: 0 0 10px 0; color: #333; font-size: 14px;'>Apresente este código na agência dos Correios:</p>
                        <div style='background-color: #1a1a2e; padding: 15px 25px; border-radius: 8px; display: inline-block;'>
                            <span style='font-family: ''Courier New'', monospace; font-size: 24px; font-weight: bold; color: #c8a050; letter-spacing: 2px;'>
                                {devolucao.CodigoRastreamento}
                            </span>
                        </div>
                        {(devolucao.DataLimitePostagem.HasValue ? $@"
                        <p style='margin: 15px 0 0 0; color: #dc3545; font-weight: bold;'>
                            ⚠️ Data limite para postagem: {devolucao.DataLimitePostagem:dd/MM/yyyy}
                        </p>" : "")}
                    </div>
                </div>
                
                <div style='background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <h4 style='color: #856404; margin: 0 0 15px 0;'>📋 Como enviar seu produto:</h4>
                    <ol style='margin: 0; padding-left: 20px; color: #856404;'>
                        <li style='margin-bottom: 8px;'><strong>Embale</strong> o produto adequadamente para evitar danos durante o transporte</li>
                        <li style='margin-bottom: 8px;'>Vá até uma <strong>agência dos Correios</strong> mais próxima</li>
                        <li style='margin-bottom: 8px;'>Informe o código de postagem acima - <strong>não precisa pagar nada!</strong></li>
                        <li style='margin-bottom: 0;'>Guarde o comprovante de postagem para acompanhar o rastreamento</li>
                    </ol>
                </div>";
        }
        else if (status == DevolucaoStatus.Solicitado)
        {
            infoPostagemHtml = $@"
                <div style='background-color: #e3f2fd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2196f3;'>
                    <h4 style='color: #1565c0; margin: 0 0 10px 0;'>⏳ Aguardando Aprovação</h4>
                    <p style='color: #1565c0; margin: 0;'>
                        Sua solicitação está sendo analisada pela nossa equipe. Assim que for aprovada, 
                        você receberá um novo email com o <strong>código de postagem gratuito</strong> para enviar o produto.
                    </p>
                </div>";
        }
        else if (status == DevolucaoStatus.Enviado && !string.IsNullOrEmpty(devolucao.CodigoRastreamento))
        {
            infoPostagemHtml = $@"
                <div style='background-color: #e8f5e9; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4caf50;'>
                    <h4 style='color: #2e7d32; margin: 0 0 10px 0;'>📍 Acompanhe seu envio</h4>
                    <p style='color: #2e7d32; margin: 0;'>
                        Código de rastreamento: <strong>{devolucao.CodigoRastreamento}</strong>
                    </p>
                    <p style='color: #666; margin: 10px 0 0 0; font-size: 13px;'>
                        Acompanhe em: <a href='https://rastreamento.correios.com.br/app/index.php' style='color: #2196f3;'>rastreamento.correios.com.br</a>
                    </p>
                </div>";
        }
        else if (status == DevolucaoStatus.ReembolsoEmitido || status == DevolucaoStatus.Reembolsado)
        {
            var textoReembolso = status == DevolucaoStatus.ReembolsoEmitido 
                ? "Seu reembolso foi aprovado e está sendo processado. O valor será creditado em até 5 dias úteis."
                : "Seu reembolso foi concluído com sucesso! Verifique sua conta ou cartão de crédito.";
            
            infoPostagemHtml = $@"
                <div style='background-color: #e8f5e9; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4caf50;'>
                    <h4 style='color: #2e7d32; margin: 0 0 10px 0;'>💰 Informações do Reembolso</h4>
                    <p style='color: #2e7d32; margin: 0 0 10px 0;'>{textoReembolso}</p>
                    <p style='color: #2e7d32; margin: 0; font-size: 20px; font-weight: bold;'>
                        Valor: R$ {valorTotal:N2}
                    </p>
                </div>";
        }
        else if (status == DevolucaoStatus.Rejeitado)
        {
            infoPostagemHtml = $@"
                <div style='background-color: #ffebee; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f44336;'>
                    <h4 style='color: #c62828; margin: 0 0 10px 0;'>❌ Solicitação Não Aprovada</h4>
                    <p style='color: #c62828; margin: 0;'>
                        Infelizmente sua solicitação de devolução não foi aprovada. 
                        Se você discorda desta decisão ou tem dúvidas, entre em contato conosco pelo WhatsApp ou email.
                    </p>
                </div>";
        }

        var observacoesHtml = !string.IsNullOrEmpty(observacoes)
            ? $@"<div style='background-color: #f5f5f5; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #9e9e9e;'>
                    <strong style='color: #616161;'>📝 Observações:</strong>
                    <p style='color: #424242; margin: 10px 0 0 0;'>{observacoes}</p>
                </div>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Devolução #{devolucao.Id} - G4 Motocenter</title>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        
        <!-- Header com Logo -->
        <div style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); padding: 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;'>G4 MOTOCENTER</h1>
            <p style='color: #c8a050; margin: 10px 0 0 0; font-size: 14px; letter-spacing: 2px;'>DEVOLUÇÃO DE PRODUTO</p>
        </div>

        <!-- Status Badge Grande -->
        <div style='text-align: center; padding: 30px 20px; background-color: #fafafa; border-bottom: 3px solid {corStatus};'>
            <span style='font-size: 40px; display: block; margin-bottom: 10px;'>{iconeStatus}</span>
            <span style='background-color: {corStatus}; color: white; padding: 12px 30px; border-radius: 30px; font-weight: bold; font-size: 16px; display: inline-block; text-transform: uppercase; letter-spacing: 1px;'>
                {textoStatus}
            </span>
        </div>

        <!-- Conteúdo Principal -->
        <div style='padding: 30px;'>
            <!-- Saudação -->
            <p style='font-size: 18px; margin: 0 0 20px 0;'>
                Olá, <strong>{devolucao.NomeCliente}</strong>! 👋
            </p>
            
            <!-- Mensagem Principal -->
            <p style='font-size: 16px; color: #555; margin: 0 0 25px 0; line-height: 1.8;'>
                {mensagem}
            </p>

            {infoPostagemHtml}
            {observacoesHtml}

            <!-- Timeline do Processo -->
            {timelineHtml}

            <!-- Detalhes da Devolução -->
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 12px; margin: 25px 0;'>
                <h3 style='margin: 0 0 15px 0; color: #1a1a2e; font-size: 16px;'>📋 Dados da Solicitação</h3>
                <table style='width: 100%; font-size: 14px;'>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Nº da Devolução:</td>
                        <td style='padding: 5px 0; text-align: right;'><strong>#{devolucao.Id}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Nº do Pedido:</td>
                        <td style='padding: 5px 0; text-align: right;'><strong>#{devolucao.PedidoId}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Data da Solicitação:</td>
                        <td style='padding: 5px 0; text-align: right;'>{devolucao.DataCriacao:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    {(devolucao.Motivo != null ? $@"
                    <tr>
                        <td style='padding: 5px 0; color: #666;'>Motivo:</td>
                        <td style='padding: 5px 0; text-align: right;'>{devolucao.Motivo}</td>
                    </tr>" : "")}
                </table>
            </div>

            <!-- Itens da Devolução -->
            <h3 style='margin: 25px 0 15px 0; color: #1a1a2e; font-size: 16px;'>🛒 Itens da Devolução</h3>
            <table style='width: 100%; border-collapse: collapse; border-radius: 8px; overflow: hidden;'>
                <thead>
                    <tr style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);'>
                        <th style='padding: 14px; text-align: left; color: white; font-weight: 600;'>Produto</th>
                        <th style='padding: 14px; text-align: center; color: white; font-weight: 600;'>Qtd</th>
                        <th style='padding: 14px; text-align: right; color: white; font-weight: 600;'>Valor</th>
                    </tr>
                </thead>
                <tbody>
                    {itensHtml}
                </tbody>
                <tfoot>
                    <tr style='background-color: #f8f9fa;'>
                        <td colspan='2' style='padding: 14px; text-align: right; font-weight: bold; font-size: 15px;'>Valor Total para Reembolso:</td>
                        <td style='padding: 14px; text-align: right; font-weight: bold; font-size: 18px; color: #c8a050;'>R$ {valorTotal:N2}</td>
                    </tr>
                </tfoot>
            </table>
        </div>

        <!-- Precisa de Ajuda -->
        <div style='background-color: #f8f9fa; padding: 25px 30px; text-align: center;'>
            <p style='margin: 0 0 15px 0; color: #666; font-size: 15px;'>Precisa de ajuda? Estamos aqui para você!</p>
            <a href='https://wa.me/5561999999999' style='display: inline-block; background-color: #25d366; color: white; padding: 12px 25px; border-radius: 25px; text-decoration: none; font-weight: bold; margin-right: 10px;'>
                💬 WhatsApp
            </a>
            <a href='mailto:contato@g4motocenter.com.br' style='display: inline-block; background-color: #1a1a2e; color: white; padding: 12px 25px; border-radius: 25px; text-decoration: none; font-weight: bold;'>
                ✉️ Email
            </a>
        </div>

        <!-- Footer -->
        <div style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); color: #999; padding: 25px 30px; text-align: center;'>
            <p style='margin: 0 0 10px 0; color: #c8a050; font-weight: bold;'>G4 Motocenter</p>
            <p style='margin: 0 0 5px 0; font-size: 13px;'>Q Quadra 10C Rua 13 Lote 11 - Royal Parque</p>
            <p style='margin: 0 0 15px 0; font-size: 13px;'>Águas Lindas de Goiás - GO</p>
            <p style='margin: 0; font-size: 11px; color: #666;'>© 2026 G4 Motocenter - Todos os direitos reservados</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GerarTimelineDevolucao(DevolucaoStatus statusAtual)
    {
        var etapas = new[]
        {
            (DevolucaoStatus.Solicitado, "Solicitação", "📝"),
            (DevolucaoStatus.SolicitacaoEnviada, "Aprovação", "✅"),
            (DevolucaoStatus.Enviado, "Envio", "🚚"),
            (DevolucaoStatus.EmAnalise, "Análise", "🔍"),
            (DevolucaoStatus.ReembolsoEmitido, "Reembolso", "💰")
        };

        // Se rejeitado, não mostra timeline
        if (statusAtual == DevolucaoStatus.Rejeitado)
            return "";

        var html = new StringBuilder();
        html.Append(@"
            <div style='margin: 30px 0;'>
                <h4 style='color: #1a1a2e; margin: 0 0 20px 0; font-size: 14px;'>📊 Progresso da Devolução</h4>
                <div style='display: flex; justify-content: space-between; position: relative;'>");

        for (int i = 0; i < etapas.Length; i++)
        {
            var (etapaStatus, nome, icone) = etapas[i];
            var isCompleto = statusAtual >= etapaStatus;
            var isAtual = statusAtual == etapaStatus;
            
            var corCirculo = isCompleto ? "#28a745" : "#ddd";
            var corTexto = isCompleto ? "#28a745" : "#999";
            var fontWeight = isAtual ? "bold" : "normal";
            
            html.Append($@"
                <div style='text-align: center; flex: 1; position: relative;'>
                    <div style='width: 35px; height: 35px; border-radius: 50%; background-color: {corCirculo}; margin: 0 auto 8px; display: flex; align-items: center; justify-content: center; font-size: 16px;'>
                        {(isCompleto ? "✓" : (i + 1).ToString())}
                    </div>
                    <span style='font-size: 11px; color: {corTexto}; font-weight: {fontWeight};'>{nome}</span>
                </div>");
        }

        html.Append(@"
                </div>
            </div>");

        return html.ToString();
    }

    private string ObterMensagemDevolucaoPorStatus(DevolucaoStatus status)
    {
        return status switch
        {
            DevolucaoStatus.Solicitado =>
                "Recebemos sua solicitação de devolução e ela está em análise pela nossa equipe. " +
                "Fique tranquilo(a), em breve você receberá um novo email com as instruções para envio do produto.",
            
            DevolucaoStatus.SolicitacaoEnviada =>
                "Ótimas notícias! Sua solicitação de devolução foi <strong>aprovada</strong>! 🎉<br><br>" +
                "Geramos um código de postagem gratuito para você enviar o produto pelos Correios. " +
                "Basta ir até uma agência e informar o código abaixo - você não paga nada!",
            
            DevolucaoStatus.Enviado =>
                "Identificamos que o produto foi postado e está a caminho da nossa central. " +
                "Assim que recebermos, iniciaremos a análise das condições do produto.",
            
            DevolucaoStatus.EmAnalise =>
                "Recebemos o produto e nossa equipe está analisando as condições. " +
                "Este processo leva em média 2 dias úteis. Você receberá um email assim que concluirmos.",
            
            DevolucaoStatus.ReembolsoEmitido =>
                "Excelente notícia! Após análise, seu reembolso foi <strong>aprovado</strong>! 💰<br><br>" +
                "O valor será creditado em até 5 dias úteis, dependendo do método de pagamento original.",
            
            DevolucaoStatus.Rejeitado =>
                "Após análise criteriosa, infelizmente não foi possível aprovar sua solicitação de devolução. " +
                "Se você tiver dúvidas ou discordar da decisão, entre em contato conosco para esclarecimentos.",
            
            DevolucaoStatus.Reembolsado =>
                "Seu reembolso foi processado com sucesso e o valor já foi creditado! 🎉<br><br>" +
                "Verifique sua conta bancária ou fatura do cartão de crédito. " +
                "Agradecemos sua paciência durante todo o processo.",
            
            _ => "Houve uma atualização no status da sua devolução. Confira os detalhes abaixo."
        };
    }

    private string ObterCorStatusDevolucao(DevolucaoStatus status)
    {
        return status switch
        {
            DevolucaoStatus.Solicitado => "#6c757d",        // Cinza
            DevolucaoStatus.SolicitacaoEnviada => "#28a745", // Verde (aprovado!)
            DevolucaoStatus.Enviado => "#17a2b8",           // Azul
            DevolucaoStatus.EmAnalise => "#ffc107",         // Amarelo
            DevolucaoStatus.ReembolsoEmitido => "#28a745",  // Verde
            DevolucaoStatus.Rejeitado => "#dc3545",         // Vermelho
            DevolucaoStatus.Reembolsado => "#20c997",       // Verde água
            _ => "#6c757d"
        };
    }

    private string ObterTextoDevolucaoStatus(DevolucaoStatus status)
    {
        return status switch
        {
            DevolucaoStatus.Solicitado => "Aguardando Aprovação",
            DevolucaoStatus.SolicitacaoEnviada => "Aprovada - Envie o Produto",
            DevolucaoStatus.Enviado => "Produto em Trânsito",
            DevolucaoStatus.EmAnalise => "Em Análise",
            DevolucaoStatus.ReembolsoEmitido => "Reembolso Aprovado",
            DevolucaoStatus.Rejeitado => "Não Aprovada",
            DevolucaoStatus.Reembolsado => "Reembolso Concluído",
            _ => status.ToString()
        };
    }

    private string ObterIconeStatusDevolucao(DevolucaoStatus status)
    {
        return status switch
        {
            DevolucaoStatus.Solicitado => "⏳",
            DevolucaoStatus.SolicitacaoEnviada => "✅",
            DevolucaoStatus.Enviado => "🚚",
            DevolucaoStatus.EmAnalise => "🔍",
            DevolucaoStatus.ReembolsoEmitido => "💰",
            DevolucaoStatus.Rejeitado => "❌",
            DevolucaoStatus.Reembolsado => "🎉",
            _ => "📦"
        };
    }

    #endregion
}
