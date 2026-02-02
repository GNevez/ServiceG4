using g4api.Data;
using g4api.DTOs;
using g4api.Models;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly ICartService _cartService;
    private readonly G4DbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IEmailService _emailService;
    private readonly ICorreiosService _correiosService;
    private readonly RotuloAutomaticoService _rotuloAutomaticoService;

    public PaymentsController(
        IMercadoPagoService mercadoPagoService,
        ICartService cartService,
        G4DbContext context,
        IConfiguration configuration,
        ILogger<PaymentsController> logger,
        IEmailService emailService,
        ICorreiosService correiosService,
        RotuloAutomaticoService rotuloAutomaticoService)
    {
        _mercadoPagoService = mercadoPagoService;
        _cartService = cartService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _correiosService = correiosService;
        _rotuloAutomaticoService = rotuloAutomaticoService;
    }

    /// <summary>
    /// Obtém a public key do Mercado Pago para uso no frontend
    /// </summary>
    [HttpGet("public-key")]
    public ActionResult<object> GetPublicKey()
    {
        try
        {
            var publicKey = _mercadoPagoService.GetPublicKey();
            return Ok(new { publicKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error getting public key");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cria um novo pedido e processa o pagamento
    /// </summary>
    [HttpPost("create-order")]
    public async Task<ActionResult<PagamentoResponseDto>> CreateOrder([FromBody] CriarPagamentoDto dto)
    {
        try
        {
            // Obter token do carrinho
            var cartToken = Request.Cookies["cart_token"];
            if (string.IsNullOrEmpty(cartToken))
            {
                return BadRequest(new { message = "Token do carrinho não encontrado" });
            }

            // Buscar carrinho
            var carrinho = await _context.Carrinhos
                .Include(c => c.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(c => c.Cupom)
                .FirstOrDefaultAsync(c => c.Token == cartToken && c.Ativo);

            if (carrinho == null || !carrinho.Itens.Any())
            {
                return BadRequest(new { message = "Carrinho não encontrado ou vazio" });
            }

            // Calcular totais do backend (valor base SEM juros)
            var subtotal = carrinho.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
            var descontoCupom = carrinho.CupomDesconto;
            var frete = dto.PrecoFrete ?? 0;
            var totalCalculadoBackend = subtotal - descontoCupom + frete;
            
            // IMPORTANTE: O Mercado Pago sempre recebe o valor BASE (sem juros)
            // Os juros são calculados automaticamente pelo MP com base nas parcelas
            decimal total = totalCalculadoBackend;
            
            _logger.LogInformation("[Payments] Creating order - Subtotal: {Subtotal}, Desconto: {Desconto}, Frete: {Frete}, Total: {Total}, Parcelas: {Parcelas}",
                subtotal, descontoCupom, frete, total, dto.GetParcelas());

            // Gerar código do pedido
            var codigoPedido = GerarCodigoPedido();

            // Separar nome em primeiro e último nome
            var nomePartes = dto.Nome.Split(' ', 2);
            var firstName = nomePartes[0];
            var lastName = nomePartes.Length > 1 ? nomePartes[1] : "";

            MercadoPagoPaymentResponse paymentResponse;

            if (dto.MetodoPagamento?.ToLower() == "pix")
            {
                // Pagamento via PIX
                var pixRequest = new MercadoPagoPixRequest
                {
                    TransactionAmount = total,
                    Description = $"Pedido #{codigoPedido} - G4 Motocenter",
                    PaymentMethodId = "pix",
                    ExternalReference = codigoPedido,
                    Payer = new MercadoPagoPayer
                    {
                        Email = dto.Email,
                        FirstName = firstName,
                        LastName = lastName,
                        Identification = new MercadoPagoIdentification
                        {
                            Type = "CPF",
                            Number = dto.Cpf.Replace(".", "").Replace("-", "")
                        },
                        Address = new MercadoPagoAddress
                        {
                            ZipCode = dto.Cep.Replace("-", ""),
                            StreetName = dto.Logradouro,
                            StreetNumber = dto.Numero,
                            Neighborhood = dto.Bairro,
                            City = dto.Cidade,
                            FederalUnit = dto.Estado
                        }
                    }
                };

                paymentResponse = await _mercadoPagoService.CreatePixPaymentAsync(pixRequest);
            }
            else
            {
                // Pagamento via cartão de crédito
                if (string.IsNullOrEmpty(dto.CardToken))
                {
                    return BadRequest(new { message = "Token do cartão é obrigatório" });
                }

                // Usar o payment_method_id retornado pela consulta de installments
                var paymentMethodId = dto.PaymentMethodId ?? "visa"; // Fallback para visa se não informado
                
                _logger.LogInformation("[Payments] Using PaymentMethodId: {PaymentMethodId}", paymentMethodId);

                var cardRequest = new MercadoPagoPaymentRequest
                {
                    TransactionAmount = total,
                    Token = dto.CardToken,
                    Description = $"Pedido #{codigoPedido} - G4 Motocenter",
                    Installments = dto.GetParcelas(),
                    PaymentMethodId = paymentMethodId,
                    ExternalReference = codigoPedido,
                    Payer = new MercadoPagoPayer
                    {
                        Email = dto.Email,
                        FirstName = firstName,
                        LastName = lastName,
                        Identification = new MercadoPagoIdentification
                        {
                            Type = "CPF",
                            Number = dto.Cpf.Replace(".", "").Replace("-", "")
                        },
                        Address = new MercadoPagoAddress
                        {
                            ZipCode = dto.Cep.Replace("-", ""),
                            StreetName = dto.Logradouro,
                            StreetNumber = dto.Numero,
                            Neighborhood = dto.Bairro,
                            City = dto.Cidade,
                            FederalUnit = dto.Estado
                        }
                    },
                    AdditionalInfo = new MercadoPagoAdditionalInfo
                    {
                        Items = carrinho.Itens.Select(i => new MercadoPagoItem
                        {
                            Id = i.ProdutoId.ToString(),
                            Title = i.Produto?.TituloEcommerceProduto ?? "Produto",
                            Quantity = i.Quantidade,
                            UnitPrice = i.PrecoUnitario
                        }).ToList(),
                        Shipments = new MercadoPagoShipments
                        {
                            ReceiverAddress = new MercadoPagoReceiverAddress
                            {
                                ZipCode = dto.Cep.Replace("-", ""),
                                StreetName = dto.Logradouro,
                                StreetNumber = dto.Numero,
                                Apartment = dto.Complemento
                            }
                        }
                    }
                };

                // Log detalhado do request
                _logger.LogInformation("[Payments] ===== CARD PAYMENT REQUEST =====");
                _logger.LogInformation("[Payments] TransactionAmount: {Amount}", cardRequest.TransactionAmount);
                _logger.LogInformation("[Payments] Token: {Token}", cardRequest.Token?.Substring(0, Math.Min(20, cardRequest.Token?.Length ?? 0)) + "...");
                _logger.LogInformation("[Payments] Installments: {Installments}", cardRequest.Installments);
                _logger.LogInformation("[Payments] PaymentMethodId: {PaymentMethodId}", cardRequest.PaymentMethodId);
                _logger.LogInformation("[Payments] Payer Email: {Email}", cardRequest.Payer?.Email);
                _logger.LogInformation("[Payments] Payer Name: {FirstName} {LastName}", cardRequest.Payer?.FirstName, cardRequest.Payer?.LastName);
                _logger.LogInformation("[Payments] Payer CPF: {CPF}", cardRequest.Payer?.Identification?.Number);
                _logger.LogInformation("[Payments] ================================");

                paymentResponse = await _mercadoPagoService.CreateCardPaymentAsync(cardRequest);
            }

            _logger.LogInformation("[Payments] Payment created - ID: {PaymentId}, Status: {Status}, StatusDetail: {StatusDetail}",
                paymentResponse.Id, paymentResponse.Status, paymentResponse.StatusDetail);

            // Se o pagamento foi rejeitado imediatamente, retornar erro
            if (paymentResponse.Status == "rejected")
            {
                _logger.LogWarning("[Payments] Payment rejected immediately - Reason: {StatusDetail}", paymentResponse.StatusDetail);
                return BadRequest(new { 
                    message = $"Pagamento rejeitado: {GetRejectReasonMessage(paymentResponse.StatusDetail)}",
                    statusDetail = paymentResponse.StatusDetail
                });
            }

            // Criar endereço de entrega
            var endereco = new EnderecoEntrega
            {
                Cep = dto.Cep?.Replace("-", "") ?? "",
                Logradouro = dto.Logradouro ?? "",
                Numero = dto.Numero ?? "",
                Complemento = dto.Complemento,
                Bairro = dto.Bairro ?? "",
                Cidade = dto.Cidade ?? "",
                Uf = dto.Estado ?? "",
                NomeDestinatario = dto.Nome,
                TelefoneDestinatario = dto.Telefone,
                DataCriacao = DateTime.UtcNow
            };
            _context.EnderecosEntrega.Add(endereco);
            await _context.SaveChangesAsync();

            // Calcular o total que será cobrado (com juros se houver)
            // O paymentResponse.TransactionAmount contém o valor final cobrado pelo MP
            var totalCobrado = dto.TotalAmountMercadoPago ?? total;

            // Criar pedido no banco de dados
            var pedido = new Pedido
            {
                CodigoPedido = codigoPedido,
                CarrinhoId = carrinho.Id,
                EnderecoEntregaId = endereco.Id,
                Status = paymentResponse.Status == "approved" ? StatusPedido.EmSeparacao : StatusPedido.AguardandoConfirmacao,
                PrecoFrete = dto.PrecoFrete,
                TotalPedido = totalCobrado, // Valor total cobrado (com juros se houver)
                DescontoCupom = carrinho.CupomDesconto,
                DataPedido = DateTime.UtcNow,
                MetodoPagamento = dto.MetodoPagamento,
                MercadoPagoPaymentId = paymentResponse.Id.ToString(),
                NomeCliente = dto.Nome,
                EmailCliente = dto.Email,
                TelefoneCliente = dto.Telefone,
                CpfCliente = dto.Cpf
            };
            _context.Pedidos.Add(pedido);

            // Marcar carrinho como finalizado
            carrinho.Status = StatusCarrinho.Finalizado;
            carrinho.DataAtualizacao = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Payments] Order saved - Codigo: {Codigo}, PedidoId: {PedidoId}", 
                codigoPedido, pedido.Id);
            
            if (dto.MetodoPagamento == "pix" && paymentResponse.PointOfInteraction?.TransactionData != null)
            {
                await _emailService.EnviarEmailPixAsync(
                    pedido,
                    paymentResponse.PointOfInteraction.TransactionData.QrCode ?? "",
                    paymentResponse.PointOfInteraction.TransactionData.QrCodeBase64 ?? "");
            }
            else
            {
                await _emailService.EnviarEmailConfirmacaoPedidoAsync(pedido);
            }

            // Montar resposta
            var response = new PagamentoResponseDto
            {
                OrderId = codigoPedido,
                OrderCode = codigoPedido,
                MercadoPagoPaymentId = paymentResponse.Id,
                Status = paymentResponse.Status,
                StatusDetail = paymentResponse.StatusDetail,
                Total = total,
                Parcelas = paymentResponse.Installments
            };

            // Adicionar dados do PIX se aplicável
            if (paymentResponse.PointOfInteraction?.TransactionData != null)
            {
                response.Pix = new PixInfoDto
                {
                    QrCode = paymentResponse.PointOfInteraction.TransactionData.QrCode,
                    QrCodeBase64 = paymentResponse.PointOfInteraction.TransactionData.QrCodeBase64,
                    TicketUrl = paymentResponse.PointOfInteraction.TransactionData.TicketUrl
                };
            }

            // Se pagamento aprovado ou pendente, limpar cookie do carrinho
            if (paymentResponse.Status == "approved" || paymentResponse.Status == "pending" || paymentResponse.Status == "in_process")
            {
                // Deletar com as mesmas opções que foram usadas para criar o cookie
                var isHttps = Request.IsHttps;
                Response.Cookies.Delete("cart_token", new CookieOptions
                {
                    Path = "/",
                    HttpOnly = true,
                    Secure = isHttps,
                    SameSite = SameSiteMode.Lax
                });
                _logger.LogInformation("[Payments] Cart cookie deleted for order {CodigoPedido}", codigoPedido);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error creating order");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Busca um pagamento pelo ID do Mercado Pago
    /// </summary>
    [HttpGet("payment/{paymentId}")]
    public async Task<ActionResult<MercadoPagoPaymentResponse>> GetPayment(long paymentId)
    {
        try
        {
            var payment = await _mercadoPagoService.GetPaymentAsync(paymentId);
            if (payment == null)
            {
                return NotFound(new { message = "Pagamento não encontrado" });
            }
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error getting payment {PaymentId}", paymentId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Busca métodos de pagamento disponíveis
    /// </summary>
    [HttpGet("payment-methods")]
    public async Task<ActionResult<List<MetodoPagamentoDto>>> GetPaymentMethods()
    {
        try
        {
            var methods = await _mercadoPagoService.GetPaymentMethodsAsync();
            return Ok(methods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error getting payment methods");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Busca opções de parcelamento para um valor
    /// </summary>
    [HttpGet("installments")]
    public async Task<ActionResult<ParcelasDto>> GetInstallments([FromQuery] decimal amount, [FromQuery] string? paymentMethodId = null, [FromQuery] string? bin = null)
    {
        try
        {
            var installments = await _mercadoPagoService.GetInstallmentsAsync(amount, paymentMethodId, bin);
            if (installments == null)
            {
                return NotFound(new { message = "Não foi possível obter opções de parcelamento" });
            }
            return Ok(installments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error getting installments");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Webhook do Mercado Pago para notificações de pagamento
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> MercadoPagoWebhook()
    {
        try
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync();

            _logger.LogInformation("[Webhook] Received notification: {Json}", json);

            var webhookSecret = _configuration["MercadoPago:WebhookSecret"];
            if (!string.IsNullOrEmpty(webhookSecret))
            {
                var xSignature = Request.Headers["x-signature"].FirstOrDefault();
                var xRequestId = Request.Headers["x-request-id"].FirstOrDefault();
                
                if (!string.IsNullOrEmpty(xSignature))
                {
                    var parts = xSignature.Split(',');
                    var ts = parts.FirstOrDefault(p => p.StartsWith("ts="))?.Substring(3);
                    var v1 = parts.FirstOrDefault(p => p.StartsWith("v1="))?.Substring(3);
                    
                    if (!string.IsNullOrEmpty(ts) && !string.IsNullOrEmpty(v1))
                    {
                        var notification = JsonSerializer.Deserialize<MercadoPagoWebhookNotification>(json,
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                        
                        var dataId = notification?.Data?.Id ?? "";
                        
                        var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";
                        
                        using var hmac = new System.Security.Cryptography.HMACSHA256(
                            System.Text.Encoding.UTF8.GetBytes(webhookSecret));
                        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(manifest));
                        var calculatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        
                        if (calculatedSignature != v1)
                        {
                            _logger.LogWarning("[Webhook] Invalid signature. Expected: {Expected}, Got: {Got}", 
                                calculatedSignature, v1);
                        }
                        else
                        {
                            _logger.LogInformation("[Webhook] Signature validated successfully");
                        }
                    }
                }
            }

            var webhookNotification = JsonSerializer.Deserialize<MercadoPagoWebhookNotification>(json, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            if (webhookNotification == null)
            {
                return BadRequest(new { error = "Invalid notification" });
            }

            _logger.LogInformation("[Webhook] Notification Type: {Type}, Action: {Action}, DataId: {DataId}", 
                webhookNotification.Type, webhookNotification.Action, webhookNotification.Data?.Id);

            // Processar notificações de pagamento (type=payment ou action contém payment)
            var isPaymentNotification = webhookNotification.Type?.Equals("payment", StringComparison.OrdinalIgnoreCase) == true ||
                                        webhookNotification.Action?.Contains("payment", StringComparison.OrdinalIgnoreCase) == true;
            
            if (isPaymentNotification && webhookNotification.Data?.Id != null)
            {
                var paymentId = long.Parse(webhookNotification.Data.Id);
                var payment = await _mercadoPagoService.GetPaymentAsync(paymentId);

                if (payment != null)
                {
                    _logger.LogInformation("[Webhook] Payment {PaymentId} - Status: '{Status}', ExternalRef: {ExternalRef}",
                        payment.Id, payment.Status, payment.ExternalReference);

                    var codigoPedido = payment.ExternalReference;

                    var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.CodigoPedido == codigoPedido);
                    
                    if (pedido == null)
                    {
                        _logger.LogWarning("[Webhook] Order not found: {CodigoPedido}", codigoPedido);
                        return Ok(); 
                    }

                    // Normalizar status para lowercase
                    var status = payment.Status?.ToLowerInvariant() ?? "";
                    _logger.LogInformation("[Webhook] Processing status '{Status}' for order {CodigoPedido}", status, codigoPedido);

                    switch (status)
                    {
                        case "approved":
                            _logger.LogInformation("[Webhook] Payment approved for order {CodigoPedido}", codigoPedido);
                            pedido.Status = StatusPedido.EmSeparacao;
                            pedido.MercadoPagoPaymentId = payment.Id.ToString();

                            // Geração automática de pré-postagem, rótulo e fila de impressão
                            try
                            {
                                // 1. Criar pré-postagem se não existir
                                var prePostagemExistente = await _context.PrePostagens
                                    .FirstOrDefaultAsync(p => p.PedidoId == pedido.Id && 
                                        p.Status != StatusPrePostagem.Cancelada && 
                                        p.Status != StatusPrePostagem.Erro);
                                
                                if (prePostagemExistente == null)
                                {
                                    _logger.LogInformation("[Webhook] Criando pré-postagem para pedido {PedidoId}", pedido.Id);
                                    var prePostagemDto = new CriarPrePostagemDto { PedidoId = pedido.Id };
                                    await _correiosService.CriarPrePostagemAsync(prePostagemDto);
                                }

                                // 2. Gerar rótulo e adicionar à fila de impressão
                                var rotulo = await _rotuloAutomaticoService.GerarRotuloParaPedidoAsync(pedido.Id);
                                if (rotulo != null)
                                {
                                    _logger.LogInformation("[Webhook] Rótulo gerado para pedido {PedidoId}, adicionando à fila de impressão", pedido.Id);
                                    await _rotuloAutomaticoService.AdicionarFilaImpressaoAsync(rotulo, pedido.Id);
                                }
                                else
                                {
                                    _logger.LogWarning("[Webhook] Não foi possível gerar rótulo para pedido {PedidoId}", pedido.Id);
                                }
                            }
                            catch (Exception rotuloEx)
                            {
                                _logger.LogError(rotuloEx, "[Webhook] Erro ao gerar rótulo/fila automaticamente para pedido {PedidoId}", pedido.Id);
                            }
                            break;
                        
                        case "rejected":
                            _logger.LogInformation("[Webhook] Payment rejected for order {CodigoPedido}", codigoPedido);
                            pedido.Status = StatusPedido.Cancelado;
                            pedido.MotivoCancelamento = $"Pagamento rejeitado: {payment.StatusDetail}";
                            break;
                        
                        case "cancelled":
                        case "refunded":
                        case "charged_back":
                            _logger.LogInformation("[Webhook] Payment {Status} for order {CodigoPedido}", payment.Status, codigoPedido);
                            pedido.Status = StatusPedido.Cancelado;
                            pedido.MotivoCancelamento = $"Pagamento {payment.Status}";
                            break;
                        
                        case "pending":
                        case "in_process":
                        case "in_mediation":
                            _logger.LogInformation("[Webhook] Payment pending for order {CodigoPedido}", codigoPedido);
                            pedido.Status = StatusPedido.AguardandoConfirmacao;
                            break;
                        
                        default:
                            _logger.LogWarning("[Webhook] Unhandled payment status '{Status}' for order {CodigoPedido}", status, codigoPedido);
                            break;
                    }

                    pedido.DataAtualizacao = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("[Webhook] Order {CodigoPedido} updated to status {Status}", 
                        codigoPedido, pedido.Status);
                    
                    await _emailService.EnviarEmailStatusPedidoAsync(pedido, pedido.Status);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Webhook] Error processing webhook");
            return Ok();
        }
    }

    private string GerarCodigoPedido()
    {
        var random = new Random();
        var numero = random.Next(10000, 99999);
        return $"G4-{DateTime.Now:yyyyMMdd}-{numero}";
    }

    private string GetRejectReasonMessage(string? statusDetail)
    {
        return statusDetail switch
        {
            "cc_rejected_bad_filled_card_number" => "Número do cartão inválido",
            "cc_rejected_bad_filled_date" => "Data de validade inválida",
            "cc_rejected_bad_filled_other" => "Dados do cartão inválidos",
            "cc_rejected_bad_filled_security_code" => "Código de segurança inválido",
            "cc_rejected_blacklist" => "Cartão na lista de restrições",
            "cc_rejected_call_for_authorize" => "Ligue para o banco para autorizar",
            "cc_rejected_card_disabled" => "Cartão desabilitado",
            "cc_rejected_card_error" => "Erro no cartão",
            "cc_rejected_duplicated_payment" => "Pagamento duplicado",
            "cc_rejected_high_risk" => "Pagamento recusado por risco alto",
            "cc_rejected_insufficient_amount" => "Saldo insuficiente",
            "cc_rejected_invalid_installments" => "Parcelas inválidas para este cartão",
            "cc_rejected_max_attempts" => "Limite de tentativas excedido",
            "cc_rejected_other_reason" => "Pagamento recusado pelo banco",
            _ => statusDetail ?? "Pagamento recusado"
        };
    }

    /// <summary>
    /// Busca um pedido pelo código para exibição do PIX
    /// </summary>
    [HttpGet("order-by-code/{codigoPedido}")]
    public async Task<ActionResult<object>> GetOrderByCode(string codigoPedido)
    {
        try
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.CodigoPedido == codigoPedido);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido não encontrado" });
            }

            // Se tem MercadoPagoPaymentId, buscar dados do pagamento no Mercado Pago
            if (!string.IsNullOrEmpty(pedido.MercadoPagoPaymentId) && 
                long.TryParse(pedido.MercadoPagoPaymentId, out var paymentId))
            {
                try
                {
                    var payment = await _mercadoPagoService.GetPaymentAsync(paymentId);
                    if (payment != null)
                    {
                        var response = new
                        {
                            codigoPedido = pedido.CodigoPedido,
                            metodoPagamento = pedido.MetodoPagamento,
                            totalPedido = pedido.TotalPedido,
                            status = payment.Status,
                            statusDetail = payment.StatusDetail,
                            pix = payment.PointOfInteraction?.TransactionData != null ? new
                            {
                                qrCode = payment.PointOfInteraction.TransactionData.QrCode,
                                qrCodeBase64 = payment.PointOfInteraction.TransactionData.QrCodeBase64,
                                ticketUrl = payment.PointOfInteraction.TransactionData.TicketUrl
                            } : null
                        };

                        return Ok(response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Payments] Error fetching from Mercado Pago: {Message}", ex.Message);
                }
            }

            // Fallback: retornar dados básicos
            return Ok(new
            {
                codigoPedido = pedido.CodigoPedido,
                metodoPagamento = pedido.MetodoPagamento,
                totalPedido = pedido.TotalPedido,
                status = pedido.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payments] Error getting order by code: {Message}", ex.Message);
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
