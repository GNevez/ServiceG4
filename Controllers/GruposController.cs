using g4api.DTOs;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GruposController : ControllerBase
{
    private readonly IGrupoService _grupoService;

    public GruposController(IGrupoService grupoService)
    {
        _grupoService = grupoService;
    }

    /// <summary>
    /// Retorna todos os grupos com seus subgrupos
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GrupoDto>>> GetGrupos()
    {
        var grupos = await _grupoService.GetAllGruposAsync();
        return Ok(grupos);
    }

    /// <summary>
    /// Retorna um grupo específico por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GrupoDto>> GetGrupo(int id)
    {
        var grupo = await _grupoService.GetGrupoByIdAsync(id);
        
        if (grupo == null)
            return NotFound(new { message = $"Grupo com ID {id} não encontrado" });

        return Ok(grupo);
    }

    /// <summary>
    /// Retorna os subgrupos de um grupo específico
    /// </summary>
    [HttpGet("{id}/subgrupos")]
    public async Task<ActionResult<IEnumerable<SubGrupoDto>>> GetSubGrupos(int id)
    {
        var grupo = await _grupoService.GetGrupoByIdAsync(id);
        
        if (grupo == null)
            return NotFound(new { message = $"Grupo com ID {id} não encontrado" });

        var subGrupos = await _grupoService.GetSubGruposByGrupoIdAsync(id);
        return Ok(subGrupos);
    }

    /// <summary>
    /// Retorna todas as coleções (subgrupos)
    /// </summary>
    [HttpGet("colecoes")]
    public async Task<ActionResult<IEnumerable<SubGrupoDto>>> GetColecoes()
    {
        var colecoes = await _grupoService.GetAllSubGruposAsync();
        return Ok(colecoes);
    }

    /// <summary>
    /// Retorna uma coleção (subgrupo) por slug ou ID
    /// </summary>
    [HttpGet("colecoes/{slugOrId}")]
    public async Task<ActionResult<SubGrupoDto>> GetColecao(string slugOrId)
    {
        var colecao = await _grupoService.GetSubGrupoBySlugOrIdAsync(slugOrId);
        
        if (colecao == null)
            return NotFound(new { message = $"Coleção '{slugOrId}' não encontrada" });

        return Ok(colecao);
    }

    /// <summary>
    /// Retorna um grupo por slug ou ID (para página de collection)
    /// </summary>
    [HttpGet("grupo/{slugOrId}")]
    public async Task<ActionResult<GrupoDto>> GetGrupoBySlug(string slugOrId)
    {
        var grupo = await _grupoService.GetGrupoBySlugOrIdAsync(slugOrId);
        
        if (grupo == null)
            return NotFound(new { message = $"Grupo '{slugOrId}' não encontrado" });

        return Ok(grupo);
    }

    /// <summary>
    /// Retorna um subgrupo específico dentro de um grupo (para página de subgrupo)
    /// </summary>
    [HttpGet("grupo/{grupoSlug}/subgrupo/{subGrupoSlug}")]
    public async Task<ActionResult<SubGrupoDto>> GetSubGrupoInGrupo(string grupoSlug, string subGrupoSlug)
    {
        var subGrupo = await _grupoService.GetSubGrupoBySlugInGrupoAsync(grupoSlug, subGrupoSlug);
        
        if (subGrupo == null)
            return NotFound(new { message = $"Subgrupo '{subGrupoSlug}' não encontrado no grupo '{grupoSlug}'" });

        return Ok(subGrupo);
    }
}
