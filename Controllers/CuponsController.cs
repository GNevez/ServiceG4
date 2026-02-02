using g4api.Data;
using g4api.DTOs;
using g4api.Models;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CuponsController : ControllerBase
{
    private readonly ICupomService _cupomService;
    private readonly G4DbContext _context;

    public CuponsController(ICupomService cupomService, G4DbContext context)
    {
        _cupomService = cupomService;
        _context = context;
    }

    /// <summary>
    /// Lista todos os cupons
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CupomDto>>> GetCupons()
    {
        var cupons = await _context.Cupons
            .OrderByDescending(c => c.DataCriacao)
            .Select(c => new CupomDto
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Descricao = c.Descricao,
                TipoDesconto = c.TipoDesconto,
                ValorDesconto = c.ValorDesconto,
                ValorMinimoCompra = c.ValorMinimoCompra,
                ValorMaximoDesconto = c.ValorMaximoDesconto,
                QuantidadeMaximaUsos = c.QuantidadeMaximaUsos,
                QuantidadeUsosAtual = c.QuantidadeUsosAtual,
                UsosPorUsuario = c.UsoPorUsuario,
                DataInicio = c.DataInicio,
                DataExpiracao = c.DataExpiracao,
                Ativo = c.Ativo
            })
            .ToListAsync();

        return Ok(cupons);
    }

    /// <summary>
    /// Busca um cupom por ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CupomDto>> GetCupomById(int id)
    {
        var cupom = await _context.Cupons.FindAsync(id);

        if (cupom == null)
            return NotFound(new { message = "Cupom não encontrado" });

        return Ok(new CupomDto
        {
            Id = cupom.Id,
            Codigo = cupom.Codigo,
            Descricao = cupom.Descricao,
            TipoDesconto = cupom.TipoDesconto,
            ValorDesconto = cupom.ValorDesconto,
            ValorMinimoCompra = cupom.ValorMinimoCompra,
            ValorMaximoDesconto = cupom.ValorMaximoDesconto,
            QuantidadeMaximaUsos = cupom.QuantidadeMaximaUsos,
            QuantidadeUsosAtual = cupom.QuantidadeUsosAtual,
            UsosPorUsuario = cupom.UsoPorUsuario,
            DataInicio = cupom.DataInicio,
            DataExpiracao = cupom.DataExpiracao,
            Ativo = cupom.Ativo
        });
    }

    /// <summary>
    /// Valida um cupom sem aplicá-lo ao carrinho
    /// </summary>
    [HttpGet("validar/{codigo}")]
    public async Task<ActionResult> ValidarCupom(string codigo, [FromQuery] decimal subtotal, [FromQuery] int? clienteId = null)
    {
        var cupom = await _cupomService.GetCupomByCodigoAsync(codigo);

        if (cupom == null)
            return NotFound(new { message = "Cupom não encontrado" });

        var isValid = await _cupomService.ValidarCupomAsync(codigo, subtotal, clienteId);

        if (!isValid)
            return BadRequest(new { message = "Cupom inválido ou não aplicável a esta compra" });

        var desconto = await _cupomService.CalcularDescontoAsync(codigo, subtotal);

        return Ok(new
        {
            codigo = cupom.Codigo,
            descricao = cupom.Descricao,
            tipoDesconto = cupom.TipoDesconto,
            valorDesconto = cupom.ValorDesconto,
            descontoCalculado = desconto,
            valorMinimoCompra = cupom.ValorMinimoCompra,
            valorMaximoDesconto = cupom.ValorMaximoDesconto,
            dataExpiracao = cupom.DataExpiracao
        });
    }

    /// <summary>
    /// Busca informações de um cupom pelo código
    /// </summary>
    [HttpGet("codigo/{codigo}")]
    public async Task<ActionResult> GetCupomByCodigo(string codigo)
    {
        var cupom = await _cupomService.GetCupomByCodigoAsync(codigo);

        if (cupom == null)
            return NotFound(new { message = "Cupom não encontrado" });

        return Ok(new CupomDto
        {
            Id = cupom.Id,
            Codigo = cupom.Codigo,
            Descricao = cupom.Descricao,
            TipoDesconto = cupom.TipoDesconto,
            ValorDesconto = cupom.ValorDesconto,
            ValorMinimoCompra = cupom.ValorMinimoCompra,
            ValorMaximoDesconto = cupom.ValorMaximoDesconto,
            QuantidadeMaximaUsos = cupom.QuantidadeMaximaUsos,
            QuantidadeUsosAtual = cupom.QuantidadeUsosAtual,
            UsosPorUsuario = cupom.UsoPorUsuario,
            DataInicio = cupom.DataInicio,
            DataExpiracao = cupom.DataExpiracao,
            Ativo = cupom.Ativo
        });
    }

    /// <summary>
    /// Cria um novo cupom
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CupomDto>> CreateCupom([FromBody] CupomCreateDto dto)
    {
        // Verificar se já existe cupom com esse código
        var existente = await _context.Cupons
            .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == dto.Codigo.ToUpper());

        if (existente != null)
            return BadRequest(new { message = "Já existe um cupom com esse código" });

        var cupom = new Cupom
        {
            Codigo = dto.Codigo.ToUpper(),
            Descricao = dto.Descricao,
            TipoDesconto = dto.TipoDesconto,
            ValorDesconto = dto.ValorDesconto,
            ValorMinimoCompra = dto.ValorMinimoCompra,
            ValorMaximoDesconto = dto.ValorMaximoDesconto,
            QuantidadeMaximaUsos = dto.QuantidadeMaximaUsos,
            QuantidadeUsosAtual = 0,
            UsoPorUsuario = dto.UsosPorUsuario ?? 1,
            DataInicio = dto.DataInicio ?? DateTime.Now,
            DataExpiracao = dto.DataExpiracao,
            Ativo = dto.Ativo ?? true,
            DataCriacao = DateTime.Now
        };

        _context.Cupons.Add(cupom);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCupomById), new { id = cupom.Id }, new CupomDto
        {
            Id = cupom.Id,
            Codigo = cupom.Codigo,
            Descricao = cupom.Descricao,
            TipoDesconto = cupom.TipoDesconto,
            ValorDesconto = cupom.ValorDesconto,
            ValorMinimoCompra = cupom.ValorMinimoCompra,
            ValorMaximoDesconto = cupom.ValorMaximoDesconto,
            QuantidadeMaximaUsos = cupom.QuantidadeMaximaUsos,
            QuantidadeUsosAtual = cupom.QuantidadeUsosAtual,
            UsosPorUsuario = cupom.UsoPorUsuario,
            DataInicio = cupom.DataInicio,
            DataExpiracao = cupom.DataExpiracao,
            Ativo = cupom.Ativo
        });
    }

    /// <summary>
    /// Atualiza um cupom existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CupomDto>> UpdateCupom(int id, [FromBody] CupomUpdateDto dto)
    {
        var cupom = await _context.Cupons.FindAsync(id);

        if (cupom == null)
            return NotFound(new { message = "Cupom não encontrado" });

        // Verificar se código já existe em outro cupom
        if (!string.IsNullOrEmpty(dto.Codigo))
        {
            var existente = await _context.Cupons
                .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == dto.Codigo.ToUpper() && c.Id != id);

            if (existente != null)
                return BadRequest(new { message = "Já existe outro cupom com esse código" });

            cupom.Codigo = dto.Codigo.ToUpper();
        }

        if (dto.Descricao != null) cupom.Descricao = dto.Descricao;
        if (dto.TipoDesconto != null) cupom.TipoDesconto = dto.TipoDesconto;
        if (dto.ValorDesconto.HasValue) cupom.ValorDesconto = dto.ValorDesconto.Value;
        if (dto.ValorMinimoCompra.HasValue) cupom.ValorMinimoCompra = dto.ValorMinimoCompra;
        if (dto.ValorMaximoDesconto.HasValue) cupom.ValorMaximoDesconto = dto.ValorMaximoDesconto;
        if (dto.QuantidadeMaximaUsos.HasValue) cupom.QuantidadeMaximaUsos = dto.QuantidadeMaximaUsos;
        if (dto.UsosPorUsuario.HasValue) cupom.UsoPorUsuario = dto.UsosPorUsuario.Value;
        if (dto.DataInicio.HasValue) cupom.DataInicio = dto.DataInicio.Value;
        if (dto.DataExpiracao.HasValue) cupom.DataExpiracao = dto.DataExpiracao;
        if (dto.Ativo.HasValue) cupom.Ativo = dto.Ativo.Value;

        cupom.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new CupomDto
        {
            Id = cupom.Id,
            Codigo = cupom.Codigo,
            Descricao = cupom.Descricao,
            TipoDesconto = cupom.TipoDesconto,
            ValorDesconto = cupom.ValorDesconto,
            ValorMinimoCompra = cupom.ValorMinimoCompra,
            ValorMaximoDesconto = cupom.ValorMaximoDesconto,
            QuantidadeMaximaUsos = cupom.QuantidadeMaximaUsos,
            QuantidadeUsosAtual = cupom.QuantidadeUsosAtual,
            UsosPorUsuario = cupom.UsoPorUsuario,
            DataInicio = cupom.DataInicio,
            DataExpiracao = cupom.DataExpiracao,
            Ativo = cupom.Ativo
        });
    }

    /// <summary>
    /// Exclui um cupom
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCupom(int id)
    {
        var cupom = await _context.Cupons.FindAsync(id);

        if (cupom == null)
            return NotFound(new { message = "Cupom não encontrado" });

        _context.Cupons.Remove(cupom);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Ativa/Desativa um cupom
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult> ToggleCupom(int id)
    {
        var cupom = await _context.Cupons.FindAsync(id);

        if (cupom == null)
            return NotFound(new { message = "Cupom não encontrado" });

        cupom.Ativo = !cupom.Ativo;
        cupom.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { ativo = cupom.Ativo });
    }
}
