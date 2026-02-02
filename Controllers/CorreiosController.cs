using g4api.DTOs;
using g4api.Models;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CorreiosController : ControllerBase
{
    private readonly ICorreiosService _correiosService;
    private readonly ILogger<CorreiosController> _logger;

    public CorreiosController(ICorreiosService correiosService, ILogger<CorreiosController> logger)
    {
        _correiosService = correiosService;
        _logger = logger;
    }

    #region Serviços

    /// <summary>
    /// Lista os serviços de envio disponíveis
    /// </summary>
    [HttpGet("servicos")]
    public ActionResult<List<ServicoCorreiosDto>> GetServicosDisponiveis()
    {
        var servicos = _correiosService.GetServicosDisponiveis();
        return Ok(servicos);
    }

    #endregion

    #region Cálculo de Frete

    /// <summary>
    /// Calcula o frete para um CEP de destino
    /// </summary>
    [HttpPost("calcular-frete")]
    public async Task<ActionResult<List<CalcularFreteResponseDto>>> CalcularFrete([FromBody] CalcularFreteRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CepDestino))
        {
            return BadRequest(new { error = "CEP de destino é obrigatório" });
        }

        var resultados = await _correiosService.CalcularFreteAsync(dto);
        return Ok(resultados);
    }

    /// <summary>
    /// Calcula o frete via GET (para facilitar testes)
    /// </summary>
    [HttpGet("calcular-frete")]
    public async Task<ActionResult<List<CalcularFreteResponseDto>>> CalcularFreteGet(
        [FromQuery] string cepDestino,
        [FromQuery] decimal peso = 0.3m,
        [FromQuery] int altura = 5,
        [FromQuery] int largura = 15,
        [FromQuery] int comprimento = 20)
    {
        if (string.IsNullOrWhiteSpace(cepDestino))
        {
            return BadRequest(new { error = "CEP de destino é obrigatório" });
        }

        var dto = new CalcularFreteRequestDto
        {
            CepDestino = cepDestino,
            Peso = peso,
            Altura = altura,
            Largura = largura,
            Comprimento = comprimento
        };

        var resultados = await _correiosService.CalcularFreteAsync(dto);
        return Ok(resultados);
    }

    #endregion

    #region Rastreamento


    [HttpGet("rastreamento/{codigo}")]
    public async Task<ActionResult<RastreamentoResponseDto>> RastrearObjeto(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return BadRequest(new { error = "Código de rastreamento é obrigatório" });
        }

        var resultado = await _correiosService.RastrearObjetoAsync(codigo.ToUpper());
        if (resultado == null)
        {
            return NotFound(new { error = "Objeto não encontrado" });
        }
        return Ok(resultado);
    }

    /// <summary>
    /// Rastreia múltiplos objetos
    /// </summary>
    [HttpPost("rastreamento/lote")]
    public async Task<ActionResult<List<RastreamentoResponseDto>>> RastrearMultiplosObjetos([FromBody] List<string> codigos)
    {
        if (codigos == null || codigos.Count == 0)
        {
            return BadRequest(new { error = "Nenhum código informado" });
        }

        if (codigos.Count > 50)
        {
            return BadRequest(new { error = "Máximo de 50 códigos por requisição" });
        }

        var resultados = await _correiosService.RastrearMultiplosObjetosAsync(codigos);
        return Ok(resultados);
    }

    #endregion

    #region Pré-Postagem Local (Banco de Dados)

    /// <summary>
    /// Cria uma nova pré-postagem para um pedido
    /// </summary>
    [HttpPost("pre-postagem")]
    public async Task<ActionResult<PrePostagemResponseDto>> CriarPrePostagem([FromBody] CriarPrePostagemDto dto)
    {
        if (dto.PedidoId <= 0)
        {
            return BadRequest(new { error = "PedidoId é obrigatório" });
        }

        var resultado = await _correiosService.CriarPrePostagemAsync(dto);
        if (resultado == null)
        {
            return NotFound(new { error = "Pedido não encontrado ou sem endereço de entrega" });
        }

        return Ok(resultado);
    }

    /// <summary>
    /// Obtém uma pré-postagem por ID
    /// </summary>
    [HttpGet("pre-postagem/{id:int}")]
    public async Task<ActionResult<PrePostagemResponseDto>> GetPrePostagemById(int id)
    {
        var prePostagem = await _correiosService.GetPrePostagemByIdAsync(id);
        if (prePostagem == null)
        {
            return NotFound(new { error = "Pré-postagem não encontrada" });
        }
        return Ok(prePostagem);
    }

    /// <summary>
    /// Obtém a pré-postagem de um pedido específico
    /// </summary>
    [HttpGet("pre-postagem/pedido/{pedidoId:int}")]
    public async Task<ActionResult<PrePostagemResponseDto>> GetPrePostagemByPedidoId(int pedidoId)
    {
        var prePostagem = await _correiosService.GetPrePostagemByPedidoIdAsync(pedidoId);
        if (prePostagem == null)
        {
            return NotFound(new { error = "Pré-postagem não encontrada para este pedido" });
        }
        return Ok(prePostagem);
    }

    /// <summary>
    /// Lista pré-postagens com filtros e paginação
    /// </summary>
    [HttpGet("pre-postagem")]
    public async Task<ActionResult<PrePostagemPaginadoDto>> GetPrePostagens([FromQuery] PrePostagemFiltroDto filtro)
    {
        var resultado = await _correiosService.GetPrePostagensAsync(filtro);
        return Ok(resultado);
    }

    /// <summary>
    /// Cria pré-postagens para múltiplos pedidos em lote
    /// </summary>
    [HttpPost("pre-postagem/lote")]
    public async Task<IActionResult> CriarPrePostagensEmLote([FromBody] List<CriarPrePostagemDto> dtos)
    {
        if (dtos == null || dtos.Count == 0)
        {
            return BadRequest(new { error = "Nenhum pedido informado" });
        }

        var sucesso = new List<PrePostagemResponseDto>();
        var erros = new List<object>();

        foreach (var dto in dtos)
        {
            try
            {
                var prePostagem = await _correiosService.CriarPrePostagemAsync(dto);
                if (prePostagem != null)
                {
                    sucesso.Add(prePostagem);
                }
                else
                {
                    erros.Add(new { pedidoId = dto.PedidoId, erro = "Falha ao criar pré-postagem" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar pré-postagem para pedido {PedidoId}", dto.PedidoId);
                erros.Add(new { pedidoId = dto.PedidoId, erro = ex.Message });
            }
        }

        return Ok(new
        {
            sucesso = sucesso,
            erros = erros,
            totalSucesso = sucesso.Count,
            totalErros = erros.Count
        });
    }

    /// <summary>
    /// Cancela uma pré-postagem local
    /// </summary>
    [HttpDelete("pre-postagem/{id:int}")]
    public async Task<IActionResult> CancelarPrePostagem(int id)
    {
        var sucesso = await _correiosService.CancelarPrePostagemAsync(id);
        if (!sucesso)
        {
            return BadRequest(new { error = "Não foi possível cancelar a pré-postagem. Ela pode já ter sido postada ou não existe." });
        }
        return Ok(new { message = "Pré-postagem cancelada com sucesso" });
    }

    /// <summary>
    /// Cancela uma pré-postagem nos Correios E localmente (endpoint unificado)
    /// </summary>
    [HttpDelete("pre-postagem/{id:int}/completo")]
    public async Task<IActionResult> CancelarPrePostagemCompleto(int id)
    {
        var prePostagem = await _correiosService.GetPrePostagemByIdAsync(id);
        if (prePostagem == null)
        {
            return NotFound(new { error = "Pré-postagem não encontrada" });
        }

        // Se tem ID nos Correios, cancela lá primeiro
        if (!string.IsNullOrEmpty(prePostagem.IdPrePostagem))
        {
            var canceladoCorreios = await _correiosService.CancelarPrePostagemCorreiosAsync(prePostagem.IdPrePostagem);
            if (!canceladoCorreios)
            {
                _logger.LogWarning("Não foi possível cancelar pré-postagem nos Correios, mas continuaremos com cancelamento local");
            }
        }

        // Cancela localmente
        var sucesso = await _correiosService.CancelarPrePostagemAsync(id);
        if (!sucesso)
        {
            return BadRequest(new { error = "Não foi possível cancelar a pré-postagem localmente" });
        }

        return Ok(new { message = "Pré-postagem cancelada com sucesso" });
    }

    /// <summary>
    /// Obtém detalhes completos de uma pré-postagem (local + API Correios se disponível)
    /// </summary>
    [HttpGet("pre-postagem/{id:int}/detalhes")]
    public async Task<IActionResult> GetPrePostagemDetalhes(int id)
    {
        var prePostagem = await _correiosService.GetPrePostagemByIdAsync(id);
        if (prePostagem == null)
        {
            return NotFound(new { error = "Pré-postagem não encontrada" });
        }

        PrePostagemPostadaDetalhesDto? detalhesCorreios = null;
        
        // Se tem ID nos Correios, busca detalhes de lá
        if (!string.IsNullOrEmpty(prePostagem.IdPrePostagem))
        {
            try
            {
                detalhesCorreios = await _correiosService.ConsultarPrePostagemCorreiosByIdAsync(prePostagem.IdPrePostagem);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao consultar detalhes da pré-postagem nos Correios");
            }
        }

        return Ok(new
        {
            local = prePostagem,
            correios = detalhesCorreios
        });
    }

    #endregion

    #region Pré-Postagem API Correios

    /// <summary>
    /// Lista pré-postagens diretamente da API dos Correios
    /// </summary>
    [HttpGet("pre-postagem/correios")]
    public async Task<ActionResult<PrePostagemCorreiosPaginadoDto>> ListarPrePostagensCorreios([FromQuery] PrePostagemCorreiosFiltroDto filtro)
    {
        var resultado = await _correiosService.ListarPrePostagensCorreiosAsync(filtro);
        return Ok(resultado);
    }

    /// <summary>
    /// Consulta detalhes de uma pré-postagem postada nos Correios
    /// </summary>
    [HttpGet("pre-postagem/correios/postada/{codigoObjeto}")]
    public async Task<ActionResult<PrePostagemPostadaDetalhesDto>> ConsultarPrePostagemPostada(string codigoObjeto)
    {
        var resultado = await _correiosService.ConsultarPrePostagemPostadaAsync(codigoObjeto);
        if (resultado == null)
        {
            return NotFound(new { error = "Pré-postagem não encontrada ou ainda não foi postada" });
        }
        return Ok(resultado);
    }

    /// <summary>
    /// Cancela uma pré-postagem na API dos Correios
    /// </summary>
    [HttpDelete("pre-postagem/correios/{idPrePostagem}")]
    public async Task<IActionResult> CancelarPrePostagemCorreios(string idPrePostagem)
    {
        var sucesso = await _correiosService.CancelarPrePostagemCorreiosAsync(idPrePostagem);
        if (!sucesso)
        {
            return BadRequest(new { error = "Não foi possível cancelar a pré-postagem nos Correios" });
        }
        return Ok(new { message = "Pré-postagem cancelada com sucesso nos Correios" });
    }

    #endregion

    #region Geração de Rótulos

    /// <summary>
    /// Gera rótulos por range (intervalo de códigos)
    /// </summary>
    [HttpPost("rotulo/range")]
    public async Task<ActionResult<GerarRotuloResponseDto>> GerarRotuloRange([FromBody] GerarRotuloRangeRequestDto dto)
    {
        var resultado = await _correiosService.GerarRotuloRangeAsync(dto);
        
        if (resultado.PdfBytes != null && resultado.PdfBytes.Length > 0)
        {
            return File(resultado.PdfBytes, "application/pdf", $"rotulos_range_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        
        return Ok(resultado);
    }

    /// <summary>
    /// Gera rótulos em lote de forma assíncrona
    /// </summary>
    [HttpPost("rotulo/lote")]
    public async Task<ActionResult<GerarRotuloResponseDto>> GerarRotuloLoteAsync([FromBody] GerarRotuloLoteAsyncRequestDto dto)
    {
        var resultado = await _correiosService.GerarRotuloLoteAsyncAsync(dto);
        return Ok(resultado);
    }

    /// <summary>
    /// Gera rótulo registrado de forma assíncrona (endpoint principal para geração de rótulos)
    /// </summary>
    [HttpPost("rotulo/assincrono")]
    public async Task<ActionResult<GerarRotuloResponseDto>> GerarRotuloRegistradoAsync([FromBody] GerarRotuloRegistradoAsyncRequestDto dto)
    {
        var resultado = await _correiosService.GerarRotuloRegistradoAsyncAsync(dto);
        return Ok(resultado);
    }

    /// <summary>
    /// Consulta/baixa um rótulo assíncrono pelo ID do recibo (sem contexto)
    /// </summary>
    [HttpGet("rotulo/download/{idRecibo}")]
    public async Task<IActionResult> ConsultarRotulo(string idRecibo)
    {
        var resultado = await _correiosService.ConsultarRotuloAsync(idRecibo);
        
        if (resultado.PdfBytes != null && resultado.PdfBytes.Length > 0)
        {
            return File(resultado.PdfBytes, "application/pdf", $"rotulo_{idRecibo}.pdf");
        }
        
        return Ok(resultado);
    }

    /// <summary>
    /// Consulta/baixa um rótulo assíncrono pelo ID do recibo com contexto (salva no banco)
    /// </summary>
    [HttpPost("rotulo/download/{idRecibo}")]
    public async Task<IActionResult> ConsultarRotuloComContexto(string idRecibo, [FromBody] GerarRotuloRegistradoAsyncRequestDto contexto)
    {
        var resultado = await _correiosService.ConsultarRotuloAsync(idRecibo, contexto);
        
        if (resultado.PdfBytes != null && resultado.PdfBytes.Length > 0)
        {
            return File(resultado.PdfBytes, "application/pdf", $"rotulo_{idRecibo}.pdf");
        }
        
        return Ok(resultado);
    }

    /// <summary>
    /// Busca um rótulo já salvo no banco pelo ID do recibo
    /// </summary>
    [HttpGet("rotulo/banco/{idRecibo}")]
    public async Task<IActionResult> BuscarRotuloBanco(string idRecibo)
    {
        var rotulo = await _correiosService.BuscarRotuloPorIdReciboAsync(idRecibo);
        
        if (rotulo == null)
        {
            return NotFound(new { error = "Rótulo não encontrado no banco de dados" });
        }
        
        return Ok(rotulo);
    }

    /// <summary>
    /// Lista rótulos salvos no banco (opcionalmente filtrado por pedido)
    /// </summary>
    [HttpGet("rotulos")]
    public async Task<ActionResult<List<Rotulo>>> ListarRotulos([FromQuery] int? idPedido, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var rotulos = await _correiosService.ListarRotulosAsync(idPedido, page, pageSize);
        return Ok(rotulos);
    }

    /// <summary>
    /// Endpoint para verificar status do rótulo sem baixar o PDF (útil para polling)
    /// </summary>
    [HttpGet("rotulo/status/{idRecibo}")]
    public async Task<ActionResult<GerarRotuloResponseDto>> StatusRotulo(string idRecibo)
    {
        var resultado = await _correiosService.ConsultarRotuloAsync(idRecibo);
        
        // Não retorna os bytes do PDF neste endpoint
        if (resultado.PdfBytes != null)
        {
            resultado.PdfBytes = null;
            resultado.Mensagem = "Rótulo pronto para download";
        }
        
        return Ok(resultado);
    }

    #endregion

    #region Suspensão de Entrega

    /// <summary>
    /// Solicita a suspensão de entrega de um objeto
    /// </summary>
    [HttpPost("suspender-entrega")]
    public async Task<ActionResult<SuspenderEntregaResponseDto>> SuspenderEntrega([FromBody] SuspenderEntregaRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoRastreamento))
        {
            return BadRequest(new { error = "Código de rastreamento é obrigatório" });
        }

        var resultado = await _correiosService.SuspenderEntregaAsync(dto);
        return Ok(resultado);
    }

    /// <summary>
    /// Reativa a entrega de um objeto previamente suspenso
    /// </summary>
    [HttpPost("reativar-entrega/{codigoRastreamento}")]
    public async Task<ActionResult<SuspenderEntregaResponseDto>> ReativarEntrega(string codigoRastreamento)
    {
        if (string.IsNullOrWhiteSpace(codigoRastreamento))
        {
            return BadRequest(new { error = "Código de rastreamento é obrigatório" });
        }

        var resultado = await _correiosService.ReativarEntregaAsync(codigoRastreamento);
        return Ok(resultado);
    }

    #endregion

    #region Utilitários

    /// <summary>
    /// Atualiza o status de todas as pré-postagens pendentes (job manual)
    /// </summary>
    [HttpPost("atualizar-status")]
    public async Task<IActionResult> AtualizarStatusPrePostagens()
    {
        await _correiosService.AtualizarStatusPrePostagensAsync();
        return Ok(new { message = "Status das pré-postagens atualizados" });
    }

    /// <summary>
    /// Testa a autenticação com a API dos Correios
    /// </summary>
    [HttpGet("auth/test")]
    public async Task<IActionResult> TestarAutenticacao()
    {
        var token = await _correiosService.ObterTokenAsync();
        if (token == null)
        {
            return BadRequest(new { error = "Falha na autenticação com os Correios" });
        }

        return Ok(new 
        { 
            message = "Autenticação bem-sucedida",
            expira = token.Expira,
            cartaoPostagem = token.CartaoPostagem
        });
    }

    #endregion
}
