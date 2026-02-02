using g4api.Data;
using g4api.DTOs;
using g4api.Models;
using Microsoft.EntityFrameworkCore;

namespace g4api.Services;

public class RotuloAutomaticoService
{
    private readonly G4DbContext _context;
    private readonly ICorreiosService _correiosService;
    private readonly ILogger<RotuloAutomaticoService> _logger;

    public RotuloAutomaticoService(
        G4DbContext context, 
        ICorreiosService correiosService, 
        ILogger<RotuloAutomaticoService> logger)
    {
        _context = context;
        _correiosService = correiosService;
        _logger = logger;
    }

    /// <summary>
    /// Gera rótulo automaticamente para um pedido
    /// </summary>
    public async Task<Rotulo?> GerarRotuloParaPedidoAsync(int pedidoId)
    {
        try
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
            if (pedido == null)
            {
                _logger.LogWarning("[RotuloAutomatico] Pedido {PedidoId} não encontrado", pedidoId);
                return null;
            }

            // Buscar pré-postagem gerada
            var prePostagem = await _context.PrePostagens
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.Status == StatusPrePostagem.Gerada);

            if (prePostagem == null)
            {
                _logger.LogWarning("[RotuloAutomatico] Pré-postagem não encontrada ou não gerada para pedido {PedidoId}", pedidoId);
                return null;
            }

            if (string.IsNullOrEmpty(prePostagem.IdPrePostagem))
            {
                _logger.LogWarning("[RotuloAutomatico] Pré-postagem sem IdPrePostagem para pedido {PedidoId}", pedidoId);
                return null;
            }

            // Gerar rótulo via API Correios
            var dto = new GerarRotuloRegistradoAsyncRequestDto
            {
                IdsPrePostagem = new List<string> { prePostagem.IdPrePostagem },
                CodigosObjeto = !string.IsNullOrEmpty(prePostagem.CodigoRastreamento) 
                    ? new List<string> { prePostagem.CodigoRastreamento } 
                    : null,
                IdPedido = pedidoId,
                TipoRotulo = "P",
                FormatoRotulo = "ET"
            };

            _logger.LogInformation("[RotuloAutomatico] Gerando rótulo para pré-postagem {IdPrePostagem}", prePostagem.IdPrePostagem);

            var response = await _correiosService.GerarRotuloRegistradoAsyncAsync(dto);

            if (string.IsNullOrEmpty(response.IdRecibo))
            {
                _logger.LogWarning("[RotuloAutomatico] Não foi possível obter IdRecibo para pedido {PedidoId}. Mensagem: {Mensagem}", 
                    pedidoId, response.Mensagem);
                return null;
            }

            _logger.LogInformation("[RotuloAutomatico] IdRecibo obtido: {IdRecibo}", response.IdRecibo);

            // Aguardar processamento e consultar rótulo passando o contexto para salvar no banco
            await Task.Delay(3000); // Aguarda 3 segundos para processamento

            var rotuloResponse = await _correiosService.ConsultarRotuloAsync(response.IdRecibo, dto);

            if (rotuloResponse.PdfBytes == null || rotuloResponse.PdfBytes.Length == 0)
            {
                // Tentar novamente após mais alguns segundos
                _logger.LogInformation("[RotuloAutomatico] PDF não disponível ainda, aguardando mais 5 segundos...");
                await Task.Delay(5000);
                rotuloResponse = await _correiosService.ConsultarRotuloAsync(response.IdRecibo, dto);
            }

            if (rotuloResponse.PdfBytes == null || rotuloResponse.PdfBytes.Length == 0)
            {
                _logger.LogWarning("[RotuloAutomatico] PDF não disponível ainda para pedido {PedidoId}. Mensagem: {Mensagem}", 
                    pedidoId, rotuloResponse.Mensagem);
                return null;
            }

            // O ConsultarRotuloAsync já salvou o rótulo no banco, vamos buscar
            var rotulo = await _context.Rotulos
                .FirstOrDefaultAsync(r => r.IdRecibo == response.IdRecibo);

            if (rotulo == null)
            {
                // Se não salvou ainda, criar o registro manualmente
                _logger.LogWarning("[RotuloAutomatico] Rótulo não foi salvo automaticamente, criando registro...");
                
                var rotulosDir = Path.Combine("wwwroot", "rotulos");
                Directory.CreateDirectory(rotulosDir);

                var nomeArquivo = $"rotulo_{pedido.CodigoPedido}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var caminhoArquivo = Path.Combine(rotulosDir, nomeArquivo);

                await File.WriteAllBytesAsync(caminhoArquivo, rotuloResponse.PdfBytes);

                rotulo = new Rotulo
                {
                    IdPedido = pedidoId,
                    IdRecibo = response.IdRecibo,
                    NomeArquivo = nomeArquivo,
                    CaminhoArquivo = caminhoArquivo,
                    DataGeracao = DateTime.Now,
                    QuantidadeRotulos = 1,
                    CodigosObjeto = prePostagem.CodigoRastreamento,
                    IdsPrePostagem = prePostagem.IdPrePostagem,
                    TamanhoBytes = rotuloResponse.PdfBytes.Length
                };

                _context.Rotulos.Add(rotulo);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("[RotuloAutomatico] Rótulo {RotuloId} obtido para pedido {PedidoId}", rotulo.Id, pedidoId);

            return rotulo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RotuloAutomatico] Erro ao gerar rótulo para pedido {PedidoId}", pedidoId);
            return null;
        }
    }

    /// <summary>
    /// Adiciona rótulo à fila de impressão
    /// </summary>
    public async Task<FilaImpressao?> AdicionarFilaImpressaoAsync(Rotulo rotulo, int? pedidoId = null)
    {
        try
        {
            var pedido = pedidoId.HasValue 
                ? await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId.Value) 
                : null;

            var fila = new FilaImpressao
            {
                RotuloId = rotulo.Id,
                PedidoId = pedidoId,
                CodigoPedido = pedido?.CodigoPedido,
                NomeArquivo = rotulo.NomeArquivo,
                CaminhoArquivo = rotulo.CaminhoArquivo,
                Status = StatusImpressao.Pendente,
                DataCriacao = DateTime.UtcNow
            };

            _context.FilaImpressao.Add(fila);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[RotuloAutomatico] Item {FilaId} adicionado à fila de impressão para pedido {CodigoPedido}", 
                fila.Id, pedido?.CodigoPedido);

            return fila;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RotuloAutomatico] Erro ao adicionar à fila de impressão");
            return null;
        }
    }
}
