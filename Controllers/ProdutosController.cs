using g4api.DTOs;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtoService;

    public ProdutosController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    [HttpGet]
    public async Task<ActionResult<ProdutoListResponse>> GetProdutos([FromQuery] ProdutoBuscaParams parametros)
    {
        var result = await _produtoService.GetProdutosAsync(parametros);
        return Ok(result);
    }

    [HttpGet("ativos")]
    public async Task<ActionResult<ProdutoListResponse>> GetProdutosAtivos([FromQuery] ProdutoBuscaParams parametros)
    {
        var result = await _produtoService.GetProdutosAtivosAsync(parametros);
        return Ok(result);
    }

    [HttpGet("inativos")]
    public async Task<ActionResult<ProdutoListResponse>> GetProdutosInativos([FromQuery] ProdutoBuscaParams parametros)
    {
        var result = await _produtoService.GetProdutosInativosAsync(parametros);
        return Ok(result);
    }

    [HttpGet("paginated")]
    public async Task<ActionResult<ProdutoPaginatedResponse>> GetPaginatedProdutos(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? grupoId = null,
        [FromQuery] string? corId = null,
        [FromQuery] decimal? precoMin = null,
        [FromQuery] decimal? precoMax = null,
        [FromQuery] string? ordenacao = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 50) pageSize = 50;

        var result = await _produtoService.GetProdutosPaginadosAsync(
            pageNumber, pageSize, grupoId, corId, precoMin, precoMax, ordenacao);
        
        return Ok(result);
    }

    [HttpGet("aleatorios")]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetProdutosAleatorios([FromQuery] int quantidade = 10)
    {
        var produtos = await _produtoService.GetProdutosAleatoriosAsync(quantidade);
        return Ok(produtos);
    }

    [HttpGet("recentes")]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetProdutosRecentes([FromQuery] int quantidade = 10)
    {
        var produtos = await _produtoService.GetProdutosRecentesAsync(quantidade);
        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoDto>> GetProduto(int id)
    {
        var produto = await _produtoService.GetProdutoByIdAsync(id);
        
        if (produto == null)
            return NotFound(new { message = $"Produto com ID {id} não encontrado" });

        return Ok(produto);
    }

    [HttpGet("referencia/{referencia}")]
    public async Task<ActionResult<ProdutoDto>> GetProdutoByReferencia(string referencia)
    {
        var produto = await _produtoService.GetProdutoByReferenciaAsync(referencia);
        
        if (produto == null)
            return NotFound(new { message = $"Produto com referência {referencia} não encontrado" });

        return Ok(produto);
    }

    [HttpGet("detalhado/{referencia}")]
    public async Task<ActionResult<ProdutoDetalhadoDto>> GetProdutoDetalhado(string referencia)
    {
        var produto = await _produtoService.GetProdutoDetalhadoAsync(referencia);
        
        if (produto == null)
            return NotFound(new { message = $"Produto com referência {referencia} não encontrado" });

        return Ok(produto);
    }

    [HttpGet("grupo/{grupo}")]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> GetProdutosByGrupo(string grupo, [FromQuery] int quantidade = 20)
    {
        var produtos = await _produtoService.GetProdutosByGrupoAsync(grupo, quantidade);
        return Ok(produtos);
    }

    [HttpGet("grupos")]
    public async Task<ActionResult<IEnumerable<string>>> GetGrupos()
    {
        var grupos = await _produtoService.GetGruposDistintosAsync();
        return Ok(grupos);
    }

    [HttpGet("marcas")]
    public async Task<ActionResult<IEnumerable<string>>> GetMarcas()
    {
        var marcas = await _produtoService.GetMarcasDistintasAsync();
        return Ok(marcas);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProdutoSearchDto>>> Search([FromQuery] string q, [FromQuery] int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(Enumerable.Empty<ProdutoSearchDto>());

        limit = Math.Clamp(limit, 1, 20);
        var resultados = await _produtoService.SearchAsync(q, limit);
        return Ok(resultados);
    }

    [HttpGet("validate/referencia/{referencia}")]
    public async Task<ActionResult<ReferenciaValidationResult>> ValidateReferencia(string referencia)
    {
        var result = await _produtoService.ValidateReferenciaAsync(referencia);
        return Ok(result);
    }

    [HttpGet("cores-disponiveis")]
    public async Task<ActionResult<IEnumerable<CorDisponivelSimplificadaDto>>> GetCoresDisponiveis()
    {
        var cores = await _produtoService.GetCoresDisponiveisAsync();
        return Ok(cores);
    }

    [HttpGet("tamanhos-disponiveis")]
    public async Task<ActionResult<IEnumerable<TamanhoDisponivelDto>>> GetTamanhosDisponiveis()
    {
        var tamanhos = await _produtoService.GetTamanhosDisponiveisAsync();
        return Ok(tamanhos);
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoDto>> CreateProduto([FromBody] ProdutoCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.Nome))
            return BadRequest(new { message = "Nome do produto é obrigatório" });

        var produto = await _produtoService.CreateProdutoAsync(dto);
        return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProdutoDto>> UpdateProduto(int id, [FromBody] ProdutoUpdateDto dto)
    {
        var produto = await _produtoService.UpdateProdutoAsync(id, dto);
        
        if (produto == null)
            return NotFound(new { message = $"Produto com ID {id} não encontrado" });

        return Ok(produto);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduto(int id)
    {
        var deleted = await _produtoService.DeleteProdutoAsync(id);
        
        if (!deleted)
            return NotFound(new { message = $"Produto com ID {id} não encontrado" });

        return NoContent();
    }

    [HttpGet("{id}/grades")]
    public async Task<ActionResult<IEnumerable<ProdutoGradeDto>>> GetGrades(int id)
    {
        var grades = await _produtoService.GetGradesByProdutoIdAsync(id);
        return Ok(grades);
    }

    [HttpPost("{id}/grades")]
    public async Task<ActionResult<ProdutoGradeDto>> CreateGrade(int id, [FromBody] ProdutoGradeCreateDto dto)
    {
        var grade = await _produtoService.CreateGradeAsync(id, dto);
        return CreatedAtAction(nameof(GetGrades), new { id }, grade);
    }

    [HttpPut("grades/{gradeId}")]
    public async Task<ActionResult<ProdutoGradeDto>> UpdateGrade(int gradeId, [FromBody] ProdutoGradeUpdateDto dto)
    {
        var grade = await _produtoService.UpdateGradeAsync(gradeId, dto);
        
        if (grade == null)
            return NotFound(new { message = $"Grade com ID {gradeId} não encontrada" });

        return Ok(grade);
    }

    [HttpDelete("grades/{gradeId}")]
    public async Task<ActionResult> DeleteGrade(int gradeId)
    {
        var deleted = await _produtoService.DeleteGradeAsync(gradeId);
        
        if (!deleted)
            return NotFound(new { message = $"Grade com ID {gradeId} não encontrada" });

        return NoContent();
    }

    /// <summary>
    /// Upload de imagem para produto principal
    /// Salva em wwwroot/img/catalogo/{referencia}/{referencia}.jpg
    /// </summary>
    [HttpPost("upload-imagem")]
    public async Task<ActionResult<ImagemUploadResponse>> UploadImagemProduto([FromForm] ImagemProdutoUploadDto dto)
    {
        if (dto.Imagem == null || dto.Imagem.Length == 0)
            return BadRequest(new { message = "Nenhuma imagem enviada" });

        // Validar extensão - apenas JPG
        var extension = Path.GetExtension(dto.Imagem.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg")
            return BadRequest(new { message = "Apenas imagens JPG são aceitas" });

        // Validar content type
        if (!dto.Imagem.ContentType.StartsWith("image/jpeg"))
            return BadRequest(new { message = "O arquivo deve ser uma imagem JPEG" });

        if (string.IsNullOrWhiteSpace(dto.Referencia))
            return BadRequest(new { message = "Referência do produto é obrigatória" });

        // Limpar referência para usar como nome de pasta/arquivo
        var referenciaLimpa = dto.Referencia.Trim().Replace(" ", "_").Replace("/", "-");
        
        // Criar diretório se não existir
        var diretorio = Path.Combine("wwwroot", "img", "catalogo", referenciaLimpa);
        if (!Directory.Exists(diretorio))
            Directory.CreateDirectory(diretorio);

        // Salvar arquivo
        var nomeArquivo = $"{referenciaLimpa}.jpg";
        var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

        using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
        {
            await dto.Imagem.CopyToAsync(stream);
        }

        var caminhoRelativo = $"/img/catalogo/{referenciaLimpa}/{nomeArquivo}";

        return Ok(new ImagemUploadResponse
        {
            Sucesso = true,
            Caminho = caminhoRelativo,
            Mensagem = "Imagem enviada com sucesso"
        });
    }

    /// <summary>
    /// Upload de múltiplas imagens para uma grade (variação)
    /// Salva em wwwroot/img/catalogo/{referencia}/{gradeId}_{ordem}.jpg
    /// </summary>
    [HttpPost("grades/{gradeId}/upload-imagens")]
    public async Task<ActionResult<ImagensGradeUploadResponse>> UploadImagensGrade(
        int gradeId, 
        [FromForm] ImagensGradeUploadDto dto)
    {
        if (dto.Imagens == null || dto.Imagens.Count == 0)
            return BadRequest(new { message = "Nenhuma imagem enviada" });

        if (dto.Imagens.Count > 5)
            return BadRequest(new { message = "Máximo de 5 imagens por grade" });

        // Validar todas as imagens
        foreach (var imagem in dto.Imagens)
        {
            var extension = Path.GetExtension(imagem.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg")
                return BadRequest(new { message = $"Apenas imagens JPG são aceitas. Arquivo inválido: {imagem.FileName}" });
            
            if (!imagem.ContentType.StartsWith("image/jpeg"))
                return BadRequest(new { message = $"O arquivo {imagem.FileName} deve ser uma imagem JPEG" });
        }

        if (string.IsNullOrWhiteSpace(dto.Referencia))
            return BadRequest(new { message = "Referência do produto é obrigatória" });

        var result = await _produtoService.UploadImagensGradeAsync(gradeId, dto.Referencia, dto.Imagens);
        
        if (!result.Sucesso)
            return BadRequest(new { message = result.Mensagem });

        return Ok(result);
    }

    /// <summary>
    /// Buscar imagens de uma grade
    /// </summary>
    [HttpGet("grades/{gradeId}/imagens")]
    public async Task<ActionResult<IEnumerable<ProdutoGradeImagemDto>>> GetImagensGrade(int gradeId)
    {
        var imagens = await _produtoService.GetImagensGradeAsync(gradeId);
        return Ok(imagens);
    }

    /// <summary>
    /// Deletar imagem de uma grade
    /// </summary>
    [HttpDelete("grades/imagens/{imagemId}")]
    public async Task<ActionResult> DeleteImagemGrade(int imagemId)
    {
        var deleted = await _produtoService.DeleteImagemGradeAsync(imagemId);
        
        if (!deleted)
            return NotFound(new { message = "Imagem não encontrada" });

        return NoContent();
    }

    /// <summary>
    /// Corrigir caminhos de imagens que usavam /grades/
    /// Remove o /grades/ do caminho das imagens e move os arquivos físicos
    /// </summary>
    [HttpPost("corrigir-caminhos-imagens")]
    public async Task<ActionResult> CorrigirCaminhosImagens()
    {
        var result = await _produtoService.CorrigirCaminhosImagensAsync();
        return Ok(result);
    }
}
