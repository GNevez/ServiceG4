using g4api.Data;
using g4api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilaImpressaoController : ControllerBase
{
    private readonly G4DbContext _context;
    private readonly ILogger<FilaImpressaoController> _logger;

    public FilaImpressaoController(G4DbContext context, ILogger<FilaImpressaoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista itens pendentes na fila de impressão
    /// </summary>
    [HttpGet("pendentes")]
    public async Task<ActionResult<IEnumerable<FilaImpressao>>> GetPendentes()
    {
        var pendentes = await _context.FilaImpressao
            .Where(f => f.Status == StatusImpressao.Pendente)
            .OrderBy(f => f.DataCriacao)
            .Take(10)
            .ToListAsync();

        return Ok(pendentes);
    }

    /// <summary>
    /// Reserva um item da fila para impressão
    /// </summary>
    [HttpPost("reservar/{id}")]
    public async Task<IActionResult> Reservar(int id, [FromQuery] string clienteId)
    {
        var fila = await _context.FilaImpressao.FindAsync(id);

        if (fila == null)
        {
            return NotFound(new { error = "Item não encontrado na fila" });
        }

        if (fila.Status != StatusImpressao.Pendente)
        {
            return BadRequest(new { error = "Item não está pendente" });
        }

        fila.Status = StatusImpressao.EmProcessamento;
        fila.ClienteId = clienteId;
        fila.DataProcessamento = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("[FilaImpressao] Item {Id} reservado pelo cliente {ClienteId}", id, clienteId);

        return Ok(new { message = "Item reservado com sucesso" });
    }

    /// <summary>
    /// Baixa o arquivo PDF de um item da fila
    /// </summary>
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var fila = await _context.FilaImpressao
            .Include(f => f.Rotulo)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fila == null)
        {
            return NotFound(new { error = "Item não encontrado na fila" });
        }

        var filePath = fila.CaminhoArquivo;

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "Arquivo não encontrado no servidor" });
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, "application/pdf", fila.NomeArquivo);
    }

    /// <summary>
    /// Confirma que a impressão foi realizada com sucesso
    /// </summary>
    [HttpPost("confirmar/{id}")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var fila = await _context.FilaImpressao.FindAsync(id);

        if (fila == null)
        {
            return NotFound(new { error = "Item não encontrado na fila" });
        }

        fila.Status = StatusImpressao.Impresso;
        fila.DataImpressao = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("[FilaImpressao] Item {Id} confirmado como impresso", id);

        return Ok(new { message = "Impressão confirmada" });
    }

    /// <summary>
    /// Marca um item como erro na impressão
    /// </summary>
    [HttpPost("erro/{id}")]
    public async Task<IActionResult> MarcarErro(int id, [FromBody] string? mensagem)
    {
        var fila = await _context.FilaImpressao.FindAsync(id);

        if (fila == null)
        {
            return NotFound(new { error = "Item não encontrado na fila" });
        }

        fila.Tentativas++;
        fila.MensagemErro = mensagem;

        if (fila.Tentativas >= fila.MaxTentativas)
        {
            fila.Status = StatusImpressao.Erro;
            _logger.LogWarning("[FilaImpressao] Item {Id} marcado como erro após {Tentativas} tentativas", id, fila.Tentativas);
        }
        else
        {
            fila.Status = StatusImpressao.Pendente; // Volta para pendente para tentar novamente
            _logger.LogInformation("[FilaImpressao] Item {Id} voltou para pendente. Tentativa {Tentativas}/{MaxTentativas}", 
                id, fila.Tentativas, fila.MaxTentativas);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Erro registrado", tentativas = fila.Tentativas });
    }

    /// <summary>
    /// Lista todos os itens da fila com filtros
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FilaImpressao>>> GetAll(
        [FromQuery] StatusImpressao? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.FilaImpressao.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.DataCriacao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }
}
