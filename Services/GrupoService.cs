using g4api.Data;
using g4api.DTOs;
using g4api.Extensions;
using Microsoft.EntityFrameworkCore;

namespace g4api.Services;

public class GrupoService : IGrupoService
{
    private readonly G4DbContext _context;

    public GrupoService(G4DbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GrupoDto>> GetAllGruposAsync()
    {
        var grupos = await _context.ProdutoGrupos
            .Include(g => g.SubGrupos)
            .OrderBy(g => g.DescricaoGrupoProduto)
            .ToListAsync();

        return grupos.Select(g => new GrupoDto
        {
            Id = g.IdProdutoGrupo,
            Nome = g.DescricaoGrupoProduto ?? "",
            Slug = (g.DescricaoGrupoProduto ?? "").GenerateSlug(),
            Img = g.Img,
            SubGrupos = g.SubGrupos
                .OrderBy(s => s.DescricaoSubGrupoProduto)
                .Select(s => new SubGrupoDto
                {
                    Id = s.IdProdutoSubGrupo,
                    Nome = s.DescricaoSubGrupoProduto ?? "",
                    Slug = (s.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
                    Img = s.Img
                }).ToList()
        });
    }

    public async Task<GrupoDto?> GetGrupoByIdAsync(int id)
    {
        var grupo = await _context.ProdutoGrupos
            .Include(g => g.SubGrupos)
            .FirstOrDefaultAsync(g => g.IdProdutoGrupo == id);

        if (grupo == null)
            return null;

        return new GrupoDto
        {
            Id = grupo.IdProdutoGrupo,
            Nome = grupo.DescricaoGrupoProduto ?? "",
            Slug = (grupo.DescricaoGrupoProduto ?? "").GenerateSlug(),
            Img = grupo.Img,
            SubGrupos = grupo.SubGrupos
                .OrderBy(s => s.DescricaoSubGrupoProduto)
                .Select(s => new SubGrupoDto
                {
                    Id = s.IdProdutoSubGrupo,
                    Nome = s.DescricaoSubGrupoProduto ?? "",
                    Slug = (s.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
                    Img = s.Img
                }).ToList()
        };
    }

    public async Task<IEnumerable<SubGrupoDto>> GetSubGruposByGrupoIdAsync(int grupoId)
    {
        var subGrupos = await _context.ProdutoSubGrupos
            .Where(s => s.IdProdutoGrupo == grupoId)
            .OrderBy(s => s.DescricaoSubGrupoProduto)
            .ToListAsync();

        return subGrupos.Select(s => new SubGrupoDto
        {
            Id = s.IdProdutoSubGrupo,
            Nome = s.DescricaoSubGrupoProduto ?? "",
            Slug = (s.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
            Img = s.Img
        });
    }

    public async Task<IEnumerable<SubGrupoDto>> GetAllSubGruposAsync()
    {
        var subGrupos = await _context.ProdutoSubGrupos
            .Include(s => s.Grupo)
            .OrderBy(s => s.DescricaoSubGrupoProduto)
            .ToListAsync();

        return subGrupos.Select(s => new SubGrupoDto
        {
            Id = s.IdProdutoSubGrupo,
            Nome = s.DescricaoSubGrupoProduto ?? "",
            Slug = (s.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
            Img = s.Img
        });
    }

    public async Task<SubGrupoDto?> GetSubGrupoBySlugOrIdAsync(string slugOrId)
    {
        // Tenta buscar por ID primeiro
        if (int.TryParse(slugOrId, out int id))
        {
            var subGrupoById = await _context.ProdutoSubGrupos
                .FirstOrDefaultAsync(s => s.IdProdutoSubGrupo == id);

            if (subGrupoById != null)
            {
                return new SubGrupoDto
                {
                    Id = subGrupoById.IdProdutoSubGrupo,
                    Nome = subGrupoById.DescricaoSubGrupoProduto ?? "",
                    Slug = (subGrupoById.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
                    Img = subGrupoById.Img
                };
            }
        }

        // Busca por slug (comparando com o nome normalizado)
        var subGrupos = await _context.ProdutoSubGrupos.ToListAsync();
        var subGrupo = subGrupos.FirstOrDefault(s => 
            (s.DescricaoSubGrupoProduto ?? "").GenerateSlug().Equals(slugOrId, StringComparison.OrdinalIgnoreCase));

        if (subGrupo == null)
            return null;

        return new SubGrupoDto
        {
            Id = subGrupo.IdProdutoSubGrupo,
            Nome = subGrupo.DescricaoSubGrupoProduto ?? "",
            Slug = (subGrupo.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
            Img = subGrupo.Img
        };
    }

    public async Task<GrupoDto?> GetGrupoBySlugOrIdAsync(string slugOrId)
    {
        // Tenta buscar por ID primeiro
        if (int.TryParse(slugOrId, out int id))
        {
            return await GetGrupoByIdAsync(id);
        }

        var grupos = await _context.ProdutoGrupos
            .Include(g => g.SubGrupos)
            .ToListAsync();
            
        var grupo = grupos.FirstOrDefault(g => 
            (g.DescricaoGrupoProduto ?? "").GenerateSlug().Equals(slugOrId, StringComparison.OrdinalIgnoreCase));

        if (grupo == null)
            return null;

        return new GrupoDto
        {
            Id = grupo.IdProdutoGrupo,
            Nome = grupo.DescricaoGrupoProduto ?? "",
            Slug = (grupo.DescricaoGrupoProduto ?? "").GenerateSlug(),
            Img = grupo.Img,
            SubGrupos = grupo.SubGrupos
                .OrderBy(s => s.DescricaoSubGrupoProduto)
                .Select(s => new SubGrupoDto
                {
                    Id = s.IdProdutoSubGrupo,
                    Nome = s.DescricaoSubGrupoProduto ?? "",
                    Slug = (s.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
                    Img = s.Img
                }).ToList()
        };
    }

    public async Task<SubGrupoDto?> GetSubGrupoBySlugInGrupoAsync(string grupoSlug, string subGrupoSlug)
    {
        // Primeiro encontra o grupo
        var grupos = await _context.ProdutoGrupos.ToListAsync();
        var grupo = grupos.FirstOrDefault(g => 
            (g.DescricaoGrupoProduto ?? "").GenerateSlug().Equals(grupoSlug, StringComparison.OrdinalIgnoreCase));

        if (grupo == null)
            return null;

        // Busca o subgrupo dentro desse grupo
        var subGrupos = await _context.ProdutoSubGrupos
            .Where(s => s.IdProdutoGrupo == grupo.IdProdutoGrupo)
            .ToListAsync();
            
        var subGrupo = subGrupos.FirstOrDefault(s => 
            (s.DescricaoSubGrupoProduto ?? "").GenerateSlug().Equals(subGrupoSlug, StringComparison.OrdinalIgnoreCase));

        if (subGrupo == null)
            return null;

        return new SubGrupoDto
        {
            Id = subGrupo.IdProdutoSubGrupo,
            Nome = subGrupo.DescricaoSubGrupoProduto ?? "",
            Slug = (subGrupo.DescricaoSubGrupoProduto ?? "").GenerateSlug(),
            Img = subGrupo.Img
        };
    }
}
