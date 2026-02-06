using g4api.Data;
using g4api.DTOs;
using g4api.Extensions;
using g4api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace g4api.Services;

public class ProdutoService : IProdutoService
{
    private readonly G4DbContext _context;

    public ProdutoService(G4DbContext context)
    {
        _context = context;
    }

    #region Listagem e Busca

    public async Task<ProdutoListResponse> GetProdutosAsync(ProdutoBuscaParams p)
    {
        var query = _context.Produtos
            .Where(x => !string.IsNullOrEmpty(x.TituloEcommerceProduto))
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrEmpty(p.Termo))
        {
            var termo = p.Termo.ToLower();
            query = query.Where(x => 
                (x.TituloEcommerceProduto != null && x.TituloEcommerceProduto.ToLower().Contains(termo)) ||
                (x.ReferenciaProduto != null && x.ReferenciaProduto.ToLower().Contains(termo)) ||
                (x.Fabricante != null && x.Fabricante.ToLower().Contains(termo)));
        }

        if (p.GrupoId.HasValue)
            query = query.Where(x => x.GrupoProduto == p.GrupoId.Value);

        if (!string.IsNullOrEmpty(p.Marca))
            query = query.Where(x => x.Fabricante != null && x.Fabricante.ToLower() == p.Marca.ToLower());

        if (p.PrecoMin.HasValue)
            query = query.Where(x => x.PrecoMinimoProduto >= p.PrecoMin.Value);

        if (p.PrecoMax.HasValue)
            query = query.Where(x => x.PrecoMinimoProduto <= p.PrecoMax.Value);

        if (!string.IsNullOrEmpty(p.Cor))
            query = query.Where(x => x.CorPredominanteProduto != null && x.CorPredominanteProduto.ToLower().Contains(p.Cor.ToLower()));

        if (!string.IsNullOrEmpty(p.Tamanho))
            query = query.Where(x => x.TamanhoProduto != null && x.TamanhoProduto.ToLower() == p.Tamanho.ToLower());

        // Total antes da paginação
        var total = await query.CountAsync();

        // Ordenação
        query = p.OrderBy?.ToLower() switch
        {
            "preco" => p.Desc ? query.OrderByDescending(x => x.PrecoMinimoProduto) : query.OrderBy(x => x.PrecoMinimoProduto),
            "nome" => p.Desc ? query.OrderByDescending(x => x.TituloEcommerceProduto) : query.OrderBy(x => x.TituloEcommerceProduto),
            "marca" => p.Desc ? query.OrderByDescending(x => x.Fabricante) : query.OrderBy(x => x.Fabricante),
            "data" => p.Desc ? query.OrderByDescending(x => x.DataAlteracao) : query.OrderBy(x => x.DataAlteracao),
            _ => query.OrderBy(x => x.TituloEcommerceProduto)
        };

        // Paginação
        var items = await query
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        return new ProdutoListResponse
        {
            Items = items.Select(MapToDto).ToList(),
            Total = total,
            Page = p.Page,
            PageSize = p.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)p.PageSize)
        };
    }

    public async Task<IEnumerable<ProdutoDto>> GetProdutosAleatoriosAsync(int quantidade = 10)
    {
        var produtos = await _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.TituloEcommerceProduto) && p.PrecoMinimoProduto > 0)
            .OrderBy(p => Guid.NewGuid())
            .Take(quantidade)
            .ToListAsync();

        return produtos.Select(MapToDto);
    }

    public async Task<ProdutoListResponse> GetProdutosAtivosAsync(ProdutoBuscaParams p)
    {
        var produtosComEstoque = await _context.ProdutoGrades
            .Where(g => g.QtdProduto > 0)
            .Select(g => g.IdProdutoPrincipal)
            .Distinct()
            .ToListAsync();

        var query = _context.Produtos
            .Where(x => !string.IsNullOrEmpty(x.TituloEcommerceProduto) && 
                        produtosComEstoque.Contains(x.IdProduto))
            .AsQueryable();

        if (!string.IsNullOrEmpty(p.Termo))
        {
            var termo = p.Termo.ToLower();
            query = query.Where(x => 
                (x.TituloEcommerceProduto != null && x.TituloEcommerceProduto.ToLower().Contains(termo)) ||
                (x.ReferenciaProduto != null && x.ReferenciaProduto.ToLower().Contains(termo)) ||
                (x.Fabricante != null && x.Fabricante.ToLower().Contains(termo)));
        }

        if (p.GrupoId.HasValue)
            query = query.Where(x => x.GrupoProduto == p.GrupoId.Value);

        if (!string.IsNullOrEmpty(p.Marca))
            query = query.Where(x => x.Fabricante != null && x.Fabricante.ToLower() == p.Marca.ToLower());

        if (p.PrecoMin.HasValue)
            query = query.Where(x => x.PrecoMinimoProduto >= p.PrecoMin.Value);

        if (p.PrecoMax.HasValue)
            query = query.Where(x => x.PrecoMinimoProduto <= p.PrecoMax.Value);

        var total = await query.CountAsync();

        query = p.OrderBy?.ToLower() switch
        {
            "preco" => p.Desc ? query.OrderByDescending(x => x.PrecoMinimoProduto) : query.OrderBy(x => x.PrecoMinimoProduto),
            "nome" => p.Desc ? query.OrderByDescending(x => x.TituloEcommerceProduto) : query.OrderBy(x => x.TituloEcommerceProduto),
            "marca" => p.Desc ? query.OrderByDescending(x => x.Fabricante) : query.OrderBy(x => x.Fabricante),
            "data" => p.Desc ? query.OrderByDescending(x => x.DataAlteracao) : query.OrderBy(x => x.DataAlteracao),
            _ => query.OrderBy(x => x.TituloEcommerceProduto)
        };

        var items = await query
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        var produtoIds = items.Select(i => i.IdProduto).ToList();
        var grades = await _context.ProdutoGrades
            .Where(g => produtoIds.Contains(g.IdProdutoPrincipal))
            .ToListAsync();

        var gradeIds = grades.Select(g => g.IdProdutoGrade).ToList();
        var gradeImagens = await _context.ProdutoGradeImagens
            .Where(i => gradeIds.Contains(i.IdProdutoGrade))
            .OrderBy(i => i.Ordem)
            .ToListAsync();

        var produtosDto = items.Select(prod => {
            var dto = MapToDto(prod);
            dto.Grades = grades.Where(g => g.IdProdutoPrincipal == prod.IdProduto)
                              .Select(g => MapGradeToDto(g, gradeImagens.Where(i => i.IdProdutoGrade == g.IdProdutoGrade).ToList())).ToList();
            return dto;
        }).ToList();

        return new ProdutoListResponse
        {
            Items = produtosDto,
            Total = total,
            Page = p.Page,
            PageSize = p.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)p.PageSize)
        };
    }

    public async Task<ProdutoListResponse> GetProdutosInativosAsync(ProdutoBuscaParams p)
    {
        var produtosComEstoque = await _context.ProdutoGrades
            .Where(g => g.QtdProduto > 0)
            .Select(g => g.IdProdutoPrincipal)
            .Distinct()
            .ToListAsync();

        var query = _context.Produtos
            .Where(x => !string.IsNullOrEmpty(x.TituloEcommerceProduto) && 
                        !produtosComEstoque.Contains(x.IdProduto))
            .AsQueryable();

        if (!string.IsNullOrEmpty(p.Termo))
        {
            var termo = p.Termo.ToLower();
            query = query.Where(x => 
                (x.TituloEcommerceProduto != null && x.TituloEcommerceProduto.ToLower().Contains(termo)) ||
                (x.ReferenciaProduto != null && x.ReferenciaProduto.ToLower().Contains(termo)) ||
                (x.Fabricante != null && x.Fabricante.ToLower().Contains(termo)));
        }

        if (p.GrupoId.HasValue)
            query = query.Where(x => x.GrupoProduto == p.GrupoId.Value);

        if (!string.IsNullOrEmpty(p.Marca))
            query = query.Where(x => x.Fabricante != null && x.Fabricante.ToLower() == p.Marca.ToLower());

        var total = await query.CountAsync();

        query = p.OrderBy?.ToLower() switch
        {
            "preco" => p.Desc ? query.OrderByDescending(x => x.PrecoMinimoProduto) : query.OrderBy(x => x.PrecoMinimoProduto),
            "nome" => p.Desc ? query.OrderByDescending(x => x.TituloEcommerceProduto) : query.OrderBy(x => x.TituloEcommerceProduto),
            _ => query.OrderBy(x => x.TituloEcommerceProduto)
        };

        var items = await query
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        var produtoIds = items.Select(i => i.IdProduto).ToList();
        var grades = await _context.ProdutoGrades
            .Where(g => produtoIds.Contains(g.IdProdutoPrincipal))
            .ToListAsync();

        var gradeIds = grades.Select(g => g.IdProdutoGrade).ToList();
        var gradeImagens = await _context.ProdutoGradeImagens
            .Where(i => gradeIds.Contains(i.IdProdutoGrade))
            .OrderBy(i => i.Ordem)
            .ToListAsync();

        var produtosDto = items.Select(prod => {
            var dto = MapToDto(prod);
            dto.Grades = grades.Where(g => g.IdProdutoPrincipal == prod.IdProduto)
                              .Select(g => MapGradeToDto(g, gradeImagens.Where(i => i.IdProdutoGrade == g.IdProdutoGrade).ToList())).ToList();
            return dto;
        }).ToList();

        return new ProdutoListResponse
        {
            Items = produtosDto,
            Total = total,
            Page = p.Page,
            PageSize = p.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)p.PageSize)
        };
    }

    public async Task<IEnumerable<ProdutoDto>> GetProdutosRecentesAsync(int quantidade = 10)
    {
        var produtos = await _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.TituloEcommerceProduto) && 
                        p.PrecoMinimoProduto > 0 &&
                        p.DataAlteracao != null)
            .OrderByDescending(p => p.DataAlteracao)
            .Take(quantidade)
            .ToListAsync();

        return produtos.Select(MapToDto);
    }

    public async Task<ProdutoDto?> GetProdutoByIdAsync(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null) return null;

        var dto = MapToDto(produto);
        
        // Carrega as grades
        var grades = await _context.ProdutoGrades
            .Where(g => g.IdProdutoPrincipal == id)
            .ToListAsync();

        var gradeIds = grades.Select(g => g.IdProdutoGrade).ToList();
        var gradeImagens = await _context.ProdutoGradeImagens
            .Where(i => gradeIds.Contains(i.IdProdutoGrade))
            .OrderBy(i => i.Ordem)
            .ToListAsync();
        
        dto.Grades = grades.Select(g => MapGradeToDto(g, gradeImagens.Where(i => i.IdProdutoGrade == g.IdProdutoGrade).ToList())).ToList();
        
        return dto;
    }

    public async Task<ProdutoDto?> GetProdutoByReferenciaAsync(string referencia)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.ReferenciaProduto == referencia);
        
        if (produto == null) return null;

        var dto = MapToDto(produto);
        
        var grades = await _context.ProdutoGrades
            .Where(g => g.IdProdutoPrincipal == produto.IdProduto)
            .ToListAsync();

        var gradeIds = grades.Select(g => g.IdProdutoGrade).ToList();
        var gradeImagens = await _context.ProdutoGradeImagens
            .Where(i => gradeIds.Contains(i.IdProdutoGrade))
            .OrderBy(i => i.Ordem)
            .ToListAsync();
        
        dto.Grades = grades.Select(g => MapGradeToDto(g, gradeImagens.Where(i => i.IdProdutoGrade == g.IdProdutoGrade).ToList())).ToList();
        
        return dto;
    }

    public async Task<ProdutoDetalhadoDto?> GetProdutoDetalhadoAsync(string referencia)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.ReferenciaProduto == referencia);
        
        if (produto == null) return null;

        var grades = await _context.ProdutoGrades
            .Where(g => g.IdProdutoPrincipal == produto.IdProduto)
            .ToListAsync();

        // Buscar imagens das grades
        var gradeIds = grades.Select(g => g.IdProdutoGrade).ToList();
        var gradeImagens = await _context.ProdutoGradeImagens
            .Where(i => gradeIds.Contains(i.IdProdutoGrade))
            .OrderBy(i => i.Ordem)
            .ToListAsync();

        var produtosIds = grades.Select(g => g.IdProduto).Distinct().ToList();
        produtosIds.Add(produto.IdProduto);
        
        var produtosRelacionados = await _context.Produtos
            .Where(p => produtosIds.Contains(p.IdProduto))
            .ToListAsync();

        var precoOriginal = produto.PrecoTabelaProduto;
        var precoAtual = produto.PrecoMinimoProduto;
        int? desconto = null;
        if (precoOriginal > precoAtual && precoAtual > 0)
        {
            desconto = (int)Math.Round((1 - (precoAtual / precoOriginal)) * 100);
        }

        var coresAgrupadas = grades
            .Where(g => !string.IsNullOrEmpty(g.CorPredominanteProduto))
            .GroupBy(g => g.CorPredominanteProduto!)
            .Select(group => {
                // Pegar a primeira grade do grupo para buscar as imagens
                var primeiraGrade = group.FirstOrDefault(g => !string.IsNullOrEmpty(g.Img)) ?? group.First();
                var imagensDaCor = gradeImagens
                    .Where(i => i.IdProdutoGrade == primeiraGrade.IdProdutoGrade)
                    .Select(i => new ProdutoGradeImagemDto
                    {
                        Id = i.Id,
                        IdProdutoGrade = i.IdProdutoGrade,
                        Ordem = i.Ordem,
                        Caminho = i.Caminho
                    })
                    .ToList();

                return new CorDisponivelDto
                {
                    Nome = group.Key,
                    Img = primeiraGrade.Img 
                          ?? produtosRelacionados.FirstOrDefault(p => p.IdProduto == primeiraGrade.IdProduto)?.Img,
                    Tamanhos = group
                        .Where(g => !string.IsNullOrEmpty(g.TamanhoProduto))
                        .Select(g => g.TamanhoProduto!)
                        .Distinct()
                        .OrderBy(t => t)
                        .ToList(),
                    Imagens = imagensDaCor
                };
            })
            .ToList();

        if (!coresAgrupadas.Any() && !string.IsNullOrEmpty(produto.CorPredominanteProduto))
        {
            coresAgrupadas.Add(new CorDisponivelDto
            {
                Nome = produto.CorPredominanteProduto,
                Img = produto.Img,
                Tamanhos = !string.IsNullOrEmpty(produto.TamanhoProduto) 
                    ? new List<string> { produto.TamanhoProduto } 
                    : new List<string>(),
                Imagens = new List<ProdutoGradeImagemDto>()
            });
        }

        var tamanhosAgrupados = grades
            .Where(g => !string.IsNullOrEmpty(g.TamanhoProduto))
            .GroupBy(g => g.TamanhoProduto!)
            .Select(group => new TamanhoDisponivelDto
            {
                Nome = group.Key,
                Quantidade = group.Sum(g => g.QtdProduto)
            })
            .OrderBy(t => t.Nome)
            .ToList();

        if (!tamanhosAgrupados.Any() && !string.IsNullOrEmpty(produto.TamanhoProduto))
        {
            tamanhosAgrupados.Add(new TamanhoDisponivelDto
            {
                Nome = produto.TamanhoProduto,
                Quantidade = 1
            });
        }

        var variacoes = grades.Select(g => {
            var produtoVariacao = produtosRelacionados.FirstOrDefault(p => p.IdProduto == g.IdProduto);
            var imagensDaGrade = gradeImagens
                .Where(i => i.IdProdutoGrade == g.IdProdutoGrade)
                .Select(i => new ProdutoGradeImagemDto
                {
                    Id = i.Id,
                    IdProdutoGrade = i.IdProdutoGrade,
                    Ordem = i.Ordem,
                    Caminho = i.Caminho
                })
                .ToList();
            
            return new VariacaoDto
            {
                Id = g.IdProdutoGrade,
                IdProduto = g.IdProduto,
                Cor = g.CorPredominanteProduto,
                Tamanho = g.TamanhoProduto,
                Quantidade = g.QtdProduto,
                Img = g.Img ?? produtoVariacao?.Img,
                Preco = produtoVariacao?.PrecoMinimoProduto,
                Imagens = imagensDaGrade
            };
        }).ToList();

        return new ProdutoDetalhadoDto
        {
            Id = produto.IdProduto,
            Nome = produto.TituloEcommerceProduto ?? "",
            Referencia = produto.ReferenciaProduto ?? "",
            Descricao = produto.DescricaoConsultaProduto,
            AplicacaoConsulta = produto.AplicacaoConsultaProduto,
            DadosAdicionais = produto.DadosAdicionaisProduto,
            Marca = produto.Fabricante ?? "",
            Grupo = produto.GrupoProduto,
            Preco = precoAtual,
            PrecoOriginal = precoOriginal > precoAtual ? precoOriginal : null,
            Desconto = desconto,
            Img = produto.Img,
            Ean = produto.EanProduto,
            Peso = produto.PesoProduto,
            Altura = produto.AlturaProduto,
            Largura = produto.LarguraProduto,
            Comprimento = produto.ComprimentoProduto,
            CoresDisponiveis = coresAgrupadas,
            TamanhosDisponiveis = tamanhosAgrupados,
            Variacoes = variacoes
        };
    }

    public async Task<IEnumerable<ProdutoDto>> GetProdutosByGrupoAsync(string grupo, int quantidade = 20)
    {
        var grupoNormalizado = grupo.ToLower().Trim();

        // Busca o grupo pelo nome para pegar o ID
        var produtoGrupo = await _context.ProdutoGrupos
            .FirstOrDefaultAsync(g => g.DescricaoGrupoProduto != null && 
                                      g.DescricaoGrupoProduto.ToLower() == grupoNormalizado);

        if (produtoGrupo == null)
            return Enumerable.Empty<ProdutoDto>();

        // Usa o método por ID
        return await GetProdutosByGrupoIdAsync(produtoGrupo.IdProdutoGrupo, quantidade);
    }

    public async Task<IEnumerable<ProdutoDto>> GetProdutosByGrupoIdAsync(int grupoId, int quantidade = 20)
    {
        // Busca todos os subGrupos desse grupo
        var subGrupoIds = await _context.ProdutoSubGrupos
            .Where(sg => sg.IdProdutoGrupo == grupoId)
            .Select(sg => sg.IdProdutoSubGrupo)
            .ToListAsync();

        // GrupoProduto na tabela produto referencia o SubGrupo
        var produtos = await _context.Produtos
            .Where(p => p.GrupoProduto != null &&
                        subGrupoIds.Contains(p.GrupoProduto.Value) &&
                        !string.IsNullOrEmpty(p.TituloEcommerceProduto) &&
                        p.PrecoMinimoProduto > 0)
            .OrderBy(p => Guid.NewGuid())
            .Take(quantidade)
            .ToListAsync();

        return produtos.Select(MapToDto);
    }

    public async Task<IEnumerable<ProdutoDto>> GetProdutosBySubGrupoIdAsync(int subGrupoId, int quantidade = 20)
    {
        // GrupoProduto na tabela produto referencia o SubGrupo
        var produtos = await _context.Produtos
            .Where(p => p.GrupoProduto == subGrupoId &&
                        !string.IsNullOrEmpty(p.TituloEcommerceProduto) &&
                        p.PrecoMinimoProduto > 0)
            .OrderBy(p => Guid.NewGuid())
            .Take(quantidade)
            .ToListAsync();

        return produtos.Select(MapToDto);
    }

    public async Task<IEnumerable<string>> GetGruposDistintosAsync()
    {
        return await _context.Produtos
            .Where(p => p.GrupoProduto.HasValue)
            .Select(p => p.GrupoProduto!.Value.ToString())
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetMarcasDistintasAsync()
    {
        return await _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.Fabricante) && p.Fabricante != "-1")
            .Select(p => p.Fabricante!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<ProdutoPaginatedResponse> GetProdutosPaginadosAsync(
        int pageNumber, int pageSize, int? grupoId, string? cor,
        decimal? precoMin, decimal? precoMax, string? ordenacao)
    {
        var query = _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.TituloEcommerceProduto) && p.PrecoMinimoProduto > 0)
            .AsQueryable();

        // Filtro por grupo
        if (grupoId.HasValue)
            query = query.Where(p => p.GrupoProduto == grupoId.Value);

        // Filtro por cor
        if (!string.IsNullOrEmpty(cor))
            query = query.Where(p => p.CorPredominanteProduto != null && p.CorPredominanteProduto.ToLower().Contains(cor.ToLower()));

        // Filtro por preço mínimo
        if (precoMin.HasValue)
            query = query.Where(p => p.PrecoMinimoProduto >= precoMin.Value);

        // Filtro por preço máximo
        if (precoMax.HasValue)
            query = query.Where(p => p.PrecoMinimoProduto <= precoMax.Value);

        // Ordenação
        query = ordenacao?.ToLower() switch
        {
            "preco_asc" => query.OrderBy(p => p.PrecoMinimoProduto),
            "preco_desc" => query.OrderByDescending(p => p.PrecoMinimoProduto),
            "nome_asc" => query.OrderBy(p => p.TituloEcommerceProduto),
            "nome_desc" => query.OrderByDescending(p => p.TituloEcommerceProduto),
            "recentes" => query.OrderByDescending(p => p.DataAlteracao),
            _ => query.OrderBy(p => p.TituloEcommerceProduto)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var produtos = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ProdutoPaginatedResponse
        {
            Produtos = produtos.Select(MapToDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        };
    }

    public async Task<ProdutoDto?> GetByReferenciaIncludingInactiveAsync(string referencia)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.ReferenciaProduto == referencia);
        
        return produto != null ? MapToDto(produto) : null;
    }

    public async Task<ReferenciaValidationResult> ValidateReferenciaAsync(string referencia)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.ReferenciaProduto == referencia && 
                                      !string.IsNullOrEmpty(p.TituloEcommerceProduto) && 
                                      p.PrecoMinimoProduto > 0);

        if (produto != null)
        {
            return new ReferenciaValidationResult
            {
                Exists = true,
                IsActive = true,
                Message = $"Referência '{referencia}' já está cadastrada no produto: '{produto.TituloEcommerceProduto}'",
                ProdutoNome = produto.TituloEcommerceProduto
            };
        }

        // Verifica se existe produto inativo (preço 0 ou sem nome)
        var produtoInativo = await _context.Produtos
            .FirstOrDefaultAsync(p => p.ReferenciaProduto == referencia);

        if (produtoInativo != null)
        {
            return new ReferenciaValidationResult
            {
                Exists = true,
                IsActive = false,
                Message = $"Essa referência já está cadastrada em um produto inativo ('{produtoInativo.TituloEcommerceProduto}')",
                ProdutoNome = produtoInativo.TituloEcommerceProduto
            };
        }

        return new ReferenciaValidationResult
        {
            Exists = false,
            IsActive = false,
            Message = "Referência disponível"
        };
    }

    public async Task<IEnumerable<CorDisponivelSimplificadaDto>> GetCoresDisponiveisAsync()
    {
        var cores = await _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.CorPredominanteProduto) && 
                        !string.IsNullOrEmpty(p.TituloEcommerceProduto) && 
                        p.PrecoMinimoProduto > 0)
            .GroupBy(p => p.CorPredominanteProduto!)
            .Select(g => new CorDisponivelSimplificadaDto
            {
                Id = 0,
                Nome = g.Key,
                QuantidadeProdutos = g.Count()
            })
            .OrderBy(c => c.Nome)
            .ToListAsync();

        // Atribui IDs sequenciais
        for (int i = 0; i < cores.Count; i++)
        {
            cores[i].Id = i + 1;
        }

        return cores;
    }

    public async Task<IEnumerable<TamanhoDisponivelDto>> GetTamanhosDisponiveisAsync()
    {
        return await _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.TamanhoProduto) && 
                        !string.IsNullOrEmpty(p.TituloEcommerceProduto) && 
                        p.PrecoMinimoProduto > 0)
            .GroupBy(p => p.TamanhoProduto!)
            .Select(g => new TamanhoDisponivelDto
            {
                Nome = g.Key,
                Quantidade = g.Count()
            })
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProdutoSearchDto>> SearchAsync(string termo, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
            return Enumerable.Empty<ProdutoSearchDto>();

        var termoLower = termo.ToLower().Trim();
        limit = Math.Clamp(limit, 1, 20);

        var produtos = await _context.Produtos
            .Where(p => !string.IsNullOrEmpty(p.TituloEcommerceProduto) && 
                        p.PrecoMinimoProduto > 0 &&
                        (p.TituloEcommerceProduto.ToLower().Contains(termoLower) ||
                         (p.ReferenciaProduto != null && p.ReferenciaProduto.ToLower().Contains(termoLower)) ||
                         (p.Fabricante != null && p.Fabricante.ToLower().Contains(termoLower))))
            .OrderByDescending(p => p.TituloEcommerceProduto!.ToLower().StartsWith(termoLower))
            .ThenBy(p => p.TituloEcommerceProduto)
            .Take(limit)
            .Select(p => new ProdutoSearchDto
            {
                Id = p.IdProduto,
                Nome = p.TituloEcommerceProduto ?? "",
                Slug = p.ReferenciaProduto ?? p.IdProduto.ToString(),
                Preco = p.PrecoMinimoProduto,
                ImagemPrincipal = p.Img
            })
            .ToListAsync();

        return produtos;
    }

    #endregion

    #region CRUD Produto

    public async Task<ProdutoDto> CreateProdutoAsync(ProdutoCreateDto dto)
    {
        var produto = new Produto
        {
            TituloEcommerceProduto = dto.Nome,
            DescricaoConsultaProduto = dto.Descricao,
            Fabricante = dto.Marca,
            GrupoProduto = dto.Grupo,
            ReferenciaProduto = dto.Referencia,
            PrecoTabelaProduto = dto.PrecoTabela,
            PrecoMinimoProduto = dto.PrecoMinimo,
            Img = dto.Img,
            CorPredominanteProduto = dto.Cor,
            TamanhoProduto = dto.Tamanho,
            DadosAdicionaisProduto = dto.DadosAdicionais,
            EanProduto = dto.Ean,
            PesoProduto = dto.Peso,
            AlturaProduto = dto.Altura,
            LarguraProduto = dto.Largura,
            ComprimentoProduto = dto.Comprimento,
            DataAlteracao = DateTime.Now
        };

        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        return MapToDto(produto);
    }

    public async Task<ProdutoDto?> UpdateProdutoAsync(int id, ProdutoUpdateDto dto)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null) return null;

        if (dto.Nome != null) produto.TituloEcommerceProduto = dto.Nome;
        if (dto.Descricao != null) produto.DescricaoConsultaProduto = dto.Descricao;
        if (dto.Marca != null) produto.Fabricante = dto.Marca;
        if (dto.Grupo != null) produto.GrupoProduto = dto.Grupo;
        if (dto.Referencia != null) produto.ReferenciaProduto = dto.Referencia;
        if (dto.PrecoTabela.HasValue) produto.PrecoTabelaProduto = dto.PrecoTabela.Value;
        if (dto.PrecoMinimo.HasValue) produto.PrecoMinimoProduto = dto.PrecoMinimo.Value;
        if (dto.Img != null) produto.Img = dto.Img;
        if (dto.Cor != null) produto.CorPredominanteProduto = dto.Cor;
        if (dto.Tamanho != null) produto.TamanhoProduto = dto.Tamanho;
        if (dto.DadosAdicionais != null) produto.DadosAdicionaisProduto = dto.DadosAdicionais;
        if (dto.Ean != null) produto.EanProduto = dto.Ean;
        if (dto.Peso.HasValue) produto.PesoProduto = dto.Peso;
        if (dto.Altura.HasValue) produto.AlturaProduto = dto.Altura;
        if (dto.Largura.HasValue) produto.LarguraProduto = dto.Largura;
        if (dto.Comprimento.HasValue) produto.ComprimentoProduto = dto.Comprimento;
        
        produto.DataAlteracao = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(produto);
    }

    public async Task<bool> DeleteProdutoAsync(int id)
    {
        var produto = await _context.Produtos.FindAsync(id);
        if (produto == null) return false;

        // Remove grades associadas
        var grades = await _context.ProdutoGrades
            .Where(g => g.IdProdutoPrincipal == id)
            .ToListAsync();
        
        _context.ProdutoGrades.RemoveRange(grades);
        _context.Produtos.Remove(produto);
        
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Grades (Variações)

    public async Task<IEnumerable<ProdutoGradeDto>> GetGradesByProdutoIdAsync(int produtoId)
    {
        var grades = await _context.ProdutoGrades
            .Where(g => g.IdProdutoPrincipal == produtoId)
            .ToListAsync();

        var gradeIds = grades.Select(g => g.IdProdutoGrade).ToList();
        var imagens = await _context.ProdutoGradeImagens
            .Where(i => gradeIds.Contains(i.IdProdutoGrade))
            .OrderBy(i => i.Ordem)
            .ToListAsync();

        return grades.Select(g => MapGradeToDto(g, imagens.Where(i => i.IdProdutoGrade == g.IdProdutoGrade).ToList()));
    }

    public async Task<ProdutoGradeDto> CreateGradeAsync(int produtoId, ProdutoGradeCreateDto dto)
    {
        var grade = new ProdutoGrade
        {
            IdProduto = produtoId,
            IdProdutoPrincipal = dto.IdProdutoPrincipal > 0 ? dto.IdProdutoPrincipal : produtoId,
            ReferenciaProduto = dto.Referencia,
            CorPredominanteProduto = dto.Cor,
            TamanhoProduto = dto.Tamanho,
            QtdProduto = dto.Quantidade,
            Img = dto.Img
        };

        _context.ProdutoGrades.Add(grade);
        await _context.SaveChangesAsync();

        return MapGradeToDto(grade);
    }

    public async Task<ProdutoGradeDto?> UpdateGradeAsync(int gradeId, ProdutoGradeUpdateDto dto)
    {
        var grade = await _context.ProdutoGrades.FindAsync(gradeId);
        if (grade == null) return null;

        if (dto.Referencia != null) grade.ReferenciaProduto = dto.Referencia;
        if (dto.Cor != null) grade.CorPredominanteProduto = dto.Cor;
        if (dto.Tamanho != null) grade.TamanhoProduto = dto.Tamanho;
        if (dto.Quantidade.HasValue) grade.QtdProduto = dto.Quantidade.Value;
        if (dto.Img != null) grade.Img = dto.Img;

        await _context.SaveChangesAsync();

        return MapGradeToDto(grade);
    }

    public async Task<bool> DeleteGradeAsync(int gradeId)
    {
        var grade = await _context.ProdutoGrades.FindAsync(gradeId);
        if (grade == null) return false;

        _context.ProdutoGrades.Remove(grade);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Imagens de Grades

    public async Task<ImagensGradeUploadResponse> UploadImagensGradeAsync(int gradeId, string referencia, List<IFormFile> imagens)
    {
        var grade = await _context.ProdutoGrades.FindAsync(gradeId);
        if (grade == null)
        {
            return new ImagensGradeUploadResponse
            {
                Sucesso = false,
                Mensagem = "Grade não encontrada"
            };
        }

        // Limpar referência para usar como nome de pasta
        var referenciaLimpa = referencia.Trim().Replace(" ", "_").Replace("/", "-");
        
        // Criar diretório se não existir: img/catalogo/{referencia}/
        var diretorio = Path.Combine("wwwroot", "img", "catalogo", referenciaLimpa);
        if (!Directory.Exists(diretorio))
            Directory.CreateDirectory(diretorio);

        // Remover imagens antigas da grade
        var imagensAntigas = await _context.ProdutoGradeImagens
            .Where(i => i.IdProdutoGrade == gradeId)
            .ToListAsync();
        
        _context.ProdutoGradeImagens.RemoveRange(imagensAntigas);

        var imagensSalvas = new List<ProdutoGradeImagemDto>();
        var ordem = 1;

        foreach (var imagem in imagens)
        {
            // Nome do arquivo: {referencia}.jpg para a primeira, {referencia}_2.jpg, {referencia}_3.jpg, etc.
            var nomeArquivo = ordem == 1 
                ? $"{referenciaLimpa}.jpg" 
                : $"{referenciaLimpa}_{ordem}.jpg";
            var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await imagem.CopyToAsync(stream);
            }

            var caminhoRelativo = $"/img/catalogo/{referenciaLimpa}/{nomeArquivo}";

            // Criar registro no banco
            var novaImagem = new ProdutoGradeImagem
            {
                IdProdutoGrade = gradeId,
                Ordem = ordem,
                Caminho = caminhoRelativo,
                CriadoEm = DateTime.UtcNow
            };

            _context.ProdutoGradeImagens.Add(novaImagem);
            await _context.SaveChangesAsync();

            imagensSalvas.Add(new ProdutoGradeImagemDto
            {
                Id = novaImagem.Id,
                IdProdutoGrade = gradeId,
                Ordem = ordem,
                Caminho = caminhoRelativo
            });

            ordem++;
        }

        // Atualizar a imagem principal da grade com a primeira imagem
        if (imagensSalvas.Count > 0)
        {
            grade.Img = imagensSalvas[0].Caminho;
            await _context.SaveChangesAsync();
        }

        return new ImagensGradeUploadResponse
        {
            Sucesso = true,
            Mensagem = $"{imagensSalvas.Count} imagem(ns) enviada(s) com sucesso",
            Imagens = imagensSalvas
        };
    }

    public async Task<IEnumerable<ProdutoGradeImagemDto>> GetImagensGradeAsync(int gradeId)
    {
        var imagens = await _context.ProdutoGradeImagens
            .Where(i => i.IdProdutoGrade == gradeId)
            .OrderBy(i => i.Ordem)
            .ToListAsync();

        return imagens.Select(i => new ProdutoGradeImagemDto
        {
            Id = i.Id,
            IdProdutoGrade = i.IdProdutoGrade,
            Ordem = i.Ordem,
            Caminho = i.Caminho
        });
    }

    public async Task<bool> DeleteImagemGradeAsync(int imagemId)
    {
        var imagem = await _context.ProdutoGradeImagens.FindAsync(imagemId);
        if (imagem == null) return false;

        // Tentar deletar arquivo físico
        var caminhoFisico = Path.Combine("wwwroot", imagem.Caminho.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(caminhoFisico))
        {
            try
            {
                File.Delete(caminhoFisico);
            }
            catch
            {
                // Continuar mesmo se não conseguir deletar o arquivo
            }
        }

        _context.ProdutoGradeImagens.Remove(imagem);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Mappers

    private ProdutoDto MapToDto(Produto produto)
    {
        var precoOriginal = produto.PrecoTabelaProduto;
        var precoAtual = produto.PrecoMinimoProduto;
        int? desconto = null;

        if (precoOriginal > precoAtual && precoAtual > 0)
        {
            desconto = (int)Math.Round((1 - (precoAtual / precoOriginal)) * 100);
        }

        return new ProdutoDto
        {
            Id = produto.IdProduto,
            Nome = produto.TituloEcommerceProduto ?? "",
            Slug = (produto.ReferenciaProduto ?? produto.IdProduto.ToString()).GenerateSlug(),
            Descricao = produto.DescricaoConsultaProduto,
            Marca = produto.Fabricante ?? "",
            Grupo = produto.GrupoProduto,
            Referencia = produto.ReferenciaProduto,
            Preco = precoAtual,
            PrecoOriginal = precoOriginal > precoAtual ? precoOriginal : null,
            Desconto = desconto,
            Img = produto.Img,
            Cor = produto.CorPredominanteProduto,
            Tamanho = produto.TamanhoProduto,
            DadosAdicionais = produto.DadosAdicionaisProduto,
            Ean = produto.EanProduto,
            Peso = produto.PesoProduto,
            Altura = produto.AlturaProduto,
            Largura = produto.LarguraProduto,
            Comprimento = produto.ComprimentoProduto
        };
    }

    private ProdutoGradeDto MapGradeToDto(ProdutoGrade grade, List<ProdutoGradeImagem>? imagens = null)
    {
        return new ProdutoGradeDto
        {
            Id = grade.IdProdutoGrade,
            IdProduto = grade.IdProduto,
            Referencia = grade.ReferenciaProduto,
            Cor = grade.CorPredominanteProduto,
            Tamanho = grade.TamanhoProduto,
            Quantidade = grade.QtdProduto,
            Img = grade.Img,
            Imagens = imagens?.Select(i => new ProdutoGradeImagemDto
            {
                Id = i.Id,
                IdProdutoGrade = i.IdProdutoGrade,
                Ordem = i.Ordem,
                Caminho = i.Caminho
            }).ToList() ?? new List<ProdutoGradeImagemDto>()
        };
    }

    #endregion

    #region Correção de Caminhos

    public async Task<CorrigirCaminhosResult> CorrigirCaminhosImagensAsync()
    {
        var imagens = await _context.ProdutoGradeImagens
            .Where(i => i.Caminho.Contains("/grades/"))
            .ToListAsync();

        var corrigidas = 0;
        var arquivosMovidos = 0;
        var erros = new List<string>();

        foreach (var img in imagens)
        {
            // Mover arquivo físico
            var caminhoAntigo = Path.Combine("wwwroot", img.Caminho.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            var caminhoNovo = caminhoAntigo.Replace($"{Path.DirectorySeparatorChar}grades{Path.DirectorySeparatorChar}", Path.DirectorySeparatorChar.ToString());

            if (File.Exists(caminhoAntigo) && !File.Exists(caminhoNovo))
            {
                try
                {
                    var dirDestino = Path.GetDirectoryName(caminhoNovo);
                    if (!string.IsNullOrEmpty(dirDestino) && !Directory.Exists(dirDestino))
                        Directory.CreateDirectory(dirDestino);

                    File.Move(caminhoAntigo, caminhoNovo);
                    arquivosMovidos++;
                }
                catch (Exception ex)
                {
                    erros.Add($"Erro ao mover {caminhoAntigo}: {ex.Message}");
                }
            }

            img.Caminho = img.Caminho.Replace("/grades/", "/");
            corrigidas++;
        }

        var grades = await _context.ProdutoGrades
            .Where(g => g.Img != null && g.Img.Contains("/grades/"))
            .ToListAsync();

        foreach (var grade in grades)
        {
            if (grade.Img != null)
            {
                grade.Img = grade.Img.Replace("/grades/", "/");
                corrigidas++;
            }
        }

        await _context.SaveChangesAsync();

        return new CorrigirCaminhosResult
        {
            Message = $"Corrigidos {corrigidas} caminhos de imagens, {arquivosMovidos} arquivos movidos",
            ImagensCorrigidas = imagens.Count,
            GradesCorrigidas = grades.Count,
            ArquivosMovidos = arquivosMovidos,
            Erros = erros.Count > 0 ? erros : null
        };
    }

    #endregion
}
