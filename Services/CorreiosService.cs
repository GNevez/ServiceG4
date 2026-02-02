using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using g4api.Data;
using g4api.DTOs;
using g4api.Models;
using Microsoft.EntityFrameworkCore;

namespace g4api.Services;

public class CorreiosService : ICorreiosService
{
    private readonly G4DbContext _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CorreiosService> _logger;
    
    // Cache do token
    private static string? _cachedToken;
    private static DateTime _tokenExpiration = DateTime.MinValue;
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    public CorreiosService(
        G4DbContext context,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<CorreiosService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_configuration["Correios:BaseUrl"] ?? "https://api.correios.com.br");
    }

    #region Autenticação

    public async Task<CorreiosTokenResponse?> ObterTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            // Verificar se o token ainda é válido (com margem de 5 minutos)
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiration.AddMinutes(-5))
            {
                return new CorreiosTokenResponse
                {
                    Token = _cachedToken,
                    Expira = _tokenExpiration,
                    CartaoPostagem = _configuration["Correios:CartaoPostagem"]
                };
            }

            var usuario = _configuration["Correios:Usuario"];
            var codigoAcesso = _configuration["Correios:CodigoAcesso"];
            var cartaoPostagem = _configuration["Correios:CartaoPostagem"];

            // Criar autenticação Basic
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{usuario}:{codigoAcesso}"));
            
            var request = new HttpRequestMessage(HttpMethod.Post, "/token/v1/autentica/cartaopostagem");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { numero = cartaoPostagem }),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("[Correios] Solicitando novo token de autenticação...");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Correios] Erro ao obter token: {StatusCode} - {Content}", response.StatusCode, content);
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
            
            _cachedToken = tokenData.GetProperty("token").GetString();
            
            // Token dos Correios geralmente expira em 1 hora
            if (tokenData.TryGetProperty("expiraEm", out var expiraEl))
            {
                _tokenExpiration = DateTime.Parse(expiraEl.GetString()!);
            }
            else
            {
                _tokenExpiration = DateTime.UtcNow.AddHours(1);
            }

            _logger.LogInformation("[Correios] Token obtido com sucesso. Expira em: {Expira}", _tokenExpiration);

            return new CorreiosTokenResponse
            {
                Token = _cachedToken,
                Expira = _tokenExpiration,
                CartaoPostagem = cartaoPostagem,
                Contrato = tokenData.TryGetProperty("contrato", out var contratoEl) ? contratoEl.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao obter token");
            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpRequestMessage> CriarRequestAutenticadoAsync(HttpMethod method, string endpoint)
    {
        var tokenResponse = await ObterTokenAsync();
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
        {
            throw new InvalidOperationException("Não foi possível obter token de autenticação dos Correios");
        }

        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
        return request;
    }

    #endregion

    #region Rastreamento

    public async Task<RastreamentoResponseDto?> RastrearObjetoAsync(string codigoRastreamento)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Get, $"/srorastro/v1/objetos/{codigoRastreamento}?resultado=T");
            
            _logger.LogInformation("[Correios] Rastreando objeto {Codigo}...", codigoRastreamento);
            
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta rastreamento: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                return new RastreamentoResponseDto
                {
                    CodigoObjeto = codigoRastreamento,
                    Mensagem = $"Erro ao rastrear: {response.StatusCode}"
                };
            }

            var data = JsonSerializer.Deserialize<JsonElement>(content);
            var resultado = new RastreamentoResponseDto
            {
                CodigoObjeto = codigoRastreamento
            };

            if (data.TryGetProperty("objetos", out var objetosEl) && objetosEl.GetArrayLength() > 0)
            {
                var objeto = objetosEl[0];
                
                if (objeto.TryGetProperty("tipoPostal", out var tipoEl))
                {
                    if (tipoEl.TryGetProperty("descricao", out var descEl))
                    {
                        resultado.TipoPostal = descEl.GetString();
                    }
                }

                if (objeto.TryGetProperty("eventos", out var eventosEl))
                {
                    foreach (var evento in eventosEl.EnumerateArray())
                    {
                        var eventoDto = new EventoRastreamentoDto();
                        
                        if (evento.TryGetProperty("dtHrCriado", out var dtEl))
                        {
                            eventoDto.DataHora = DateTime.Parse(dtEl.GetString()!);
                        }
                        if (evento.TryGetProperty("descricao", out var descEvEl))
                        {
                            eventoDto.Descricao = descEvEl.GetString();
                        }
                        if (evento.TryGetProperty("tipo", out var tipoEvEl))
                        {
                            eventoDto.Tipo = tipoEvEl.GetString();
                        }
                        if (evento.TryGetProperty("unidade", out var unidadeEl))
                        {
                            if (unidadeEl.TryGetProperty("nome", out var nomeUnEl))
                            {
                                eventoDto.Unidade = nomeUnEl.GetString();
                            }
                            if (unidadeEl.TryGetProperty("endereco", out var endUnEl))
                            {
                                if (endUnEl.TryGetProperty("cidade", out var cidadeEl))
                                {
                                    eventoDto.Cidade = cidadeEl.GetString();
                                }
                                if (endUnEl.TryGetProperty("uf", out var ufEl))
                                {
                                    eventoDto.UF = ufEl.GetString();
                                }
                            }
                        }

                        resultado.Eventos.Add(eventoDto);
                    }
                }
            }
            else
            {
                resultado.Mensagem = "Objeto não encontrado";
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao rastrear objeto {Codigo}", codigoRastreamento);
            return new RastreamentoResponseDto
            {
                CodigoObjeto = codigoRastreamento,
                Mensagem = $"Erro: {ex.Message}"
            };
        }
    }

    public async Task<List<RastreamentoResponseDto>> RastrearMultiplosObjetosAsync(List<string> codigos)
    {
        var resultados = new List<RastreamentoResponseDto>();
        
        foreach (var codigo in codigos.Take(50)) // Limite de 50 objetos
        {
            var resultado = await RastrearObjetoAsync(codigo);
            if (resultado != null)
            {
                resultados.Add(resultado);
            }
        }

        return resultados;
    }

    #endregion

    #region Serviços

    public List<ServicoCorreiosDto> GetServicosDisponiveis()
    {
        return ServicosCorreios.Lista;
    }

    #endregion

    #region Cálculo de Frete

    public async Task<List<CalcularFreteResponseDto>> CalcularFreteAsync(CalcularFreteRequestDto dto)
    {
        try
        {
            var cepOrigem = _configuration["Correios:RemetenteCEP"]?.Replace("-", "");
            var cepDestino = dto.CepDestino.Replace("-", "");
            
            var servicosCalcular = dto.CodigosServico ?? new List<string> { "03220", "03298" }; // SEDEX e PAC por padrão
            var resultados = new List<CalcularFreteResponseDto>();

            // Usar carrinhoId se disponível, senão gerar ID aleatório
            var idLote = dto.CarrinhoId.HasValue 
                ? $"CAR{dto.CarrinhoId.Value}" 
                : Guid.NewGuid().ToString("N").Substring(0, 10);

            foreach (var codigoServico in servicosCalcular)
            {
                try
                {
                    decimal preco = 0m;
                    int prazo = 0;
                    DateTime? dataPrevisao = null;
                    string? mensagemErro = null;

                    // ============ CHAMAR API DE PREÇO ============
                    var requestPreco = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/preco/v1/nacional");
                    
                    // Peso padronizado: 10kg = 10000 gramas
                    var pesoGramas = 10000;
                    
                    var payloadPreco = new
                    {
                        idLote,
                        parametrosProduto = new[]
                        {
                            new
                            {
                                coProduto = codigoServico,
                                nuRequisicao = $"{idLote}_{codigoServico}_P",
                                cepOrigem,
                                cepDestino,
                                psObjeto = pesoGramas,
                                tpObjeto = 2,
                                comprimento = dto.Comprimento,
                                largura = dto.Largura,
                                altura = dto.Altura
                            }
                        }
                    };

                    requestPreco.Content = new StringContent(
                        JsonSerializer.Serialize(payloadPreco, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var responsePreco = await _httpClient.SendAsync(requestPreco);
                    var contentPreco = await responsePreco.Content.ReadAsStringAsync();

                    _logger.LogInformation("[Correios] Resposta PREÇO {Servico}: {StatusCode} - {Content}", 
                        codigoServico, responsePreco.StatusCode, contentPreco);

                    if (responsePreco.IsSuccessStatusCode)
                    {
                        var responseData = JsonSerializer.Deserialize<JsonElement>(contentPreco);
                        
                        if (responseData.ValueKind == JsonValueKind.Array && responseData.GetArrayLength() > 0)
                        {
                            var primeiro = responseData[0];
                            
                            if (primeiro.TryGetProperty("pcFinal", out var precoEl))
                            {
                                var precoStr = precoEl.ValueKind == JsonValueKind.String 
                                    ? precoEl.GetString() 
                                    : precoEl.ToString();
                                decimal.TryParse(precoStr?.Replace(",", "."), 
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, 
                                    out preco);
                            }
                        }
                    }
                    else
                    {
                        mensagemErro = $"Erro preço: {contentPreco}";
                    }

                    // ============ CHAMAR API DE PRAZO ============
                    var requestPrazo = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/prazo/v1/nacional");
                    
                    // Formato correto para API dos Correios: dd-MM-yyyy
                    var dataPostagem = DateTime.Now.ToString("dd-MM-yyyy");
                    var payloadPrazo = new
                    {
                        idLote,
                        parametrosPrazo = new[]
                        {
                            new
                            {
                                coProduto = codigoServico,
                                nuRequisicao = $"{idLote}_{codigoServico}_D",
                                cepOrigem,
                                cepDestino,
                                dtEvento = dataPostagem
                            }
                        }
                    };

                    requestPrazo.Content = new StringContent(
                        JsonSerializer.Serialize(payloadPrazo, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var responsePrazo = await _httpClient.SendAsync(requestPrazo);
                    var contentPrazo = await responsePrazo.Content.ReadAsStringAsync();

                    _logger.LogInformation("[Correios] Resposta PRAZO {Servico}: {StatusCode} - {Content}", 
                        codigoServico, responsePrazo.StatusCode, contentPrazo);

                    if (responsePrazo.IsSuccessStatusCode)
                    {
                        var prazoData = JsonSerializer.Deserialize<JsonElement>(contentPrazo);
                        
                        if (prazoData.ValueKind == JsonValueKind.Array && prazoData.GetArrayLength() > 0)
                        {
                            var primeiro = prazoData[0];
                            
                            if (primeiro.TryGetProperty("prazoEntrega", out var prazoEl))
                            {
                                prazo = prazoEl.GetInt32();
                            }
                            if (primeiro.TryGetProperty("dataMaxima", out var dataMaxEl))
                            {
                                DateTime.TryParse(dataMaxEl.GetString(), out var dt);
                                dataPrevisao = dt;
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(mensagemErro))
                    {
                        mensagemErro = $"Erro prazo: {contentPrazo}";
                    }

                    // ============ ADICIONAR RESULTADO ============
                    if (preco > 0 || prazo > 0)
                    {
                        resultados.Add(new CalcularFreteResponseDto
                        {
                            CodigoServico = codigoServico,
                            NomeServico = ServicosCorreios.GetNome(codigoServico),
                            Preco = preco,
                            PrazoEntrega = prazo,
                            DataPrevistaEntrega = dataPrevisao,
                            Erro = false
                        });
                    }
                    else
                    {
                        resultados.Add(new CalcularFreteResponseDto
                        {
                            CodigoServico = codigoServico,
                            NomeServico = ServicosCorreios.GetNome(codigoServico),
                            Erro = true,
                            Mensagem = mensagemErro ?? "Não foi possível calcular o frete"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Correios] Erro ao calcular frete para serviço {Servico}", codigoServico);
                    resultados.Add(new CalcularFreteResponseDto
                    {
                        CodigoServico = codigoServico,
                        NomeServico = ServicosCorreios.GetNome(codigoServico),
                        Erro = true,
                        Mensagem = ex.Message
                    });
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro geral ao calcular frete");
            return new List<CalcularFreteResponseDto>
            {
                new()
                {
                    Erro = true,
                    Mensagem = ex.Message
                }
            };
        }
    }

    #endregion

    #region Pré-Postagem

    public async Task<PrePostagemResponseDto?> CriarPrePostagemAsync(CriarPrePostagemDto dto)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.EnderecoEntrega)
                .Include(p => p.Carrinho)
                    .ThenInclude(c => c.Itens)
                        .ThenInclude(i => i.Grade)
                .FirstOrDefaultAsync(p => p.Id == dto.PedidoId);

            if (pedido == null)
            {
                _logger.LogWarning("[Correios] Pedido {PedidoId} não encontrado", dto.PedidoId);
                return null;
            }

            var prePostagemExistente = await _context.PrePostagens
                .FirstOrDefaultAsync(p => p.PedidoId == dto.PedidoId && p.Status != StatusPrePostagem.Cancelada && p.Status != StatusPrePostagem.Erro);

            if (prePostagemExistente != null)
            {
                _logger.LogWarning("[Correios] Já existe pré-postagem para o pedido {PedidoId}", dto.PedidoId);
                return await MapToResponseDto(prePostagemExistente);
            }

            var endereco = pedido.EnderecoEntrega;
            if (endereco == null)
            {
                _logger.LogError("[Correios] Pedido {PedidoId} não possui endereço de entrega", dto.PedidoId);
                return null;
            }

            var prePostagemRequest = CriarRequestPrePostagem(pedido, dto);

            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/prepostagem/v1/prepostagens");
            request.Content = new StringContent(
                JsonSerializer.Serialize(prePostagemRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("[Correios] Enviando pré-postagem para pedido {PedidoId}...", dto.PedidoId);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta pré-postagem: {StatusCode} - {Content}", response.StatusCode, content);

            // SÓ SALVA NO BANCO SE A PRÉ-POSTAGEM FOI CRIADA COM SUCESSO NOS CORREIOS
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Correios] Erro ao criar pré-postagem nos Correios: {StatusCode} - {Content}", response.StatusCode, content);
                throw new Exception($"Erro ao criar pré-postagem nos Correios: {response.StatusCode} - {content}");
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(content);
            
            var prePostagem = new PrePostagem
            {
                PedidoId = dto.PedidoId,
                CodigoServico = dto.CodigoServico,
                NomeServico = ServicosCorreios.GetNome(dto.CodigoServico),
                Peso = dto.Peso ?? 0.3m,
                Altura = dto.Altura ?? 5,
                Largura = dto.Largura ?? 15,
                Comprimento = dto.Comprimento ?? 20,
                ValorDeclarado = dto.ValorDeclarado ?? pedido.TotalPedido,
                RespostaCorreiosJson = content,
                Status = StatusPrePostagem.Gerada
            };
            
            if (responseData.TryGetProperty("codigoObjeto", out var codigoEl))
                prePostagem.CodigoRastreamento = codigoEl.GetString();
            if (responseData.TryGetProperty("id", out var idEl))
                prePostagem.IdPrePostagem = idEl.GetString();
            if (responseData.TryGetProperty("numeroEtiqueta", out var etiquetaEl))
                prePostagem.NumeroEtiqueta = etiquetaEl.GetString();
            
            if (!string.IsNullOrEmpty(prePostagem.CodigoRastreamento))
                pedido.CodigoRastreamento = prePostagem.CodigoRastreamento;

            _logger.LogInformation("[Correios] Pré-postagem criada com sucesso. Código: {Codigo}", prePostagem.CodigoRastreamento);

            _context.PrePostagens.Add(prePostagem);
            await _context.SaveChangesAsync();

            return await MapToResponseDto(prePostagem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao criar pré-postagem para pedido {PedidoId}", dto.PedidoId);
            throw;
        }
    }

    private object CriarRequestPrePostagem(Pedido pedido, CriarPrePostagemDto dto)
    {
        var endereco = pedido.EnderecoEntrega!;
        var config = _configuration;

        var telefoneRemetenteRaw = config["Correios:RemetenteTelefone"]?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "") ?? "";
        var dddRemetente = telefoneRemetenteRaw.Length >= 2 ? telefoneRemetenteRaw.Substring(0, 2) : "";
        var telefoneRemetente = telefoneRemetenteRaw.Length > 2 ? telefoneRemetenteRaw.Substring(2) : "";
        if (telefoneRemetente.Length > 9) telefoneRemetente = telefoneRemetente.Substring(0, 9);

        // Usar dados do snapshot do cliente no pedido
        var telefoneDestinatarioRaw = pedido.TelefoneCliente?.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "") ?? "";
        var dddDestinatario = telefoneDestinatarioRaw.Length >= 2 ? telefoneDestinatarioRaw.Substring(0, 2) : "";
        var telefoneDestinatario = telefoneDestinatarioRaw.Length > 2 ? telefoneDestinatarioRaw.Substring(2) : "";
        if (telefoneDestinatario.Length > 9) telefoneDestinatario = telefoneDestinatario.Substring(0, 9);

        var quantidadeItens = pedido.Carrinho?.Itens?.Sum(i => i.Quantidade) ?? 1;
        var valorProdutos = pedido.TotalPedido - (pedido.PrecoFrete ?? 0) + pedido.DescontoCupom;

        return new
        {
            idCorreios = Guid.NewGuid().ToString(),
            remetente = new
            {
                nome = config["Correios:RemetenteNome"],
                cpfCnpj = config["Correios:RemetenteCPFCNPJ"]?.Replace(".", "").Replace("-", "").Replace("/", ""),
                dddCelular = dddRemetente,
                celular = telefoneRemetente,
                email = config["Correios:RemetenteEmail"],
                endereco = new
                {
                    cep = config["Correios:RemetenteCEP"]?.Replace("-", ""),
                    logradouro = config["Correios:RemetenteLogradouro"],
                    numero = config["Correios:RemetenteNumero"],
                    bairro = config["Correios:RemetenteBairro"],
                    cidade = config["Correios:RemetenteCidade"],
                    uf = config["Correios:RemetenteUF"]
                }
            },
            destinatario = new
            {
                nome = pedido.NomeCliente,
                cpfCnpj = pedido.CpfCliente?.Replace(".", "").Replace("-", ""),
                dddCelular = dddDestinatario,
                celular = telefoneDestinatario,
                email = pedido.EmailCliente,
                endereco = new
                {
                    cep = endereco.Cep?.Replace("-", ""),
                    logradouro = endereco.Logradouro,
                    numero = endereco.Numero,
                    complemento = endereco.Complemento ?? "",
                    bairro = endereco.Bairro,
                    cidade = endereco.Cidade,
                    uf = endereco.Uf
                }
            },
            codigoServico = dto.CodigoServico,
            numeroCartaoPostagem = config["Correios:CartaoPostagem"],
            pesoInformado = ((int)((dto.Peso ?? 0.3m) * 1000)).ToString(),
            codigoFormatoObjetoInformado = "2",
            alturaInformada = (dto.Altura ?? 5).ToString(),
            larguraInformada = (dto.Largura ?? 15).ToString(),
            comprimentoInformado = (dto.Comprimento ?? 20).ToString(),
            cienteObjetoNaoProibido = 1,
            itensDeclaracaoConteudo = new[]
            {
                new
                {
                    conteudo = "Pecas e Acessorios Automotivos",
                    quantidade = quantidadeItens.ToString(),
                    valor = valorProdutos.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                }
            },
            listaServicoAdicional = new[]
            {
                new 
                { 
                    codigoServicoAdicional = "019",
                    tipoServicoAdicional = "AR",
                    valorDeclarado = valorProdutos.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                }
            },
            observacao = $"Pedido #{pedido.CodigoPedido}"
        };
    }

    public async Task<PrePostagemResponseDto?> GetPrePostagemByIdAsync(int id)
    {
        var prePostagem = await _context.PrePostagens
            .Include(p => p.Pedido)
                .ThenInclude(p => p.EnderecoEntrega)
            .FirstOrDefaultAsync(p => p.Id == id);

        return prePostagem != null ? await MapToResponseDto(prePostagem) : null;
    }

    public async Task<PrePostagemResponseDto?> GetPrePostagemByPedidoIdAsync(int pedidoId)
    {
        var prePostagem = await _context.PrePostagens
            .Include(p => p.Pedido)
                .ThenInclude(p => p.EnderecoEntrega)
            .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && p.Status != StatusPrePostagem.Cancelada);

        return prePostagem != null ? await MapToResponseDto(prePostagem) : null;
    }

    public async Task<PrePostagemPaginadoDto> GetPrePostagensAsync(PrePostagemFiltroDto filtro)
    {
        var query = _context.PrePostagens
            .Include(p => p.Pedido)
                .ThenInclude(p => p.EnderecoEntrega)
            .AsQueryable();

        if (filtro.Status.HasValue)
            query = query.Where(p => p.Status == filtro.Status.Value);

        if (filtro.DataInicio.HasValue)
            query = query.Where(p => p.DataCriacao >= filtro.DataInicio.Value);

        if (filtro.DataFim.HasValue)
            query = query.Where(p => p.DataCriacao <= filtro.DataFim.Value);

        var totalCount = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(p => p.DataCriacao)
            .Skip((filtro.PageNumber - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        var itemsDto = new List<PrePostagemResponseDto>();
        foreach (var item in items)
        {
            itemsDto.Add(await MapToResponseDto(item));
        }

        return new PrePostagemPaginadoDto
        {
            Items = itemsDto,
            TotalCount = totalCount,
            PageNumber = filtro.PageNumber,
            PageSize = filtro.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filtro.PageSize)
        };
    }

    public async Task<bool> CancelarPrePostagemAsync(int id)
    {
        var prePostagem = await _context.PrePostagens.FindAsync(id);
        if (prePostagem == null) return false;

        if (prePostagem.Status != StatusPrePostagem.Pendente && prePostagem.Status != StatusPrePostagem.Gerada)
            return false;

        if (!string.IsNullOrEmpty(prePostagem.IdPrePostagem))
        {
            try
            {
                var request = await CriarRequestAutenticadoAsync(HttpMethod.Delete, $"/prepostagem/v1/prepostagens/{prePostagem.IdPrePostagem}");
                var response = await _httpClient.SendAsync(request);
                _logger.LogInformation("[Correios] Cancelamento pré-postagem {Id}: {Status}", prePostagem.IdPrePostagem, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Correios] Erro ao cancelar pré-postagem nos Correios");
            }
        }

        prePostagem.Status = StatusPrePostagem.Cancelada;
        prePostagem.Observacoes = $"Cancelada em {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<PrePostagemResponseDto> MapToResponseDto(PrePostagem prePostagem)
    {
        if (prePostagem.Pedido == null)
        {
            await _context.Entry(prePostagem)
                .Reference(p => p.Pedido)
                .LoadAsync();
            
            if (prePostagem.Pedido != null)
            {
                await _context.Entry(prePostagem.Pedido)
                    .Reference(p => p.EnderecoEntrega)
                    .LoadAsync();
            }
        }

        var endereco = prePostagem.Pedido?.EnderecoEntrega;

        return new PrePostagemResponseDto
        {
            Id = prePostagem.Id,
            PedidoId = prePostagem.PedidoId,
            CodigoPedido = prePostagem.Pedido?.CodigoPedido,
            ClienteNome = prePostagem.Pedido?.NomeCliente,
            CodigoRastreamento = prePostagem.CodigoRastreamento,
            IdPrePostagem = prePostagem.IdPrePostagem,
            NumeroEtiqueta = prePostagem.NumeroEtiqueta,
            CodigoServico = prePostagem.CodigoServico,
            NomeServico = prePostagem.NomeServico,
            Peso = prePostagem.Peso,
            Altura = prePostagem.Altura,
            Largura = prePostagem.Largura,
            Comprimento = prePostagem.Comprimento,
            ValorDeclarado = prePostagem.ValorDeclarado,
            Status = prePostagem.Status,
            StatusNome = prePostagem.Status.ToString(),
            DataCriacao = prePostagem.DataCriacao,
            DataPostagem = prePostagem.DataPostagem,
            DataEntrega = prePostagem.DataEntrega,
            Observacoes = prePostagem.Observacoes,
            MensagemErro = prePostagem.MensagemErro,
            Destinatario = endereco != null ? new DestinatarioDto
            {
                Nome = prePostagem.Pedido?.NomeCliente,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento,
                Bairro = endereco.Bairro,
                Cidade = endereco.Cidade,
                UF = endereco.Uf,
                CEP = endereco.Cep
            } : null
        };
    }

    #endregion

    #region Listagem Pré-Postagens API Correios

    public async Task<PrePostagemCorreiosPaginadoDto> ListarPrePostagensCorreiosAsync(PrePostagemCorreiosFiltroDto filtro)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(filtro.Id)) queryParams.Add($"id={filtro.Id}");
            if (!string.IsNullOrEmpty(filtro.CodigoObjeto)) queryParams.Add($"codigoObjeto={filtro.CodigoObjeto}");
            if (!string.IsNullOrEmpty(filtro.ETicket)) queryParams.Add($"eTicket={filtro.ETicket}");
            if (!string.IsNullOrEmpty(filtro.CodigoEstampa2D)) queryParams.Add($"codigoEstampa2D={filtro.CodigoEstampa2D}");
            if (!string.IsNullOrEmpty(filtro.IdCorreios)) queryParams.Add($"idCorreios={filtro.IdCorreios}");
            if (!string.IsNullOrEmpty(filtro.Status)) queryParams.Add($"status={filtro.Status}");
            if (!string.IsNullOrEmpty(filtro.LogisticaReversa)) queryParams.Add($"logisticaReversa={filtro.LogisticaReversa}");
            if (!string.IsNullOrEmpty(filtro.TipoObjeto)) queryParams.Add($"tipoObjeto={filtro.TipoObjeto}");
            if (!string.IsNullOrEmpty(filtro.ModalidadePagamento)) queryParams.Add($"modalidadePagamento={filtro.ModalidadePagamento}");
            if (!string.IsNullOrEmpty(filtro.ObjetoCargo)) queryParams.Add($"objetoCargo={filtro.ObjetoCargo}");
            if (filtro.DataInicialCriacaoPrePostagem.HasValue) 
                queryParams.Add($"dataInicialCriacaoPrePostagem={filtro.DataInicialCriacaoPrePostagem.Value:yyyy-MM-dd}");
            if (filtro.DataFinalCriacaoPrePostagem.HasValue) 
                queryParams.Add($"dataFinalCriacaoPrePostagem={filtro.DataFinalCriacaoPrePostagem.Value:yyyy-MM-dd}");
            
            queryParams.Add($"page={filtro.Page}");
            queryParams.Add($"size={filtro.Size}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"/prepostagem/v2/prepostagens?{queryString}";

            var request = await CriarRequestAutenticadoAsync(HttpMethod.Get, endpoint);

            _logger.LogInformation("[Correios] Listando pré-postagens: {Endpoint}", endpoint);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta listagem: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Correios] Erro ao listar pré-postagens: {Content}", content);
                return new PrePostagemCorreiosPaginadoDto();
            }

            var data = JsonSerializer.Deserialize<JsonElement>(content);
            var resultado = new PrePostagemCorreiosPaginadoDto
            {
                Page = filtro.Page,
                Size = filtro.Size
            };

            if (data.TryGetProperty("itens", out var itensEl) || data.TryGetProperty("content", out itensEl))
            {
                foreach (var item in itensEl.EnumerateArray())
                {
                    var prePostagem = new PrePostagemCorreiosItemDto();
                    
                    if (item.TryGetProperty("id", out var idEl)) prePostagem.Id = idEl.GetString();
                    if (item.TryGetProperty("idCorreios", out var idCorreiosEl)) prePostagem.IdCorreios = idCorreiosEl.GetString();
                    if (item.TryGetProperty("codigoObjeto", out var codObjEl)) prePostagem.CodigoObjeto = codObjEl.GetString();
                    if (item.TryGetProperty("codigoServico", out var codServEl)) prePostagem.CodigoServico = codServEl.GetString();
                    if (item.TryGetProperty("status", out var statusEl)) prePostagem.Status = statusEl.GetString();
                    if (item.TryGetProperty("dataCriacao", out var dataCriacaoEl) && DateTime.TryParse(dataCriacaoEl.GetString(), out var dtCriacao))
                        prePostagem.DataCriacao = dtCriacao;
                    if (item.TryGetProperty("dataPostagem", out var dataPostEl) && DateTime.TryParse(dataPostEl.GetString(), out var dtPost))
                        prePostagem.DataPostagem = dtPost;
                    if (item.TryGetProperty("pesoInformado", out var pesoEl))
                        prePostagem.Peso = pesoEl.ValueKind == JsonValueKind.Number ? pesoEl.GetDecimal() : 0;
                    if (item.TryGetProperty("precoServico", out var precoEl))
                        prePostagem.PrecoServico = precoEl.ValueKind == JsonValueKind.Number ? precoEl.GetDecimal() : 0;

                    if (item.TryGetProperty("destinatario", out var destEl))
                        prePostagem.Destinatario = ParseRemetenteDestinatario(destEl);

                    if (item.TryGetProperty("remetente", out var remEl))
                        prePostagem.Remetente = ParseRemetenteDestinatario(remEl);

                    resultado.Itens.Add(prePostagem);
                }
            }

            if (data.TryGetProperty("totalElements", out var totalEl))
                resultado.TotalElements = totalEl.GetInt32();
            if (data.TryGetProperty("totalPages", out var pagesEl))
                resultado.TotalPages = pagesEl.GetInt32();
            if (data.TryGetProperty("first", out var firstEl))
                resultado.First = firstEl.GetBoolean();
            if (data.TryGetProperty("last", out var lastEl))
                resultado.Last = lastEl.GetBoolean();

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao listar pré-postagens");
            return new PrePostagemCorreiosPaginadoDto();
        }
    }

    private RemetenteDestinatarioDto ParseRemetenteDestinatario(JsonElement el)
    {
        var dto = new RemetenteDestinatarioDto();
        
        if (el.TryGetProperty("nome", out var nomeEl)) dto.Nome = nomeEl.GetString();
        if (el.TryGetProperty("cpfCnpj", out var cpfEl)) dto.CpfCnpj = cpfEl.GetString();
        if (el.TryGetProperty("email", out var emailEl)) dto.Email = emailEl.GetString();
        if (el.TryGetProperty("telefone", out var telEl)) dto.Telefone = telEl.GetString();
        if (el.TryGetProperty("celular", out var celEl)) dto.Telefone = celEl.GetString();
        
        if (el.TryGetProperty("endereco", out var endEl))
        {
            dto.Endereco = new EnderecoCorreiosDto();
            if (endEl.TryGetProperty("cep", out var cepEl)) dto.Endereco.Cep = cepEl.GetString();
            if (endEl.TryGetProperty("logradouro", out var logEl)) dto.Endereco.Logradouro = logEl.GetString();
            if (endEl.TryGetProperty("numero", out var numEl)) dto.Endereco.Numero = numEl.GetString();
            if (endEl.TryGetProperty("complemento", out var compEl)) dto.Endereco.Complemento = compEl.GetString();
            if (endEl.TryGetProperty("bairro", out var bairroEl)) dto.Endereco.Bairro = bairroEl.GetString();
            if (endEl.TryGetProperty("cidade", out var cidEl)) dto.Endereco.Cidade = cidEl.GetString();
            if (endEl.TryGetProperty("uf", out var ufEl)) dto.Endereco.Uf = ufEl.GetString();
        }

        return dto;
    }

    public async Task<PrePostagemPostadaDetalhesDto?> ConsultarPrePostagemPostadaAsync(string codigoObjeto)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Get, $"/prepostagem/v1/prepostagens/postada/{codigoObjeto}");

            _logger.LogInformation("[Correios] Consultando pré-postagem postada: {Codigo}", codigoObjeto);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta consulta postada: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Correios] Pré-postagem não encontrada: {Codigo}", codigoObjeto);
                return null;
            }

            var data = JsonSerializer.Deserialize<JsonElement>(content);
            return ParsePrePostagemPostada(data, codigoObjeto, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao consultar pré-postagem postada: {Codigo}", codigoObjeto);
            return null;
        }
    }

    private PrePostagemPostadaDetalhesDto ParsePrePostagemPostada(JsonElement data, string codigoObjeto, string jsonOriginal)
    {
        var resultado = new PrePostagemPostadaDetalhesDto
        {
            CodigoObjeto = codigoObjeto,
            RespostaJson = jsonOriginal
        };

        if (data.TryGetProperty("id", out var idEl)) resultado.IdPrePostagem = idEl.GetString();
        if (data.TryGetProperty("codigoServico", out var servEl)) 
        {
            resultado.CodigoServico = servEl.GetString();
            resultado.NomeServico = ServicosCorreios.GetNome(resultado.CodigoServico ?? "");
        }
        if (data.TryGetProperty("status", out var statusEl)) resultado.Status = statusEl.GetString();
        if (data.TryGetProperty("dataCriacao", out var dataCrEl) && DateTime.TryParse(dataCrEl.GetString(), out var dtCr))
            resultado.DataCriacao = dtCr;
        if (data.TryGetProperty("dataPostagem", out var dataPoEl) && DateTime.TryParse(dataPoEl.GetString(), out var dtPo))
            resultado.DataPostagem = dtPo;
        if (data.TryGetProperty("pesoInformado", out var pesoEl))
        {
            var pesoGramas = decimal.TryParse(pesoEl.GetString() ?? pesoEl.ToString(), out var p) ? p : 0;
            resultado.Peso = pesoGramas / 1000;
        }
        if (data.TryGetProperty("alturaInformada", out var altEl))
            resultado.Altura = decimal.TryParse(altEl.GetString() ?? altEl.ToString(), out var a) ? a : 0;
        if (data.TryGetProperty("larguraInformada", out var largEl))
            resultado.Largura = decimal.TryParse(largEl.GetString() ?? largEl.ToString(), out var l) ? l : 0;
        if (data.TryGetProperty("comprimentoInformado", out var compEl))
            resultado.Comprimento = decimal.TryParse(compEl.GetString() ?? compEl.ToString(), out var c) ? c : 0;
        if (data.TryGetProperty("precoServico", out var precoEl))
            resultado.PrecoServico = decimal.TryParse(precoEl.GetString() ?? precoEl.ToString(), out var pr) ? pr : 0;

        if (data.TryGetProperty("remetente", out var remEl))
            resultado.Remetente = ParseRemetenteDestinatario(remEl);
        if (data.TryGetProperty("destinatario", out var destEl))
            resultado.Destinatario = ParseRemetenteDestinatario(destEl);

        return resultado;
    }

    public async Task<PrePostagemPostadaDetalhesDto?> ConsultarPrePostagemCorreiosByIdAsync(string idPrePostagem)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Get, $"/prepostagem/v1/prepostagens/{idPrePostagem}");

            _logger.LogInformation("[Correios] Consultando pré-postagem por ID: {Id}", idPrePostagem);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta consulta por ID: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Correios] Pré-postagem não encontrada: {Id}", idPrePostagem);
                return null;
            }

            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            var resultado = new PrePostagemPostadaDetalhesDto { RespostaJson = content };
            
            if (data.TryGetProperty("codigoObjeto", out var codEl)) resultado.CodigoObjeto = codEl.GetString();
            if (data.TryGetProperty("id", out var idEl)) resultado.IdPrePostagem = idEl.GetString();
            if (data.TryGetProperty("codigoServico", out var servEl))
            {
                resultado.CodigoServico = servEl.GetString();
                resultado.NomeServico = ServicosCorreios.GetNome(resultado.CodigoServico ?? "");
            }
            if (data.TryGetProperty("status", out var statusEl)) resultado.Status = statusEl.GetString();
            if (data.TryGetProperty("dataCriacao", out var dataCrEl) && DateTime.TryParse(dataCrEl.GetString(), out var dtCr))
                resultado.DataCriacao = dtCr;
            if (data.TryGetProperty("dataPostagem", out var dataPoEl) && DateTime.TryParse(dataPoEl.GetString(), out var dtPo))
                resultado.DataPostagem = dtPo;
            if (data.TryGetProperty("pesoInformado", out var pesoEl))
            {
                var pesoGramas = decimal.TryParse(pesoEl.GetString() ?? pesoEl.ToString(), out var p) ? p : 0;
                resultado.Peso = pesoGramas / 1000;
            }
            if (data.TryGetProperty("alturaInformada", out var altEl))
                resultado.Altura = decimal.TryParse(altEl.GetString() ?? altEl.ToString(), out var a) ? a : 0;
            if (data.TryGetProperty("larguraInformada", out var largEl))
                resultado.Largura = decimal.TryParse(largEl.GetString() ?? largEl.ToString(), out var l) ? l : 0;
            if (data.TryGetProperty("comprimentoInformado", out var compEl))
                resultado.Comprimento = decimal.TryParse(compEl.GetString() ?? compEl.ToString(), out var c) ? c : 0;
            if (data.TryGetProperty("precoServico", out var precoEl))
                resultado.PrecoServico = decimal.TryParse(precoEl.GetString() ?? precoEl.ToString(), out var pr) ? pr : 0;
            if (data.TryGetProperty("remetente", out var remEl2))
                resultado.Remetente = ParseRemetenteDestinatario(remEl2);
            if (data.TryGetProperty("destinatario", out var destEl2))
                resultado.Destinatario = ParseRemetenteDestinatario(destEl2);
            
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao consultar pré-postagem por ID: {Id}", idPrePostagem);
            return null;
        }
    }

    public async Task<bool> CancelarPrePostagemCorreiosAsync(string idPrePostagem)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Delete, $"/prepostagem/v1/prepostagens/{idPrePostagem}");

            _logger.LogInformation("[Correios] Cancelando pré-postagem nos Correios: {Id}", idPrePostagem);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta cancelamento: {StatusCode}", response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao cancelar pré-postagem nos Correios: {Id}", idPrePostagem);
            return false;
        }
    }

    #endregion

    #region Geração de Rótulos

    public async Task<GerarRotuloResponseDto> GerarRotuloRangeAsync(GerarRotuloRangeRequestDto dto)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/prepostagem/v1/prepostagens/rotulo/range");
            
            var payload = new
            {
                codigoServico = dto.CodigoServico,
                quantidade = dto.Quantidade
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("[Correios] Gerando rótulos por range: {Servico} x {Qtd}", dto.CodigoServico, dto.Quantidade);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentType?.MediaType == "application/pdf")
                {
                    var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                    return new GerarRotuloResponseDto
                    {
                        Sucesso = true,
                        PdfBytes = pdfBytes,
                        Mensagem = "Rótulos gerados com sucesso"
                    };
                }

                var data = JsonSerializer.Deserialize<JsonElement>(content);
                return new GerarRotuloResponseDto
                {
                    Sucesso = true,
                    IdRecibo = data.TryGetProperty("idRecibo", out var idEl) ? idEl.GetString() : null,
                    UrlRotulo = data.TryGetProperty("urlRotulo", out var urlEl) ? urlEl.GetString() : null,
                    Mensagem = "Rótulos gerados com sucesso"
                };
            }

            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                Mensagem = $"Erro ao gerar rótulos: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao gerar rótulos por range");
            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                Mensagem = ex.Message
            };
        }
    }

    public async Task<GerarRotuloResponseDto> GerarRotuloLoteAsyncAsync(GerarRotuloLoteAsyncRequestDto dto)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/prepostagem/v1/prepostagens/rotulo/lote/assincrono/pdf");
            
            var payload = new
            {
                idCorreios = dto.IdCorreios ?? Guid.NewGuid().ToString(),
                numeroCartaoPostagem = dto.NumeroCartaoPostagem ?? _configuration["Correios:CartaoPostagem"],
                tipoRotulo = dto.TipoRotulo,
                formatoRotulo = dto.FormatoRotulo,
                idAtendimento = dto.IdAtendimento,
                imprimeRemetente = dto.ImprimeRemetente,
                idsLotePrePostagem = dto.IdsLotePrePostagem.Select(i => new
                {
                    idPrePostagem = i.IdPrePostagem,
                    codigoObjeto = i.CodigoObjeto,
                    sequencial = i.Sequencial
                }).ToList(),
                layoutImpressao = dto.LayoutImpressao
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("[Correios] Gerando rótulos em lote assíncrono: {Count} itens", dto.IdsLotePrePostagem.Count);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta geração rótulo lote: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                return new GerarRotuloResponseDto
                {
                    Sucesso = true,
                    IdRecibo = data.TryGetProperty("idRecibo", out var idEl) ? idEl.GetString() : null,
                    UrlRotulo = data.TryGetProperty("urlRotulo", out var urlEl) ? urlEl.GetString() : null,
                    Mensagem = "Solicitação de rótulos em lote enviada. Aguarde processamento."
                };
            }

            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                Mensagem = $"Erro ao gerar rótulos em lote: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao gerar rótulos em lote assíncrono");
            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                Mensagem = ex.Message
            };
        }
    }

    public async Task<GerarRotuloResponseDto> GerarRotuloRegistradoAsyncAsync(GerarRotuloRegistradoAsyncRequestDto dto)
    {
        try
        {
            // Validar se pelo menos um identificador foi fornecido
            var temCodigosObjeto = dto.CodigosObjeto != null && dto.CodigosObjeto.Count > 0;
            var temIdsPrePostagem = dto.IdsPrePostagem != null && dto.IdsPrePostagem.Count > 0;
            var temIdAtendimento = !string.IsNullOrEmpty(dto.IdAtendimento);
            
            if (!temCodigosObjeto && !temIdsPrePostagem && !temIdAtendimento)
            {
                return new GerarRotuloResponseDto
                {
                    Sucesso = false,
                    Mensagem = "É necessário informar CodigosObjeto, IdsPrePostagem ou IdAtendimento"
                };
            }
            
            _logger.LogInformation("[Correios] Gerando rótulo - CodigosObjeto: {CodigosObjeto}, IdsPrePostagem: {IdsPrePostagem}, IdAtendimento: {IdAtendimento}",
                temCodigosObjeto ? string.Join(",", dto.CodigosObjeto!) : "null",
                temIdsPrePostagem ? string.Join(",", dto.IdsPrePostagem!) : "null",
                dto.IdAtendimento ?? "null");

            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/prepostagem/v1/prepostagens/rotulo/assincrono/pdf");
            
            var idCorreios = dto.IdCorreios ?? Guid.NewGuid().ToString();
            var numeroCartao = dto.NumeroCartaoPostagem ?? _configuration["Correios:CartaoPostagem"];
            
            object payload;
            
            if (!string.IsNullOrEmpty(dto.IdAtendimento))
            {
                payload = new
                {
                    idAtendimento = dto.IdAtendimento,
                    idCorreios = idCorreios,
                    numeroCartaoPostagem = numeroCartao,
                    tipoRotulo = dto.TipoRotulo ?? "P",
                    formatoRotulo = dto.FormatoRotulo ?? "ET",
                    imprimeRemetente = dto.ImprimeRemetente ?? "S",
                    layoutImpressao = dto.LayoutImpressao ?? "PADRAO"
                };
            }
            else if (dto.IdsPrePostagem != null && dto.IdsPrePostagem.Count > 0)
            {
                payload = new
                {
                    idsPrePostagem = dto.IdsPrePostagem,
                    idCorreios = idCorreios,
                    numeroCartaoPostagem = numeroCartao,
                    tipoRotulo = dto.TipoRotulo ?? "P",
                    formatoRotulo = dto.FormatoRotulo ?? "ET",
                    imprimeRemetente = dto.ImprimeRemetente ?? "S",
                    layoutImpressao = dto.LayoutImpressao ?? "PADRAO"
                };
            }
            else
            {
                payload = new
                {
                    codigosObjeto = dto.CodigosObjeto,
                    idCorreios = idCorreios,
                    numeroCartaoPostagem = numeroCartao,
                    tipoRotulo = dto.TipoRotulo ?? "P",
                    formatoRotulo = dto.FormatoRotulo ?? "ET",
                    imprimeRemetente = dto.ImprimeRemetente ?? "S",
                    layoutImpressao = dto.LayoutImpressao ?? "PADRAO"
                };
            }

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            _logger.LogInformation("[Correios] Gerando rótulo registrado. Payload: {Payload}", jsonPayload);

            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                var idRecibo = data.TryGetProperty("idRecibo", out var idEl) ? idEl.GetString() : null;
                var urlRotulo = data.TryGetProperty("urlRotulo", out var urlEl) ? urlEl.GetString() : null;
                
                if (string.IsNullOrEmpty(idRecibo))
                {
                    return new GerarRotuloResponseDto
                    {
                        Sucesso = false,
                        Mensagem = "API dos Correios não retornou idRecibo."
                    };
                }
                
                return new GerarRotuloResponseDto
                {
                    Sucesso = true,
                    IdRecibo = idRecibo,
                    UrlRotulo = urlRotulo,
                    Mensagem = $"Rótulo em processamento. IdRecibo: {idRecibo}",
                    IdPedido = dto.IdPedido,
                    IdAtendimento = dto.IdAtendimento,
                    CodigosObjeto = dto.CodigosObjeto,
                    IdsPrePostagem = dto.IdsPrePostagem,
                    Observacao = dto.Observacao,
                    TipoRotulo = dto.TipoRotulo,
                    FormatoRotulo = dto.FormatoRotulo
                };
            }

            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                Mensagem = $"Erro HTTP {response.StatusCode}: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Exceção ao gerar rótulos assíncrono");
            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                Mensagem = $"Erro: {ex.Message}"
            };
        }
    }

    public async Task<GerarRotuloResponseDto> ConsultarRotuloAsync(string idRecibo, GerarRotuloRegistradoAsyncRequestDto? contexto = null)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Get, $"/prepostagem/v1/prepostagens/rotulo/download/assincrono/{idRecibo}");

            _logger.LogInformation("[Correios] Consultando rótulo assíncrono: {IdRecibo}", idRecibo);
            _logger.LogInformation("[Correios] Contexto recebido - IdPedido: {IdPedido}, CodigosObjeto: {CodigosObjeto}, IdsPrePostagem: {IdsPrePostagem}", 
                contexto?.IdPedido, 
                contexto?.CodigosObjeto != null ? string.Join(",", contexto.CodigosObjeto) : "null",
                contexto?.IdsPrePostagem != null ? string.Join(",", contexto.IdsPrePostagem) : "null");

            var response = await _httpClient.SendAsync(request);

            _logger.LogInformation("[Correios] Resposta consulta rótulo: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                
                if (contentType == "application/pdf")
                {
                    var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    // Gerar nome de arquivo mais descritivo
                    var nomeArquivo = contexto?.IdPedido != null 
                        ? $"rotulo_pedido_{contexto.IdPedido}_{idRecibo}.pdf"
                        : $"rotulo_{idRecibo}.pdf";
                    
                    // Salvar o rótulo no banco de dados
                    await SalvarRotuloAsync(
                        idRecibo: idRecibo,
                        pdfBytes: pdfBytes,
                        nomeArquivo: nomeArquivo,
                        idPedido: contexto?.IdPedido,
                        idAtendimento: contexto?.IdAtendimento,
                        codigosObjeto: contexto?.CodigosObjeto,
                        idsPrePostagem: contexto?.IdsPrePostagem,
                        observacao: contexto?.Observacao,
                        tipoRotulo: contexto?.TipoRotulo ?? "P",
                        formatoRotulo: contexto?.FormatoRotulo ?? "ET"
                    );
                    
                    return new GerarRotuloResponseDto
                    {
                        Sucesso = true,
                        IdRecibo = idRecibo,
                        PdfBytes = pdfBytes,
                        Mensagem = "Rótulo gerado e salvo com sucesso"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);

                if (data.TryGetProperty("dados", out var dadosEl) && !string.IsNullOrEmpty(dadosEl.GetString()))
                {
                    var base64Data = dadosEl.GetString();
                    if (!string.IsNullOrEmpty(base64Data))
                    {
                        var pdfBytes = Convert.FromBase64String(base64Data);
                        
                        // Gerar nome de arquivo mais descritivo
                        var nomeArquivo = contexto?.IdPedido != null 
                            ? $"rotulo_pedido_{contexto.IdPedido}_{idRecibo}.pdf"
                            : (data.TryGetProperty("nome", out var nomeEl) ? nomeEl.GetString() : $"rotulo_{idRecibo}.pdf");
                        
                        // Salvar o rótulo no banco de dados
                        await SalvarRotuloAsync(
                            idRecibo: idRecibo,
                            pdfBytes: pdfBytes,
                            nomeArquivo: nomeArquivo,
                            idPedido: contexto?.IdPedido,
                            idAtendimento: contexto?.IdAtendimento,
                            codigosObjeto: contexto?.CodigosObjeto,
                            idsPrePostagem: contexto?.IdsPrePostagem,
                            observacao: contexto?.Observacao,
                            tipoRotulo: contexto?.TipoRotulo ?? "P",
                            formatoRotulo: contexto?.FormatoRotulo ?? "ET"
                        );
                        
                        return new GerarRotuloResponseDto
                        {
                            Sucesso = true,
                            IdRecibo = idRecibo,
                            PdfBytes = pdfBytes,
                            Mensagem = $"Rótulo gerado e salvo: {nomeArquivo}"
                        };
                    }
                }

                if (data.TryGetProperty("urlRotulo", out var urlEl) && !string.IsNullOrEmpty(urlEl.GetString()))
                {
                    return new GerarRotuloResponseDto
                    {
                        Sucesso = true,
                        IdRecibo = idRecibo,
                        UrlRotulo = urlEl.GetString(),
                        Mensagem = "Rótulo disponível para download"
                    };
                }

                return new GerarRotuloResponseDto
                {
                    Sucesso = true,
                    IdRecibo = idRecibo,
                    Mensagem = "Aguardando processamento. Tente novamente em alguns segundos."
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return new GerarRotuloResponseDto
                {
                    Sucesso = false,
                    IdRecibo = idRecibo,
                    Mensagem = "Rótulo ainda em processamento. Tente novamente em alguns segundos."
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                IdRecibo = idRecibo,
                Mensagem = $"Erro ao consultar rótulo: {response.StatusCode} - {errorContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao consultar rótulo assíncrono: {IdRecibo}", idRecibo);
            return new GerarRotuloResponseDto
            {
                Sucesso = false,
                IdRecibo = idRecibo,
                Mensagem = ex.Message
            };
        }
    }

    #endregion

    #region Suspensão de Entrega

    public async Task<SuspenderEntregaResponseDto> SuspenderEntregaAsync(SuspenderEntregaRequestDto dto)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/srointeratividade/v1/solicitacoes/suspender");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    codigoObjeto = dto.CodigoRastreamento,
                    motivoSuspensao = dto.Motivo,
                    numeroCartaoPostagem = _configuration["Correios:CartaoPostagem"]
                }),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("[Correios] Suspendendo entrega {Codigo}...", dto.CodigoRastreamento);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta suspensão: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                return new SuspenderEntregaResponseDto
                {
                    Sucesso = true,
                    CodigoRastreamento = dto.CodigoRastreamento,
                    Mensagem = "Entrega suspensa com sucesso",
                    DataSuspensao = DateTime.UtcNow
                };
            }

            return new SuspenderEntregaResponseDto
            {
                Sucesso = false,
                CodigoRastreamento = dto.CodigoRastreamento,
                Mensagem = $"Erro ao suspender: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao suspender entrega {Codigo}", dto.CodigoRastreamento);
            return new SuspenderEntregaResponseDto
            {
                Sucesso = false,
                CodigoRastreamento = dto.CodigoRastreamento,
                Mensagem = $"Erro: {ex.Message}"
            };
        }
    }

    public async Task<SuspenderEntregaResponseDto> ReativarEntregaAsync(string codigoRastreamento)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/srointeratividade/v1/solicitacoes/reativar");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    codigoObjeto = codigoRastreamento,
                    numeroCartaoPostagem = _configuration["Correios:CartaoPostagem"]
                }),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("[Correios] Reativando entrega {Codigo}...", codigoRastreamento);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new SuspenderEntregaResponseDto
                {
                    Sucesso = true,
                    CodigoRastreamento = codigoRastreamento,
                    Mensagem = "Entrega reativada com sucesso"
                };
            }

            return new SuspenderEntregaResponseDto
            {
                Sucesso = false,
                CodigoRastreamento = codigoRastreamento,
                Mensagem = $"Erro ao reativar: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao reativar entrega {Codigo}", codigoRastreamento);
            return new SuspenderEntregaResponseDto
            {
                Sucesso = false,
                CodigoRastreamento = codigoRastreamento,
                Mensagem = $"Erro: {ex.Message}"
            };
        }
    }

    #endregion

    #region Atualização de Status

    public async Task AtualizarStatusPrePostagensAsync()
    {
        var prePostagens = await _context.PrePostagens
            .Where(p => !string.IsNullOrEmpty(p.CodigoRastreamento) &&
                       (p.Status == StatusPrePostagem.Gerada || 
                        p.Status == StatusPrePostagem.Postada || 
                        p.Status == StatusPrePostagem.EmTransito))
            .ToListAsync();

        foreach (var prePostagem in prePostagens)
        {
            try
            {
                var rastreamento = await RastrearObjetoAsync(prePostagem.CodigoRastreamento!);
                if (rastreamento?.Eventos.Count > 0)
                {
                    var ultimoEvento = rastreamento.Eventos.First();
                    
                    if (ultimoEvento.Tipo == "BDE" || ultimoEvento.Descricao?.Contains("Entregue") == true)
                    {
                        prePostagem.Status = StatusPrePostagem.Entregue;
                        prePostagem.DataEntrega = ultimoEvento.DataHora;
                    }
                    else if (ultimoEvento.Tipo == "BDI" || ultimoEvento.Descricao?.Contains("Devolvido") == true)
                    {
                        prePostagem.Status = StatusPrePostagem.Devolvido;
                    }
                    else if (ultimoEvento.Descricao?.Contains("postado") == true)
                    {
                        prePostagem.Status = StatusPrePostagem.Postada;
                        prePostagem.DataPostagem = ultimoEvento.DataHora;
                    }
                    else if (prePostagem.Status == StatusPrePostagem.Gerada || prePostagem.Status == StatusPrePostagem.Postada)
                    {
                        prePostagem.Status = StatusPrePostagem.EmTransito;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Correios] Erro ao atualizar status da pré-postagem {Id}", prePostagem.Id);
            }
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Salvamento de Rótulos

    public async Task<Rotulo?> SalvarRotuloAsync(
        string idRecibo, 
        byte[] pdfBytes, 
        string? nomeArquivo = null,
        int? idPedido = null,
        string? idAtendimento = null,
        List<string>? codigosObjeto = null,
        List<string>? idsPrePostagem = null,
        string? observacao = null,
        string tipoRotulo = "P",
        string formatoRotulo = "ET")
    {
        try
        {
            // Verificar se já existe um rótulo com este idRecibo
            var rotuloExistente = await _context.Rotulos
                .FirstOrDefaultAsync(r => r.IdRecibo == idRecibo);
            
            if (rotuloExistente != null)
            {
                _logger.LogInformation("[Correios] Rótulo com IdRecibo {IdRecibo} já existe no banco (ID: {Id})", idRecibo, rotuloExistente.Id);
                return rotuloExistente;
            }

            // Criar diretório rotulos se não existir
            var rotulosPath = Path.Combine("wwwroot", "rotulos");
            if (!Directory.Exists(rotulosPath))
            {
                Directory.CreateDirectory(rotulosPath);
                _logger.LogInformation("[Correios] Diretório de rótulos criado: {Path}", rotulosPath);
            }

            // Nome do arquivo
            var nome = nomeArquivo ?? $"rotulo_{idRecibo}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            if (!nome.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                nome += ".pdf";
            }

            var caminhoCompleto = Path.Combine(rotulosPath, nome);

            // Salvar o arquivo
            await File.WriteAllBytesAsync(caminhoCompleto, pdfBytes);
            _logger.LogInformation("[Correios] 💾 Rótulo salvo em disco: {Path} ({Size} bytes)", caminhoCompleto, pdfBytes.Length);

            // Salvar no banco de dados
            var rotulo = new Rotulo
            {
                IdPedido = idPedido,
                IdRecibo = idRecibo,
                IdAtendimento = idAtendimento,
                NomeArquivo = nome,
                CaminhoArquivo = $"/rotulos/{nome}",
                DataGeracao = DateTime.UtcNow,
                QuantidadeRotulos = codigosObjeto?.Count ?? idsPrePostagem?.Count ?? 1,
                CodigosObjeto = codigosObjeto != null && codigosObjeto.Any() 
                    ? JsonSerializer.Serialize(codigosObjeto) 
                    : null,
                IdsPrePostagem = idsPrePostagem != null && idsPrePostagem.Any() 
                    ? JsonSerializer.Serialize(idsPrePostagem) 
                    : null,
                TipoRotulo = tipoRotulo,
                FormatoRotulo = formatoRotulo,
                TamanhoBytes = pdfBytes.Length,
                Observacao = observacao
            };

            _context.Rotulos.Add(rotulo);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Correios] ✓ Registro de rótulo salvo no banco: ID {Id}, IdRecibo {IdRecibo}", rotulo.Id, idRecibo);
            return rotulo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao salvar rótulo no servidor");
            return null;
        }
    }

    public async Task<Rotulo?> BuscarRotuloPorIdReciboAsync(string idRecibo)
    {
        return await _context.Rotulos
            .FirstOrDefaultAsync(r => r.IdRecibo == idRecibo);
    }

    public async Task<List<Rotulo>> ListarRotulosAsync(int? idPedido = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Rotulos.AsQueryable();
        
        if (idPedido.HasValue)
        {
            query = query.Where(r => r.IdPedido == idPedido.Value);
        }
        
        return await query
            .OrderByDescending(r => r.DataGeracao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    #endregion

    #region Pré-Postagem Genérica (Logística Reversa)

    /// <summary>
    /// Cria pré-postagem genérica com payload customizado (usado para logística reversa)
    /// </summary>
    public async Task<PrePostagemGenericaResponseDto?> CriarPrePostagemGenericaAsync(object payload)
    {
        try
        {
            var request = await CriarRequestAutenticadoAsync(HttpMethod.Post, "/prepostagem/v1/prepostagens");

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogInformation("[Correios] Criando pré-postagem genérica...");
            _logger.LogDebug("[Correios] Payload: {Payload}", jsonPayload);

            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[Correios] Resposta pré-postagem genérica: {StatusCode} - {Content}", 
                response.StatusCode, content.Length > 500 ? content.Substring(0, 500) + "..." : content);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                
                var idPrePostagem = data.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var codigoObjeto = data.TryGetProperty("codigoObjeto", out var codEl) ? codEl.GetString() : null;
                DateTime? prazoPostagem = null;
                
                if (data.TryGetProperty("prazoPostagem", out var prazoEl))
                {
                    if (DateTime.TryParse(prazoEl.GetString(), out var prazo))
                    {
                        prazoPostagem = prazo;
                    }
                }

                return new PrePostagemGenericaResponseDto
                {
                    Sucesso = true,
                    IdPrePostagem = idPrePostagem,
                    CodigoRastreamento = codigoObjeto,
                    PrazoPostagem = prazoPostagem,
                    RespostaJson = content,
                    Mensagem = "Pré-postagem criada com sucesso"
                };
            }

            return new PrePostagemGenericaResponseDto
            {
                Sucesso = false,
                Mensagem = $"Erro HTTP {response.StatusCode}: {content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Correios] Erro ao criar pré-postagem genérica");
            return new PrePostagemGenericaResponseDto
            {
                Sucesso = false,
                Mensagem = $"Erro: {ex.Message}"
            };
        }
    }

    #endregion
}
