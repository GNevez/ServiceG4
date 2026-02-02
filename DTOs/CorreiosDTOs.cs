namespace g4api.DTOs;

public class CorreiosTokenResponse
{
    public string? Token { get; set; }
    public DateTime? Expira { get; set; }
    public string? CartaoPostagem { get; set; }
    public string? Contrato { get; set; }
}

public class CriarPrePostagemDto
{
    public int PedidoId { get; set; }
    public string CodigoServico { get; set; } = "03220";
    public decimal? Peso { get; set; }
    public int? Altura { get; set; }
    public int? Largura { get; set; }
    public int? Comprimento { get; set; }
    public decimal? ValorDeclarado { get; set; }
}

public class PrePostagemResponseDto
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string? CodigoPedido { get; set; }
    public string? ClienteNome { get; set; }
    public string? CodigoRastreamento { get; set; }
    public string? IdPrePostagem { get; set; }
    public string? NumeroEtiqueta { get; set; }
    public string CodigoServico { get; set; } = string.Empty;
    public string NomeServico { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public int Altura { get; set; }
    public int Largura { get; set; }
    public int Comprimento { get; set; }
    public decimal? ValorDeclarado { get; set; }
    public StatusPrePostagem Status { get; set; }
    public string StatusNome { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataPostagem { get; set; }
    public DateTime? DataEntrega { get; set; }
    public string? Observacoes { get; set; }
    public string? MensagemErro { get; set; }
    public DestinatarioDto? Destinatario { get; set; }
}

public class DestinatarioDto
{
    public string? Nome { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }
    public string? CEP { get; set; }
}

public enum StatusPrePostagem
{
    Pendente = 0,
    Gerada = 1,
    Postada = 2,
    EmTransito = 3,
    Entregue = 4,
    Devolvido = 5,
    Cancelada = 6,
    Erro = 7
}

public class PrePostagemFiltroDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public StatusPrePostagem? Status { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
}

public class PrePostagemPaginadoDto
{
    public List<PrePostagemResponseDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class CalcularFreteRequestDto
{
    public string CepDestino { get; set; } = string.Empty;
    public decimal Peso { get; set; } = 0.3m;
    public int Altura { get; set; } = 5;
    public int Largura { get; set; } = 15;
    public int Comprimento { get; set; } = 20;
    public decimal? ValorDeclarado { get; set; }
    public List<string>? CodigosServico { get; set; }
    public int? CarrinhoId { get; set; }
}

public class CalcularFreteResponseDto
{
    public string CodigoServico { get; set; } = string.Empty;
    public string NomeServico { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int PrazoEntrega { get; set; }
    public DateTime? DataPrevistaEntrega { get; set; }
    public string? Mensagem { get; set; }
    public bool Erro { get; set; }
}

public class RastreamentoResponseDto
{
    public string CodigoObjeto { get; set; } = string.Empty;
    public string? TipoPostal { get; set; }
    public List<EventoRastreamentoDto> Eventos { get; set; } = new();
    public string? Mensagem { get; set; }
}

public class EventoRastreamentoDto
{
    public DateTime DataHora { get; set; }
    public string? Descricao { get; set; }
    public string? Tipo { get; set; }
    public string? Unidade { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }
}

public class SuspenderEntregaRequestDto
{
    public string CodigoRastreamento { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public class SuspenderEntregaResponseDto
{
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
    public string? CodigoRastreamento { get; set; }
    public DateTime? DataSuspensao { get; set; }
}

public class ServicoCorreiosDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}

public static class ServicosCorreios
{
    public static readonly List<ServicoCorreiosDto> Lista = new()
    {
        new() { Codigo = "03220", Nome = "SEDEX", Descricao = "Entrega expressa" },
        new() { Codigo = "03298", Nome = "PAC", Descricao = "Entrega econômica" },
        new() { Codigo = "03140", Nome = "SEDEX 10", Descricao = "Entrega até às 10h" },
        new() { Codigo = "03204", Nome = "SEDEX 12", Descricao = "Entrega até às 12h" },
        new() { Codigo = "03158", Nome = "SEDEX Hoje", Descricao = "Entrega no mesmo dia" },
    };
    
    public static string GetNome(string codigo)
    {
        return Lista.FirstOrDefault(s => s.Codigo == codigo)?.Nome ?? "Desconhecido";
    }
}

public class GerarRotuloRangeRequestDto
{
    public string CodigoServico { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class GerarRotuloLoteAsyncRequestDto
{
    public string? IdCorreios { get; set; }
    public string? NumeroCartaoPostagem { get; set; }
    public string TipoRotulo { get; set; } = "P";
    public string FormatoRotulo { get; set; } = "ET";
    public string? IdAtendimento { get; set; }
    public string ImprimeRemetente { get; set; } = "S";
    public List<ItemLotePrePostagem> IdsLotePrePostagem { get; set; } = new();
    public string LayoutImpressao { get; set; } = "PADRAO";
}

public class ItemLotePrePostagem
{
    public string IdPrePostagem { get; set; } = string.Empty;
    public string CodigoObjeto { get; set; } = string.Empty;
    public int Sequencial { get; set; }
}

public class GerarRotuloRegistradoAsyncRequestDto
{
    public int? IdPedido { get; set; }
    public List<string> CodigosObjeto { get; set; } = new();
    public string? IdCorreios { get; set; }
    public string? NumeroCartaoPostagem { get; set; }
    public string TipoRotulo { get; set; } = "P";
    public string FormatoRotulo { get; set; } = "ET";
    public string? IdAtendimento { get; set; }
    public string ImprimeRemetente { get; set; } = "S";
    public List<string> IdsPrePostagem { get; set; } = new();
    public string LayoutImpressao { get; set; } = "PADRAO";
    public string? Observacao { get; set; }
}

public class GerarRotuloResponseDto
{
    public bool Sucesso { get; set; }
    public string? IdRecibo { get; set; }
    public string? UrlRotulo { get; set; }
    public byte[]? PdfBytes { get; set; }
    public string? Mensagem { get; set; }
    public int? IdPedido { get; set; }
    public string? IdAtendimento { get; set; }
    public List<string>? CodigosObjeto { get; set; }
    public List<string>? IdsPrePostagem { get; set; }
    public string? Observacao { get; set; }
    public string? TipoRotulo { get; set; }
    public string? FormatoRotulo { get; set; }
}

public class PrePostagemCorreiosFiltroDto
{
    public string? Id { get; set; }
    public string? CodigoObjeto { get; set; }
    public string? ETicket { get; set; }
    public string? CodigoEstampa2D { get; set; }
    public string? IdCorreios { get; set; }
    public string? Status { get; set; }
    public string? LogisticaReversa { get; set; }
    public string? TipoObjeto { get; set; }
    public string? ModalidadePagamento { get; set; }
    public string? ObjetoCargo { get; set; }
    public DateTime? DataInicialCriacaoPrePostagem { get; set; }
    public DateTime? DataFinalCriacaoPrePostagem { get; set; }
    public int Page { get; set; } = 0;
    public int Size { get; set; } = 10;
}

public class PrePostagemCorreiosItemDto
{
    public string? Id { get; set; }
    public string? IdCorreios { get; set; }
    public string? CodigoObjeto { get; set; }
    public string? CodigoServico { get; set; }
    public string? Status { get; set; }
    public string? StatusDescricao { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataPostagem { get; set; }
    public decimal? Peso { get; set; }
    public decimal? PrecoServico { get; set; }
    public RemetenteDestinatarioDto? Remetente { get; set; }
    public RemetenteDestinatarioDto? Destinatario { get; set; }
}

public class RemetenteDestinatarioDto
{
    public string? Nome { get; set; }
    public string? CpfCnpj { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public EnderecoCorreiosDto? Endereco { get; set; }
}

public class EnderecoCorreiosDto
{
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
}

public class PrePostagemCorreiosPaginadoDto
{
    public List<PrePostagemCorreiosItemDto> Itens { get; set; } = new();
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public bool First { get; set; }
    public bool Last { get; set; }
}

public class PrePostagemPostadaDetalhesDto
{
    public string? CodigoObjeto { get; set; }
    public string? IdPrePostagem { get; set; }
    public string? CodigoServico { get; set; }
    public string? NomeServico { get; set; }
    public string? Status { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataPostagem { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Largura { get; set; }
    public decimal? Comprimento { get; set; }
    public decimal? ValorDeclarado { get; set; }
    public decimal? PrecoServico { get; set; }
    public decimal? PrecoPrePostagem { get; set; }
    public string? NumeroNotaFiscal { get; set; }
    public string? NumeroCartaoPostagem { get; set; }
    public string? IdAtendimento { get; set; }
    public string? Eticket { get; set; }
    public DateTime? DataEticket { get; set; }
    public DateTime? PrazoPostagem { get; set; }
    public int? ModalidadePagamento { get; set; }
    public RemetenteDestinatarioDto? Remetente { get; set; }
    public RemetenteDestinatarioDto? Destinatario { get; set; }
    public List<ServicoAdicionalDto>? ServicosAdicionais { get; set; }
    public List<ItemDeclaracaoConteudoDto>? ItensDeclaracao { get; set; }
    public string? Observacao { get; set; }
    public string? RespostaJson { get; set; }
}

public class ServicoAdicionalDto
{
    public string? Codigo { get; set; }
    public string? Nome { get; set; }
    public decimal? Valor { get; set; }
    public decimal? ValorDeclarado { get; set; }
}

public class ItemDeclaracaoConteudoDto
{
    public string? Conteudo { get; set; }
    public string? Quantidade { get; set; }
    public string? Valor { get; set; }
}
