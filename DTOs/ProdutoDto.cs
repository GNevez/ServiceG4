using Microsoft.AspNetCore.Http;

namespace g4api.DTOs;

public class ProdutoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Descricao { get; set; }
    public string Marca { get; set; } = "";
    public int? Grupo { get; set; }
    public string? Referencia { get; set; }
    public decimal Preco { get; set; }
    public decimal? PrecoOriginal { get; set; }
    public int? Desconto { get; set; }
    public string? Img { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public string? DadosAdicionais { get; set; }
    public string? Ean { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Comprimento { get; set; }
    public List<ProdutoGradeDto>? Grades { get; set; }
}

public class ProdutoDetalhadoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string? Descricao { get; set; }
    public string? AplicacaoConsulta { get; set; }
    public string? DadosAdicionais { get; set; }
    public string Marca { get; set; } = "";
    public int? Grupo { get; set; }
    public decimal Preco { get; set; }
    public decimal? PrecoOriginal { get; set; }
    public int? Desconto { get; set; }
    public string? Img { get; set; }
    public string? Ean { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Comprimento { get; set; }
    public List<CorDisponivelDto> CoresDisponiveis { get; set; } = new();
    public List<TamanhoDisponivelDto> TamanhosDisponiveis { get; set; } = new();
    public List<VariacaoDto> Variacoes { get; set; } = new();
}

public class CorDisponivelDto
{
    public string Nome { get; set; } = "";
    public string? Img { get; set; }
    public List<string> Tamanhos { get; set; } = new();
    public List<ProdutoGradeImagemDto> Imagens { get; set; } = new();
}

public class TamanhoDisponivelDto
{
    public string Nome { get; set; } = "";
    public decimal Quantidade { get; set; }
}

public class VariacaoDto
{
    public int Id { get; set; }
    public int IdProduto { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public decimal Quantidade { get; set; }
    public string? Img { get; set; }
    public decimal? Preco { get; set; }
    public List<ProdutoGradeImagemDto> Imagens { get; set; } = new();
}

public class ProdutoGradeDto
{
    public int Id { get; set; }
    public int IdProduto { get; set; }
    public string? Referencia { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public decimal Quantidade { get; set; }
    public string? Img { get; set; }
    public List<ProdutoGradeImagemDto> Imagens { get; set; } = new();
}

public class ProdutoCreateDto
{
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public string? Marca { get; set; }
    public int? Grupo { get; set; }
    public string? Referencia { get; set; }
    public decimal PrecoTabela { get; set; }
    public decimal PrecoMinimo { get; set; }
    public string? Img { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public string? DadosAdicionais { get; set; }
    public string? Ean { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Comprimento { get; set; }
}

public class ProdutoUpdateDto
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public string? Marca { get; set; }
    public int? Grupo { get; set; }
    public string? Referencia { get; set; }
    public decimal? PrecoTabela { get; set; }
    public decimal? PrecoMinimo { get; set; }
    public string? Img { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public string? DadosAdicionais { get; set; }
    public string? Ean { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Comprimento { get; set; }
}

public class ProdutoGradeCreateDto
{
    public int IdProdutoPrincipal { get; set; }
    public string? Referencia { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public decimal Quantidade { get; set; }
    public string? Img { get; set; }
}

public class ProdutoGradeUpdateDto
{
    public string? Referencia { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public decimal? Quantidade { get; set; }
    public string? Img { get; set; }
}

public class ProdutoSearchDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public decimal Preco { get; set; }
    public string? ImagemPrincipal { get; set; }
}

public class ProdutoListResponse
{
    public List<ProdutoDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ProdutoBuscaParams
{
    public string? Termo { get; set; }
    public int? GrupoId { get; set; }
    public string? Marca { get; set; }
    public decimal? PrecoMin { get; set; }
    public decimal? PrecoMax { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? OrderBy { get; set; } = "nome";
    public bool Desc { get; set; } = false;
}

public class ProdutoPaginatedResponse
{
    public List<ProdutoDto> Produtos { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class CorDisponivelSimplificadaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int QuantidadeProdutos { get; set; }
}

public class ReferenciaValidationResult
{
    public bool Exists { get; set; }
    public bool IsActive { get; set; }
    public string Message { get; set; } = "";
    public string? ProdutoNome { get; set; }
}

// DTOs para upload de imagens
public class ImagemProdutoUploadDto
{
    public IFormFile? Imagem { get; set; }
    public string Referencia { get; set; } = "";
}

public class ImagemUploadResponse
{
    public bool Sucesso { get; set; }
    public string Caminho { get; set; } = "";
    public string Mensagem { get; set; } = "";
}

public class ImagensGradeUploadDto
{
    public List<IFormFile> Imagens { get; set; } = new();
    public string Referencia { get; set; } = "";
}

public class ImagensGradeUploadResponse
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public List<ProdutoGradeImagemDto> Imagens { get; set; } = new();
}

public class ProdutoGradeImagemDto
{
    public int Id { get; set; }
    public int IdProdutoGrade { get; set; }
    public int Ordem { get; set; }
    public string Caminho { get; set; } = "";
}

// DTO para criação de produto com grade principal
public class ProdutoComGradeCreateDto
{
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public string? Marca { get; set; }
    public int? Grupo { get; set; }
    public string Referencia { get; set; } = "";
    public decimal PrecoTabela { get; set; }
    public decimal PrecoMinimo { get; set; }
    public string? Img { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public string? DadosAdicionais { get; set; }
    public string? Ean { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Comprimento { get; set; }
    // Quantidade para criar a grade principal automaticamente
    public decimal Quantidade { get; set; }
    // Lista de variações (grades adicionais)
    public List<GradeVariacaoDto>? Variacoes { get; set; }
}

public class GradeVariacaoDto
{
    public string? Referencia { get; set; }
    public string? Cor { get; set; }
    public string? Tamanho { get; set; }
    public decimal Quantidade { get; set; }
}

