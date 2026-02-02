using g4api.Data;
using g4api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoController : ControllerBase
{
    private readonly G4DbContext _context;
    private readonly ILogger<PedidoController> _logger;

    public PedidoController(G4DbContext context, ILogger<PedidoController> logger)
    {
        _context = context;
        _logger = logger;
    }


    /// </summary>
    [HttpGet("by-cpf")]
    public async Task<IActionResult> GetByCpf([FromQuery] string cpf)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return BadRequest(new { message = "CPF é obrigatório" });
            }

            var cpfLimpo = cpf.Replace(".", "").Replace("-", "").Replace(" ", "");

            var pedidos = await _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .Where(p => p.CpfCliente != null && 
                       p.CpfCliente.Replace(".", "").Replace("-", "").Replace(" ", "") == cpfLimpo)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            if (!pedidos.Any())
            {
                return Ok(new List<object>()); 
            }

            return Ok(pedidos.Select(MapPedidoToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao buscar pedidos por CPF");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("codigo/{codigo}")]
    public async Task<IActionResult> GetByCodigo(string codigo)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .FirstOrDefaultAsync(p => p.CodigoPedido == codigo);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido não encontrado" });
            }

            return Ok(MapPedidoToResponse(pedido));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao buscar pedido {Codigo}", codigo);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new { message = "Pedido não encontrado" });
            }

            return Ok(MapPedidoToResponse(pedido));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao buscar pedido {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .OrderByDescending(p => p.DataPedido);

            var totalCount = await query.CountAsync();
            var pedidos = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                items = pedidos.Select(MapPedidoToResponse),
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao listar pedidos");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("status/{status:int}")]
    public async Task<IActionResult> ListByStatus(int status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!Enum.IsDefined(typeof(StatusPedido), status))
            {
                return BadRequest(new { message = "Status inválido" });
            }

            var statusEnum = (StatusPedido)status;

            var query = _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Produto)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .Where(p => p.Status == statusEnum)
                .OrderByDescending(p => p.DataPedido);

            var totalCount = await query.CountAsync();
            var pedidos = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                items = pedidos.Select(MapPedidoToResponse),
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao listar pedidos por status {Status}", status);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound(new { message = "Pedido não encontrado" });
            }

            if (!Enum.IsDefined(typeof(StatusPedido), request.Status))
            {
                return BadRequest(new { message = "Status inválido" });
            }

            pedido.Status = (StatusPedido)request.Status;
            pedido.DataAtualizacao = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.CodigoRastreamento))
            {
                pedido.CodigoRastreamento = request.CodigoRastreamento;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status atualizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao atualizar status do pedido {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound(new { message = "Pedido não encontrado" });
            }

            pedido.Status = StatusPedido.Cancelado;
            pedido.MotivoCancelamento = request.MotivoCancelamento;
            pedido.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido cancelado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pedido] Erro ao cancelar pedido {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private static object MapPedidoToResponse(Pedido pedido)
    {
        return new
        {
            id = pedido.Id,
            codigoPedido = pedido.CodigoPedido,
            status = (int)pedido.Status,
            statusNome = pedido.Status.ToString(),
            metodoPagamento = pedido.MetodoPagamento,
            mercadoPagoPaymentId = pedido.MercadoPagoPaymentId,
            precoFrete = pedido.PrecoFrete,
            descontoCupom = pedido.DescontoCupom,
            descontoPorUnidade = 0m,
            totalPedido = pedido.TotalPedido,
            dataPedido = pedido.DataPedido,
            dataAtualizacao = pedido.DataAtualizacao,
            codigoRastreamento = pedido.CodigoRastreamento,
            observacoes = pedido.Observacoes,
            motivoCancelamento = pedido.MotivoCancelamento,
            clienteNome = pedido.NomeCliente,
            clienteEmail = pedido.EmailCliente,
            clienteTelefone = pedido.TelefoneCliente,
            clienteCpf = pedido.CpfCliente,
            enderecoEntrega = pedido.EnderecoEntrega != null ? new
            {
                id = pedido.EnderecoEntrega.Id,
                cep = pedido.EnderecoEntrega.Cep,
                logradouro = pedido.EnderecoEntrega.Logradouro,
                numero = pedido.EnderecoEntrega.Numero,
                complemento = pedido.EnderecoEntrega.Complemento,
                bairro = pedido.EnderecoEntrega.Bairro,
                cidade = pedido.EnderecoEntrega.Cidade,
                estado = pedido.EnderecoEntrega.Uf,
                nomeDestinatario = pedido.EnderecoEntrega.NomeDestinatario,
                telefoneDestinatario = pedido.EnderecoEntrega.TelefoneDestinatario
            } : null,
            itens = (pedido.Carrinho?.Itens ?? []).Select(i => new
            {
                id = i.Id,
                produtoId = i.ProdutoId,
                produtoNome = i.Produto?.TituloEcommerceProduto ?? "Produto",
                produtoSlug = "",
                produtoPreco = i.PrecoUnitario,
                produtoImagem = i.Grade?.Img ?? i.Produto?.Img,
                corId = i.GradeId,
                corNome = i.Grade?.CorPredominanteProduto ?? "",
                tamanho = i.Grade?.TamanhoProduto,
                quantidade = i.Quantidade,
                precoTotalItem = i.Quantidade * i.PrecoUnitario
            }).ToList()
        };
    }
}

public class UpdateStatusRequest
{
    public int Status { get; set; }
    public string? CodigoRastreamento { get; set; }
}

public class CancelOrderRequest
{
    public string? MotivoCancelamento { get; set; }
}
