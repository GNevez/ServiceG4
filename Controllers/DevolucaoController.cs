using g4api.Data;
using g4api.Models;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevolucaoController : ControllerBase
{
    private readonly G4DbContext _context;
    private readonly ICorreiosService _correiosService;
    private readonly IEmailService _emailService;
    private readonly ILogger<DevolucaoController> _logger;
    private readonly IConfiguration _configuration;

    public DevolucaoController(
        G4DbContext context,
        ICorreiosService correiosService,
        IEmailService emailService,
        ILogger<DevolucaoController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _correiosService = correiosService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    #region DTOs

    public class CriarDevolucaoItemDto
    {
        public int ItemCarrinhoId { get; set; }
        public int Quantidade { get; set; }
    }

    public class CriarDevolucaoDto
    {
        public int PedidoId { get; set; }
        public string Cpf { get; set; } = null!;
        public string NomeCliente { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Motivo { get; set; }
        public List<CriarDevolucaoItemDto> Itens { get; set; } = new();
    }

    public class AtualizarStatusDto
    {
        public DevolucaoStatus Status { get; set; }
        public string? Observacoes { get; set; }
        public bool EnviarEmail { get; set; } = true;
    }

    public class AtualizarDadosCorreiosDto
    {
        public string? CodigoPostagem { get; set; }
        public string? CodigoRastreamento { get; set; }
        public DateTime? DataLimitePostagem { get; set; }
        public string? UrlEtiqueta { get; set; }
        public DevolucaoStatus? NovoStatus { get; set; }
        public bool EnviarEmail { get; set; } = true;
        public string? Observacoes { get; set; }
    }

    public class DevolucaoResponseDto
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public string? CodigoPedido { get; set; }
        public string Cpf { get; set; } = null!;
        public string NomeCliente { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefone { get; set; }
        public string? Motivo { get; set; }
        public string? Observacoes { get; set; }
        public DevolucaoStatus Status { get; set; }
        public string StatusNome => Status.ToString();
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAtualizacao { get; set; }

        // Dados Correios
        public string? IdPrePostagem { get; set; }
        public string? CodigoPostagem { get; set; }
        public string? CodigoRastreamento { get; set; }
        public DateTime? DataLimitePostagem { get; set; }
        public string? UrlEtiqueta { get; set; }

        public List<DevolucaoItemResponseDto> Itens { get; set; } = new();
    }

    public class DevolucaoItemResponseDto
    {
        public int Id { get; set; }
        public int DevolucaoId { get; set; }
        public int ItemCarrinhoId { get; set; }
        public int ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = null!;
        public int? GradeId { get; set; }
        public string? GradeDescricao { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
    }

    #endregion

    /// <summary>
    /// Cria uma nova solicitação de devolução (status: Solicitado)
    /// A pré-postagem reversa só é gerada quando o funcionário aprovar (mudar para SolicitacaoEnviada)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DevolucaoResponseDto>> Criar([FromBody] CriarDevolucaoDto dto)
    {
        try
        {
            if (dto == null) return BadRequest(new { message = "Dados inválidos" });
            if (dto.PedidoId <= 0) return BadRequest(new { message = "PedidoId inválido" });
            if (string.IsNullOrWhiteSpace(dto.Cpf)) return BadRequest(new { message = "CPF é obrigatório" });
            if (string.IsNullOrWhiteSpace(dto.NomeCliente)) return BadRequest(new { message = "Nome é obrigatório" });
            if (string.IsNullOrWhiteSpace(dto.Email)) return BadRequest(new { message = "Email é obrigatório" });
            if (dto.Itens == null || dto.Itens.Count == 0) return BadRequest(new { message = "Selecione ao menos um item" });

            var pedido = await _context.Pedidos
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .Include(p => p.EnderecoEntrega)
                .FirstOrDefaultAsync(p => p.Id == dto.PedidoId);

            if (pedido == null) return NotFound(new { message = "Pedido não encontrado" });

            // Validação básica do CPF
            var cpfLimpo = dto.Cpf.Replace(".", "").Replace("-", "").Trim();
            if (!string.IsNullOrEmpty(pedido.CpfCliente))
            {
                var cpfPedido = pedido.CpfCliente.Replace(".", "").Replace("-", "").Trim();
                if (!string.Equals(cpfPedido, cpfLimpo, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "CPF não corresponde ao titular do pedido" });
                }
            }

            // Criar devolução
            var devolucao = new Devolucao
            {
                PedidoId = pedido.Id,
                Cpf = cpfLimpo,
                NomeCliente = dto.NomeCliente,
                Email = dto.Email,
                Telefone = dto.Telefone,
                Motivo = dto.Motivo,
                Status = DevolucaoStatus.Solicitado,
                DataCriacao = DateTime.UtcNow
            };

            // Adicionar itens
            foreach (var itemDto in dto.Itens)
            {
                var itemPedido = pedido.Carrinho.Itens.FirstOrDefault(i => i.Id == itemDto.ItemCarrinhoId);
                if (itemPedido == null) 
                    return BadRequest(new { message = $"Item {itemDto.ItemCarrinhoId} não pertence ao pedido" });
                
                if (itemDto.Quantidade <= 0 || itemDto.Quantidade > itemPedido.Quantidade)
                    return BadRequest(new { message = $"Quantidade inválida para o item {itemDto.ItemCarrinhoId}" });

                devolucao.Itens.Add(new DevolucaoItem
                {
                    ItemCarrinhoId = itemPedido.Id,
                    ProdutoId = itemPedido.ProdutoId,
                    ProdutoNome = itemPedido.Produto?.TituloEcommerceProduto ?? "Produto",
                    GradeId = itemPedido.GradeId,
                    GradeDescricao = itemPedido.Grade != null 
                        ? $"{itemPedido.Grade.TamanhoProduto} - {itemPedido.Grade.CorPredominanteProduto}"
                        : null,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = itemPedido.PrecoUnitario
                });
            }

            _context.Devolucoes.Add(devolucao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Devolucao] Devolução {DevolucaoId} criada para pedido {PedidoId} com status Solicitado", 
                devolucao.Id, pedido.Id);

            // Enviar email de confirmação da solicitação (sem código de postagem ainda)
            try
            {
                await _emailService.EnviarEmailStatusDevolucaoAsync(devolucao, DevolucaoStatus.Solicitado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Devolucao] Erro ao enviar email de devolução {DevolucaoId}", devolucao.Id);
            }

            return Ok(MapToResponseDto(devolucao, pedido.CodigoPedido));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao criar devolução");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lista todas as devoluções com filtros e paginação
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> Listar(
        [FromQuery] DevolucaoStatus? status,
        [FromQuery] string? busca,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Devolucoes
                .Include(d => d.Itens)
                .Include(d => d.Pedido)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                query = query.Where(d => 
                    d.NomeCliente.Contains(busca) ||
                    d.Email.Contains(busca) ||
                    d.Cpf.Contains(busca) ||
                    d.CodigoRastreamento!.Contains(busca) ||
                    d.Pedido.CodigoPedido.Contains(busca));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(d => d.DataCriacao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DevolucaoResponseDto
                {
                    Id = d.Id,
                    PedidoId = d.PedidoId,
                    CodigoPedido = d.Pedido.CodigoPedido,
                    Cpf = d.Cpf,
                    NomeCliente = d.NomeCliente,
                    Email = d.Email,
                    Telefone = d.Telefone,
                    Motivo = d.Motivo,
                    Observacoes = d.Observacoes,
                    Status = d.Status,
                    DataCriacao = d.DataCriacao,
                    DataAtualizacao = d.DataAtualizacao,
                    IdPrePostagem = d.IdPrePostagem,
                    CodigoPostagem = d.CodigoPostagem,
                    CodigoRastreamento = d.CodigoRastreamento,
                    DataLimitePostagem = d.DataLimitePostagem,
                    UrlEtiqueta = d.UrlEtiqueta,
                    Itens = d.Itens.Select(i => new DevolucaoItemResponseDto
                    {
                        Id = i.Id,
                        DevolucaoId = i.DevolucaoId,
                        ItemCarrinhoId = i.ItemCarrinhoId,
                        ProdutoId = i.ProdutoId,
                        ProdutoNome = i.ProdutoNome,
                        GradeId = i.GradeId,
                        GradeDescricao = i.GradeDescricao,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalCount = totalCount,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao listar devoluções");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém uma devolução específica por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DevolucaoResponseDto>> ObterPorId(int id)
    {
        try
        {
            var devolucao = await _context.Devolucoes
                .Include(d => d.Itens)
                .Include(d => d.Pedido)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devolucao == null)
                return NotFound(new { message = "Devolução não encontrada" });

            return Ok(MapToResponseDto(devolucao, devolucao.Pedido?.CodigoPedido));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao obter devolução {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }


    [HttpPut("{id}/status")]
    public async Task<ActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusDto dto)
    {
        try
        {
            if (dto == null) return BadRequest(new { message = "Dados inválidos" });

            var devolucao = await _context.Devolucoes
                .Include(d => d.Itens)
                .Include(d => d.Pedido)
                    .ThenInclude(p => p.EnderecoEntrega)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devolucao == null) return NotFound(new { message = "Devolução não encontrada" });

            var statusAntigo = devolucao.Status;
            
            if (dto.Status == DevolucaoStatus.SolicitacaoEnviada && 
                string.IsNullOrEmpty(devolucao.CodigoRastreamento))
            {
                _logger.LogInformation("[Devolucao] Aprovando devolução {Id}, gerando pré-postagem reversa...", id);
                
                if (devolucao.Pedido == null)
                {
                    // Carregar pedido se necessário
                    devolucao.Pedido = await _context.Pedidos
                        .Include(p => p.EnderecoEntrega)
                        .FirstOrDefaultAsync(p => p.Id == devolucao.PedidoId);
                }

                if (devolucao.Pedido == null)
                {
                    _logger.LogWarning("[Devolucao] Pedido não encontrado para gerar pré-postagem reversa");
                    return BadRequest(new { message = "Pedido não encontrado. Não é possível gerar pré-postagem." });
                }

                // Tenta gerar a pré-postagem - se falhar, retorna erro e NÃO atualiza o status
                try
                {
                    await GerarPrePostagemReversaAsync(devolucao, devolucao.Pedido);
                    
                    if (string.IsNullOrEmpty(devolucao.CodigoRastreamento))
                    {
                        return BadRequest(new { message = "Falha ao gerar pré-postagem. Código de rastreamento não foi gerado." });
                    }
                    
                    _logger.LogInformation("[Devolucao] Pré-postagem reversa gerada com sucesso. Código: {Codigo}", 
                        devolucao.CodigoRastreamento);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Devolucao] Erro ao gerar pré-postagem reversa para devolução {Id}", id);
                    return BadRequest(new { message = $"Erro ao gerar pré-postagem: {ex.Message}" });
                }
            }

            // Enviar email ANTES de salvar o status (se habilitado e status mudou)
            if (statusAntigo != dto.Status && dto.EnviarEmail)
            {
                _logger.LogInformation("[Devolucao] Enviando email para devolução {Id}, status: {Status}", 
                    id, dto.Status);
                
                try
                {
                    // Atualiza temporariamente o status para o email mostrar o status correto
                    devolucao.Status = dto.Status;
                    await _emailService.EnviarEmailStatusDevolucaoAsync(devolucao, dto.Status, dto.Observacoes);
                    _logger.LogInformation("[Devolucao] Email enviado com sucesso para {Email}", devolucao.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Devolucao] Erro ao enviar email de status para {Email}", devolucao.Email);
                    // Reverte o status antes de retornar erro
                    devolucao.Status = statusAntigo;
                    return BadRequest(new { message = $"Erro ao enviar email: {ex.Message}" });
                }
            }

            // Agora que pré-postagem e email foram enviados com sucesso, atualiza o status
            devolucao.Status = dto.Status;
            devolucao.Observacoes = dto.Observacoes ?? devolucao.Observacoes;
            devolucao.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Devolucao] Status da devolução {Id} alterado de {StatusAntigo} para {StatusNovo}", 
                id, statusAntigo, dto.Status);

            return Ok(new { 
                message = "Status atualizado com sucesso", 
                status = devolucao.Status.ToString(),
                codigoRastreamento = devolucao.CodigoRastreamento,
                idPrePostagem = devolucao.IdPrePostagem,
                emailEnviado = dto.EnviarEmail && statusAntigo != dto.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao atualizar status da devolução {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza dados de logística reversa dos Correios
    /// </summary>
    [HttpPut("{id}/correios")]
    public async Task<ActionResult> AtualizarDadosCorreios(int id, [FromBody] AtualizarDadosCorreiosDto dto)
    {
        try
        {
            if (dto == null) return BadRequest(new { message = "Dados inválidos" });

            var devolucao = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devolucao == null) return NotFound(new { message = "Devolução não encontrada" });

            // Atualizar dados dos Correios
            if (!string.IsNullOrEmpty(dto.CodigoPostagem))
                devolucao.CodigoPostagem = dto.CodigoPostagem;

            if (!string.IsNullOrEmpty(dto.CodigoRastreamento))
                devolucao.CodigoRastreamento = dto.CodigoRastreamento;

            if (dto.DataLimitePostagem.HasValue)
                devolucao.DataLimitePostagem = dto.DataLimitePostagem;

            if (!string.IsNullOrEmpty(dto.UrlEtiqueta))
                devolucao.UrlEtiqueta = dto.UrlEtiqueta;

            var statusAntigo = devolucao.Status;

            // Atualizar status se fornecido
            if (dto.NovoStatus.HasValue)
            {
                devolucao.Status = dto.NovoStatus.Value;
            }

            devolucao.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Enviar email se o status mudou e o envio estiver habilitado
            if (dto.EnviarEmail && dto.NovoStatus.HasValue && statusAntigo != dto.NovoStatus.Value)
            {
                try
                {
                    await _emailService.EnviarEmailStatusDevolucaoAsync(devolucao, dto.NovoStatus.Value, dto.Observacoes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Devolucao] Erro ao enviar email de status");
                }
            }

            return Ok(new
            {
                message = "Dados atualizados com sucesso",
                status = devolucao.Status.ToString(),
                codigoPostagem = devolucao.CodigoPostagem,
                codigoRastreamento = devolucao.CodigoRastreamento
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao atualizar dados correios da devolução {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gera manualmente a pré-postagem reversa para uma devolução
    /// </summary>
    [HttpPost("{id}/gerar-postagem-reversa")]
    public async Task<ActionResult> GerarPostagemReversa(int id)
    {
        try
        {
            var devolucao = await _context.Devolucoes
                .Include(d => d.Itens)
                .Include(d => d.Pedido)
                    .ThenInclude(p => p.EnderecoEntrega)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devolucao == null) return NotFound(new { message = "Devolução não encontrada" });

            if (!string.IsNullOrEmpty(devolucao.IdPrePostagem))
            {
                return BadRequest(new { message = "Pré-postagem reversa já foi gerada", idPrePostagem = devolucao.IdPrePostagem });
            }

            await GerarPrePostagemReversaAsync(devolucao, devolucao.Pedido);

            return Ok(new
            {
                message = "Pré-postagem reversa gerada com sucesso",
                idPrePostagem = devolucao.IdPrePostagem,
                codigoRastreamento = devolucao.CodigoRastreamento,
                dataLimitePostagem = devolucao.DataLimitePostagem
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao gerar pré-postagem reversa para devolução {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Exclui uma devolução (apenas se estiver no status Solicitado)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Excluir(int id)
    {
        try
        {
            var devolucao = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devolucao == null) return NotFound(new { message = "Devolução não encontrada" });

            if (devolucao.Status != DevolucaoStatus.Solicitado)
            {
                return BadRequest(new { message = "Apenas devoluções no status 'Solicitado' podem ser excluídas" });
            }

            _context.Devolucoes.Remove(devolucao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Devolucao] Devolução {Id} excluída", id);

            return Ok(new { message = "Devolução excluída com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao excluir devolução {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reenvia o email de status da devolução
    /// </summary>
    [HttpPost("{id}/reenviar-email")]
    public async Task<ActionResult> ReenviarEmail(int id, [FromQuery] string? email = null)
    {
        try
        {
            var devolucao = await _context.Devolucoes
                .Include(d => d.Itens)
                .Include(d => d.Pedido)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (devolucao == null) return NotFound(new { message = "Devolução não encontrada" });

            // Se foi passado um email de teste, usar ele temporariamente
            var emailOriginal = devolucao.Email;
            if (!string.IsNullOrEmpty(email))
            {
                devolucao.Email = email;
            }

            _logger.LogInformation("[Devolucao] Reenviando email para devolução {Id} para {Email}, status: {Status}", 
                id, devolucao.Email, devolucao.Status);

            await _emailService.EnviarEmailStatusDevolucaoAsync(devolucao, devolucao.Status);

            // Restaurar email original se necessário
            if (!string.IsNullOrEmpty(email))
            {
                devolucao.Email = emailOriginal;
            }

            return Ok(new { 
                message = "Email reenviado com sucesso", 
                email = string.IsNullOrEmpty(email) ? emailOriginal : email,
                status = devolucao.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Devolucao] Erro ao reenviar email da devolução {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    #region Métodos Privados

    /// <summary>
    /// Gera pré-postagem reversa nos Correios
    /// Na logística reversa, o remetente é o CLIENTE e o destinatário é a LOJA
    /// </summary>
    private async Task GerarPrePostagemReversaAsync(Devolucao devolucao, Pedido pedido)
    {
        if (pedido.EnderecoEntrega == null)
        {
            throw new InvalidOperationException("Pedido sem endereço de entrega");
        }

        var config = _configuration;
        var endereco = pedido.EnderecoEntrega;

        // Telefone do cliente (remetente na logística reversa)
        var telefoneClienteRaw = devolucao.Telefone?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "") ?? "";
        var dddCliente = telefoneClienteRaw.Length >= 2 ? telefoneClienteRaw.Substring(0, 2) : "";
        var telefoneCliente = telefoneClienteRaw.Length > 2 ? telefoneClienteRaw.Substring(2) : "";
        if (telefoneCliente.Length > 9) telefoneCliente = telefoneCliente.Substring(0, 9);

        // Telefone da loja (destinatário na logística reversa)
        var telefoneLoja = config["Correios:RemetenteTelefone"]?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "") ?? "";
        var dddLoja = telefoneLoja.Length >= 2 ? telefoneLoja.Substring(0, 2) : "";
        var telLoja = telefoneLoja.Length > 2 ? telefoneLoja.Substring(2) : "";
        if (telLoja.Length > 9) telLoja = telLoja.Substring(0, 9);

        // Calcular valor dos itens para declaração
        var valorItens = devolucao.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);
        var quantidadeItens = devolucao.Itens.Sum(i => i.Quantidade);

        // Na logística reversa:
        // - REMETENTE = Cliente (quem vai enviar o produto de volta)
        // - DESTINATÁRIO = Loja (quem vai receber o produto)
        var payload = new
        {
            idCorreios = Guid.NewGuid().ToString(),
            remetente = new
            {
                nome = devolucao.NomeCliente,
                cpfCnpj = devolucao.Cpf.Replace(".", "").Replace("-", ""),
                dddCelular = dddCliente,
                celular = telefoneCliente,
                email = devolucao.Email,
                endereco = new
                {
                    cep = endereco.Cep?.Replace("-", ""),
                    logradouro = endereco.Logradouro,
                    numero = endereco.Numero,
                    complemento = endereco.Complemento ?? "",
                    bairro = endereco.Bairro,
                    cidade = endereco.Cidade,
                    uf = endereco.Uf
                }
            },
            destinatario = new
            {
                nome = config["Correios:RemetenteNome"],
                cpfCnpj = config["Correios:RemetenteCPFCNPJ"]?.Replace(".", "").Replace("-", "").Replace("/", ""),
                dddCelular = dddLoja,
                celular = telLoja,
                email = config["Correios:RemetenteEmail"],
                endereco = new
                {
                    cep = config["Correios:RemetenteCEP"]?.Replace("-", ""),
                    logradouro = config["Correios:RemetenteLogradouro"],
                    numero = config["Correios:RemetenteNumero"],
                    bairro = config["Correios:RemetenteBairro"],
                    cidade = config["Correios:RemetenteCidade"],
                    uf = config["Correios:RemetenteUF"]
                }
            },
            codigoServico = config["Correios:CodigoServicoReversa"] ?? "03220", // PAC Reversa
            numeroCartaoPostagem = config["Correios:CartaoPostagem"],
            pesoInformado = "1000", // 1kg padrão para devolução
            codigoFormatoObjetoInformado = "2", // Pacote
            alturaInformada = "10",
            larguraInformada = "20",
            comprimentoInformado = "30",
            cienteObjetoNaoProibido = 1,
            // Para logística reversa, usar código de serviço específico OU serviço normal sem flag
            // Códigos de Logística Reversa: 04391 (PAC Reversa), 04227 (SEDEX Reversa)
            // Se usar serviço normal (03220, 03298), a flag logisticaReversa deve ser "N"
            logisticaReversa = "N", // Usando serviço normal - cliente paga na origem ou loja autoriza
            itensDeclaracaoConteudo = new[]
            {
                new
                {
                    conteudo = "Devolução de Peças e Acessórios",
                    quantidade = quantidadeItens.ToString(),
                    valor = valorItens.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                }
            },
            listaServicoAdicional = new[]
            {
                new 
                { 
                    codigoServicoAdicional = "019",
                    tipoServicoAdicional = "AR",
                    valorDeclarado = valorItens.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                }
            },
            observacao = $"Devolução #{devolucao.Id} - Pedido #{pedido.CodigoPedido}"
        };

        _logger.LogInformation("[Devolucao] Criando pré-postagem reversa para devolução {DevolucaoId}...", devolucao.Id);

        // Chamar API dos Correios
        var resultado = await _correiosService.CriarPrePostagemGenericaAsync(payload);

        if (resultado != null && resultado.Sucesso)
        {
            devolucao.IdPrePostagem = resultado.IdPrePostagem;
            devolucao.CodigoRastreamento = resultado.CodigoRastreamento;
            devolucao.DataLimitePostagem = resultado.PrazoPostagem;
            devolucao.RespostaCorreiosJson = resultado.RespostaJson;
            devolucao.Status = DevolucaoStatus.SolicitacaoEnviada;
            devolucao.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Devolucao] Pré-postagem reversa criada: {CodigoRastreamento}", resultado.CodigoRastreamento);

            // Enviar email com código de postagem
            try
            {
                await _emailService.EnviarEmailStatusDevolucaoAsync(devolucao, DevolucaoStatus.SolicitacaoEnviada);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Devolucao] Erro ao enviar email de pré-postagem");
            }
        }
        else
        {
            throw new InvalidOperationException($"Erro ao criar pré-postagem reversa: {resultado?.Mensagem ?? "Erro desconhecido"}");
        }
    }

    private DevolucaoResponseDto MapToResponseDto(Devolucao devolucao, string? codigoPedido)
    {
        return new DevolucaoResponseDto
        {
            Id = devolucao.Id,
            PedidoId = devolucao.PedidoId,
            CodigoPedido = codigoPedido,
            Cpf = devolucao.Cpf,
            NomeCliente = devolucao.NomeCliente,
            Email = devolucao.Email,
            Telefone = devolucao.Telefone,
            Motivo = devolucao.Motivo,
            Observacoes = devolucao.Observacoes,
            Status = devolucao.Status,
            DataCriacao = devolucao.DataCriacao,
            DataAtualizacao = devolucao.DataAtualizacao,
            IdPrePostagem = devolucao.IdPrePostagem,
            CodigoPostagem = devolucao.CodigoPostagem,
            CodigoRastreamento = devolucao.CodigoRastreamento,
            DataLimitePostagem = devolucao.DataLimitePostagem,
            UrlEtiqueta = devolucao.UrlEtiqueta,
            Itens = devolucao.Itens.Select(i => new DevolucaoItemResponseDto
            {
                Id = i.Id,
                DevolucaoId = i.DevolucaoId,
                ItemCarrinhoId = i.ItemCarrinhoId,
                ProdutoId = i.ProdutoId,
                ProdutoNome = i.ProdutoNome,
                GradeId = i.GradeId,
                GradeDescricao = i.GradeDescricao,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario
            }).ToList()
        };
    }

    #endregion
}
