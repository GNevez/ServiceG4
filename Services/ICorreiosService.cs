using g4api.DTOs;
using g4api.Models;

namespace g4api.Services;

public interface ICorreiosService
{
    Task<CorreiosTokenResponse?> ObterTokenAsync();
    
    Task<PrePostagemResponseDto?> CriarPrePostagemAsync(CriarPrePostagemDto dto);
    Task<PrePostagemResponseDto?> GetPrePostagemByIdAsync(int id);
    Task<PrePostagemResponseDto?> GetPrePostagemByPedidoIdAsync(int pedidoId);
    Task<PrePostagemPaginadoDto> GetPrePostagensAsync(PrePostagemFiltroDto filtro);
    Task<bool> CancelarPrePostagemAsync(int id);
    
    Task<PrePostagemCorreiosPaginadoDto> ListarPrePostagensCorreiosAsync(PrePostagemCorreiosFiltroDto filtro);
    Task<PrePostagemPostadaDetalhesDto?> ConsultarPrePostagemPostadaAsync(string codigoObjeto);
    Task<PrePostagemPostadaDetalhesDto?> ConsultarPrePostagemCorreiosByIdAsync(string idPrePostagem);
    Task<bool> CancelarPrePostagemCorreiosAsync(string idPrePostagem);
    
    Task<GerarRotuloResponseDto> GerarRotuloRangeAsync(GerarRotuloRangeRequestDto dto);
    Task<GerarRotuloResponseDto> GerarRotuloLoteAsyncAsync(GerarRotuloLoteAsyncRequestDto dto);
    Task<GerarRotuloResponseDto> GerarRotuloRegistradoAsyncAsync(GerarRotuloRegistradoAsyncRequestDto dto);
    Task<GerarRotuloResponseDto> ConsultarRotuloAsync(string idRecibo, GerarRotuloRegistradoAsyncRequestDto? contexto = null);
    
    // Métodos de persistência de rótulos
    Task<Rotulo?> SalvarRotuloAsync(
        string idRecibo, 
        byte[] pdfBytes, 
        string? nomeArquivo = null,
        int? idPedido = null,
        string? idAtendimento = null,
        List<string>? codigosObjeto = null,
        List<string>? idsPrePostagem = null,
        string? observacao = null,
        string tipoRotulo = "P",
        string formatoRotulo = "ET");
    Task<Rotulo?> BuscarRotuloPorIdReciboAsync(string idRecibo);
    Task<List<Rotulo>> ListarRotulosAsync(int? idPedido = null, int page = 1, int pageSize = 20);
    
    Task<RastreamentoResponseDto?> RastrearObjetoAsync(string codigoRastreamento);
    Task<List<RastreamentoResponseDto>> RastrearMultiplosObjetosAsync(List<string> codigos);
    
    Task<SuspenderEntregaResponseDto> SuspenderEntregaAsync(SuspenderEntregaRequestDto dto);
    Task<SuspenderEntregaResponseDto> ReativarEntregaAsync(string codigoRastreamento);
    
    List<ServicoCorreiosDto> GetServicosDisponiveis();
    
    Task<List<CalcularFreteResponseDto>> CalcularFreteAsync(CalcularFreteRequestDto dto);
    
    Task AtualizarStatusPrePostagensAsync();

    /// <summary>
    /// Cria pré-postagem genérica com payload customizado (usado para logística reversa)
    /// </summary>
    Task<PrePostagemGenericaResponseDto?> CriarPrePostagemGenericaAsync(object payload);
}

/// <summary>
/// Resposta de criação de pré-postagem genérica
/// </summary>
public class PrePostagemGenericaResponseDto
{
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
    public string? IdPrePostagem { get; set; }
    public string? CodigoRastreamento { get; set; }
    public DateTime? PrazoPostagem { get; set; }
    public string? RespostaJson { get; set; }
}
