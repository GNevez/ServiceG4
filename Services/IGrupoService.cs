using g4api.DTOs;

namespace g4api.Services;

public interface IGrupoService
{
    Task<IEnumerable<GrupoDto>> GetAllGruposAsync();
    Task<GrupoDto?> GetGrupoByIdAsync(int id);
    Task<GrupoDto?> GetGrupoBySlugOrIdAsync(string slugOrId);
    Task<IEnumerable<SubGrupoDto>> GetSubGruposByGrupoIdAsync(int grupoId);
    Task<IEnumerable<SubGrupoDto>> GetAllSubGruposAsync();
    Task<SubGrupoDto?> GetSubGrupoBySlugOrIdAsync(string slugOrId);
    Task<SubGrupoDto?> GetSubGrupoBySlugInGrupoAsync(string grupoSlug, string subGrupoSlug);
}
