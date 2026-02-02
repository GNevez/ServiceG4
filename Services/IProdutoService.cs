using g4api.DTOs;
using Microsoft.AspNetCore.Http;

namespace g4api.Services;

public class CorrigirCaminhosResult
{
    public string Message { get; set; } = "";
    public int ImagensCorrigidas { get; set; }
    public int GradesCorrigidas { get; set; }
    public int ArquivosMovidos { get; set; }
    public List<string>? Erros { get; set; }
}

public interface IProdutoService
{
    Task<ProdutoListResponse> GetProdutosAsync(ProdutoBuscaParams parametros);
    Task<ProdutoPaginatedResponse> GetProdutosPaginadosAsync(int pageNumber, int pageSize, string? grupo, string? cor, decimal? precoMin, decimal? precoMax, string? ordenacao);
    Task<IEnumerable<ProdutoDto>> GetProdutosAleatoriosAsync(int quantidade = 10);
    Task<ProdutoListResponse> GetProdutosAtivosAsync(ProdutoBuscaParams parametros);
    Task<ProdutoListResponse> GetProdutosInativosAsync(ProdutoBuscaParams parametros);
    Task<IEnumerable<ProdutoDto>> GetProdutosRecentesAsync(int quantidade = 10);
    Task<ProdutoDto?> GetProdutoByIdAsync(int id);
    Task<ProdutoDto?> GetProdutoByReferenciaAsync(string referencia);
    Task<ProdutoDetalhadoDto?> GetProdutoDetalhadoAsync(string referencia);
    Task<IEnumerable<ProdutoDto>> GetProdutosByGrupoAsync(string grupo, int quantidade = 20);
    Task<IEnumerable<string>> GetGruposDistintosAsync();
    Task<IEnumerable<string>> GetMarcasDistintasAsync();
    Task<IEnumerable<ProdutoSearchDto>> SearchAsync(string termo, int limit = 8);
    
    // Validações
    Task<ProdutoDto?> GetByReferenciaIncludingInactiveAsync(string referencia);
    Task<ReferenciaValidationResult> ValidateReferenciaAsync(string referencia);
    
    // Cores disponíveis
    Task<IEnumerable<CorDisponivelSimplificadaDto>> GetCoresDisponiveisAsync();
    Task<IEnumerable<TamanhoDisponivelDto>> GetTamanhosDisponiveisAsync();
    
    // CRUD Produto
    Task<ProdutoDto> CreateProdutoAsync(ProdutoCreateDto dto);
    Task<ProdutoDto?> UpdateProdutoAsync(int id, ProdutoUpdateDto dto);
    Task<bool> DeleteProdutoAsync(int id);
    
    // Grades (Variações)
    Task<IEnumerable<ProdutoGradeDto>> GetGradesByProdutoIdAsync(int produtoId);
    Task<ProdutoGradeDto> CreateGradeAsync(int produtoId, ProdutoGradeCreateDto dto);
    Task<ProdutoGradeDto?> UpdateGradeAsync(int gradeId, ProdutoGradeUpdateDto dto);
    Task<bool> DeleteGradeAsync(int gradeId);
    
    // Imagens de Grades
    Task<ImagensGradeUploadResponse> UploadImagensGradeAsync(int gradeId, string referencia, List<IFormFile> imagens);
    Task<IEnumerable<ProdutoGradeImagemDto>> GetImagensGradeAsync(int gradeId);
    Task<bool> DeleteImagemGradeAsync(int imagemId);
    
    // Correção de caminhos
    Task<CorrigirCaminhosResult> CorrigirCaminhosImagensAsync();
}
