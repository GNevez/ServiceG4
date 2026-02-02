using g4api.Data;
using g4api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RotulosController : ControllerBase
{
    private readonly G4DbContext _context;
    private readonly ILogger<RotulosController> _logger;

    public RotulosController(G4DbContext context, ILogger<RotulosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Rotulos
    [HttpGet]
    public async Task<ActionResult<object>> GetRotulos(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? busca = null,
        [FromQuery] int? idPedido = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var query = _context.Rotulos.AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(busca))
            {
                query = query.Where(r =>
                    r.IdRecibo.Contains(busca) ||
                    r.NomeArquivo.Contains(busca) ||
                    (r.CodigosObjeto != null && r.CodigosObjeto.Contains(busca)));
            }

            if (idPedido.HasValue)
            {
                query = query.Where(r => r.IdPedido == idPedido.Value);
            }

            if (dataInicio.HasValue)
            {
                query = query.Where(r => r.DataGeracao >= dataInicio.Value);
            }

            if (dataFim.HasValue)
            {
                var dataFimFinal = dataFim.Value.AddDays(1);
                query = query.Where(r => r.DataGeracao < dataFimFinal);
            }

            var totalCount = await query.CountAsync();

            var rotulos = await query
                .OrderByDescending(r => r.DataGeracao)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.IdPedido,
                    r.IdRecibo,
                    r.IdAtendimento,
                    r.NomeArquivo,
                    r.CaminhoArquivo,
                    r.DataGeracao,
                    r.QuantidadeRotulos,
                    r.CodigosObjeto,
                    r.IdsPrePostagem,
                    r.TipoRotulo,
                    r.FormatoRotulo,
                    r.TamanhoBytes,
                    TamanhoFormatado = FormatarTamanho(r.TamanhoBytes),
                    r.Observacao
                })
                .ToListAsync();

            return Ok(new
            {
                items = rotulos,
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar rótulos");
            return StatusCode(500, new { error = "Erro ao listar rótulos" });
        }
    }

    // GET: api/Rotulos/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult> GetRotulo(int id)
    {
        try
        {
            var rotulo = await _context.Rotulos.FindAsync(id);
            if (rotulo == null)
            {
                return NotFound(new { error = "Rótulo não encontrado" });
            }

            return Ok(new
            {
                rotulo.Id,
                rotulo.IdPedido,
                rotulo.IdRecibo,
                rotulo.IdAtendimento,
                rotulo.NomeArquivo,
                rotulo.CaminhoArquivo,
                rotulo.DataGeracao,
                rotulo.QuantidadeRotulos,
                rotulo.CodigosObjeto,
                rotulo.IdsPrePostagem,
                rotulo.TipoRotulo,
                rotulo.FormatoRotulo,
                rotulo.TamanhoBytes,
                TamanhoFormatado = FormatarTamanho(rotulo.TamanhoBytes),
                rotulo.Observacao
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar rótulo {Id}", id);
            return StatusCode(500, new { error = "Erro ao buscar rótulo" });
        }
    }

    // GET: api/Rotulos/{id}/download
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadRotulo(int id)
    {
        try
        {
            var rotulo = await _context.Rotulos.FindAsync(id);
            if (rotulo == null)
            {
                return NotFound(new { error = "Rótulo não encontrado" });
            }

            var caminhoCompleto = Path.Combine("wwwroot", rotulo.CaminhoArquivo.TrimStart('/'));

            if (!System.IO.File.Exists(caminhoCompleto))
            {
                _logger.LogWarning("Arquivo de rótulo não encontrado: {Path}", caminhoCompleto);
                return NotFound(new { error = "Arquivo de rótulo não encontrado no servidor" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(caminhoCompleto);
            return File(fileBytes, "application/pdf", rotulo.NomeArquivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao baixar rótulo {Id}", id);
            return StatusCode(500, new { error = "Erro ao baixar rótulo" });
        }
    }

    // DELETE: api/Rotulos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRotulo(int id)
    {
        try
        {
            var rotulo = await _context.Rotulos.FindAsync(id);
            if (rotulo == null)
            {
                return NotFound(new { error = "Rótulo não encontrado" });
            }

            // Deletar o arquivo físico
            var caminhoCompleto = Path.Combine("wwwroot", rotulo.CaminhoArquivo.TrimStart('/'));
            if (System.IO.File.Exists(caminhoCompleto))
            {
                System.IO.File.Delete(caminhoCompleto);
                _logger.LogInformation("Arquivo de rótulo deletado: {Path}", caminhoCompleto);
            }

            // Deletar do banco
            _context.Rotulos.Remove(rotulo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar rótulo {Id}", id);
            return StatusCode(500, new { error = "Erro ao deletar rótulo" });
        }
    }

    // GET: api/Rotulos/pedido/{idPedido}
    /// <summary>
    /// Busca rótulos pelo ID do pedido
    /// </summary>
    [HttpGet("pedido/{idPedido}")]
    public async Task<ActionResult<object>> GetRotulosPorPedido(int idPedido)
    {
        try
        {
            var rotulos = await _context.Rotulos
                .Where(r => r.IdPedido == idPedido)
                .OrderByDescending(r => r.DataGeracao)
                .Select(r => new
                {
                    r.Id,
                    r.IdPedido,
                    r.IdRecibo,
                    r.IdAtendimento,
                    r.NomeArquivo,
                    r.CaminhoArquivo,
                    r.DataGeracao,
                    r.QuantidadeRotulos,
                    r.CodigosObjeto,
                    r.IdsPrePostagem,
                    r.TipoRotulo,
                    r.FormatoRotulo,
                    r.TamanhoBytes,
                    TamanhoFormatado = FormatarTamanho(r.TamanhoBytes),
                    r.Observacao
                })
                .ToListAsync();

            return Ok(new
            {
                idPedido,
                total = rotulos.Count,
                items = rotulos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar rótulos do pedido {IdPedido}", idPedido);
            return StatusCode(500, new { error = "Erro ao buscar rótulos do pedido" });
        }
    }

    // GET: api/Rotulos/recibo/{idRecibo}
    /// <summary>
    /// Busca rótulo pelo ID do recibo
    /// </summary>
    [HttpGet("recibo/{idRecibo}")]
    public async Task<ActionResult<object>> GetRotuloPorRecibo(string idRecibo)
    {
        try
        {
            var rotulo = await _context.Rotulos
                .FirstOrDefaultAsync(r => r.IdRecibo == idRecibo);

            if (rotulo == null)
            {
                return NotFound(new { error = "Rótulo não encontrado com este ID de recibo" });
            }

            return Ok(new
            {
                rotulo.Id,
                rotulo.IdPedido,
                rotulo.IdRecibo,
                rotulo.IdAtendimento,
                rotulo.NomeArquivo,
                rotulo.CaminhoArquivo,
                rotulo.DataGeracao,
                rotulo.QuantidadeRotulos,
                rotulo.CodigosObjeto,
                rotulo.IdsPrePostagem,
                rotulo.TipoRotulo,
                rotulo.FormatoRotulo,
                rotulo.TamanhoBytes,
                TamanhoFormatado = FormatarTamanho(rotulo.TamanhoBytes),
                rotulo.Observacao
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar rótulo pelo recibo {IdRecibo}", idRecibo);
            return StatusCode(500, new { error = "Erro ao buscar rótulo pelo recibo" });
        }
    }

    private static string FormatarTamanho(long bytes)
    {
        string[] tamanhos = { "B", "KB", "MB", "GB" };
        double tamanho = bytes;
        int ordem = 0;
        while (tamanho >= 1024 && ordem < tamanhos.Length - 1)
        {
            ordem++;
            tamanho /= 1024;
        }
        return $"{tamanho:0.##} {tamanhos[ordem]}";
    }
}
