using g4api.Data;
using g4api.DTOs;
using g4api.Models;
using Microsoft.EntityFrameworkCore;

namespace g4api.Services;

public class CartService : ICartService
{
    private readonly G4DbContext _context;

    public CartService(G4DbContext context)
    {
        _context = context;
    }

    public async Task<CarrinhoDto> GetOrCreateCartAsync(string? token)
    {
        Carrinho? carrinho = null;

        if (!string.IsNullOrEmpty(token))
        {
            carrinho = await _context.Carrinhos
                .Include(c => c.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(c => c.Itens)
                    .ThenInclude(i => i.Grade)
                .Include(c => c.Cupom)
                .FirstOrDefaultAsync(c => c.Token == token && c.Ativo && c.Status == StatusCarrinho.Ativo);
        }

        if (carrinho == null)
        {
            carrinho = new Carrinho
            {
                Token = Guid.NewGuid().ToString(),
                Status = StatusCarrinho.Ativo,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now,
                Ativo = true
            };

            _context.Carrinhos.Add(carrinho);
            await _context.SaveChangesAsync();
        }

        return MapToDto(carrinho);
    }

    public async Task<CarrinhoDto> AddItemAsync(string? token, AdicionarItemCarrinhoDto item)
    {
        var carrinho = await GetOrCreateCartEntityAsync(token);

        var produto = await _context.Produtos.FindAsync(item.ProdutoId);
        if (produto == null)
            throw new ArgumentException($"Produto com ID {item.ProdutoId} não encontrado");

        ProdutoGrade? grade = null;
        int? gradeIdParaUsar = item.GradeId;

        if (item.GradeId.HasValue)
        {
            grade = await _context.ProdutoGrades.FindAsync(item.GradeId.Value);
            if (grade == null)
                throw new ArgumentException($"Grade com ID {item.GradeId} não encontrada");
        }
        else
        {
            // Busca a primeira grade com estoque disponível
            grade = await _context.ProdutoGrades
                .Where(g => g.IdProdutoPrincipal == item.ProdutoId && g.QtdProduto > 0)
                .OrderBy(g => g.IdProdutoGrade)
                .FirstOrDefaultAsync();
            
            if (grade != null)
            {
                gradeIdParaUsar = grade.IdProdutoGrade;
            }
        }

        if (grade == null || grade.QtdProduto <= 0)
            throw new ArgumentException("Este produto não está disponível para venda. A grade selecionada não possui estoque.");

        var itemExistente = carrinho.Itens.FirstOrDefault(i => 
            i.ProdutoId == item.ProdutoId && i.GradeId == gradeIdParaUsar);

        if (itemExistente != null)
        {
            itemExistente.Quantidade += item.Quantidade;
            itemExistente.DataAtualizacao = DateTime.Now;
        }
        else
        {
            var novoItem = new ItemCarrinho
            {
                CarrinhoId = carrinho.Id,
                ProdutoId = item.ProdutoId,
                GradeId = gradeIdParaUsar,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.PrecoMinimoProduto,
                DataAdicao = DateTime.Now
            };

            carrinho.Itens.Add(novoItem);
        }

        carrinho.DataAtualizacao = DateTime.Now;
        await _context.SaveChangesAsync();

        return await GetCartByTokenAsync(carrinho.Token);
    }

    public async Task<CarrinhoDto> UpdateItemQuantityAsync(string token, AtualizarItemCarrinhoDto item)
    {
        var carrinho = await GetCartEntityByTokenAsync(token);
        if (carrinho == null)
            throw new ArgumentException("Carrinho não encontrado");

        var itemCarrinho = carrinho.Itens.FirstOrDefault(i => i.Id == item.ItemId);
        if (itemCarrinho == null)
            throw new ArgumentException("Item não encontrado no carrinho");

        if (item.Quantidade <= 0)
        {
            // Remove o item
            _context.ItensCarrinho.Remove(itemCarrinho);
        }
        else
        {
            // Valida quantidade mínima da grade (usa QtdProduto como qtd mínima)
            if (itemCarrinho.GradeId.HasValue)
            {
                var grade = await _context.ProdutoGrades.FindAsync(itemCarrinho.GradeId.Value);
                if (grade != null && grade.QtdProduto > 0)
                {
                    var qtdMinima = (int)grade.QtdProduto;
                    if (item.Quantidade < qtdMinima)
                        throw new ArgumentException($"A quantidade mínima para este produto é {qtdMinima} unidade(s).");
                }
            }

            itemCarrinho.Quantidade = item.Quantidade;
            itemCarrinho.DataAtualizacao = DateTime.Now;
        }

        carrinho.DataAtualizacao = DateTime.Now;
        await _context.SaveChangesAsync();

        return await GetCartByTokenAsync(token);
    }

    public async Task<CarrinhoDto> RemoveItemAsync(string token, int itemId)
    {
        var carrinho = await GetCartEntityByTokenAsync(token);
        if (carrinho == null)
            throw new ArgumentException("Carrinho não encontrado");

        var item = carrinho.Itens.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _context.ItensCarrinho.Remove(item);
            carrinho.DataAtualizacao = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return await GetCartByTokenAsync(token);
    }

    public async Task<CarrinhoDto> ClearCartAsync(string token)
    {
        var carrinho = await GetCartEntityByTokenAsync(token);
        if (carrinho == null)
            throw new ArgumentException("Carrinho não encontrado");

        _context.ItensCarrinho.RemoveRange(carrinho.Itens);
        carrinho.DataAtualizacao = DateTime.Now;
        carrinho.CupomCodigo = null;
        carrinho.CupomDesconto = 0;
        
        await _context.SaveChangesAsync();

        return await GetCartByTokenAsync(token);
    }

    public async Task<bool> ValidateCartTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        return await _context.Carrinhos
            .AnyAsync(c => c.Token == token && c.Ativo && c.Status == StatusCarrinho.Ativo);
    }

    public async Task<CarrinhoDto> ApplyCouponAsync(string? token, string codigo)
    {
        var carrinho = await GetOrCreateCartEntityAsync(token);
        
        var cupom = await _context.Cupons
            .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == codigo.ToUpper());

        if (cupom == null)
            throw new ArgumentException("Cupom não encontrado");

        ValidarCupom(cupom, carrinho);

        var subtotal = carrinho.Itens.Sum(i => i.PrecoUnitario * i.Quantidade);

        if (cupom.ValorMinimoCompra.HasValue && subtotal < cupom.ValorMinimoCompra.Value)
            throw new ArgumentException($"Valor mínimo de compra para este cupom é R$ {cupom.ValorMinimoCompra.Value:N2}");

        decimal desconto = CalcularDesconto(cupom, subtotal);

        carrinho.CupomId = cupom.Id;
        carrinho.CupomCodigo = cupom.Codigo.ToUpper();
        carrinho.CupomDesconto = desconto;
        carrinho.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();

        return await GetCartByTokenAsync(carrinho.Token);
    }

    private void ValidarCupom(Cupom cupom, Carrinho carrinho)
    {
        if (!cupom.Ativo)
            throw new ArgumentException("Este cupom não está mais ativo");

        if (cupom.DataInicio > DateTime.Now)
            throw new ArgumentException("Este cupom ainda não está válido");

        if (cupom.DataExpiracao.HasValue && cupom.DataExpiracao.Value < DateTime.Now)
            throw new ArgumentException("Este cupom expirou");

        if (cupom.QuantidadeMaximaUsos.HasValue && cupom.QuantidadeUsosAtual >= cupom.QuantidadeMaximaUsos.Value)
            throw new ArgumentException("Este cupom atingiu o limite máximo de usos");

        if (carrinho.ClienteId.HasValue && cupom.UsoPorUsuario > 0)
        {
            var usosDoUsuario = _context.CuponsUso
                .Count(u => u.CupomId == cupom.Id && u.UsuarioId == carrinho.ClienteId);

            if (usosDoUsuario >= cupom.UsoPorUsuario)
                throw new ArgumentException("Você já utilizou este cupom o número máximo de vezes permitido");
        }
    }

    private decimal CalcularDesconto(Cupom cupom, decimal subtotal)
    {
        decimal desconto;

        if (cupom.IsPercentual)
        {
            desconto = subtotal * (cupom.ValorDesconto / 100);
        }
        else
        {
            desconto = cupom.ValorDesconto;
        }

        if (cupom.ValorMaximoDesconto.HasValue && desconto > cupom.ValorMaximoDesconto.Value)
        {
            desconto = cupom.ValorMaximoDesconto.Value;
        }

        if (desconto > subtotal)
        {
            desconto = subtotal;
        }

        return Math.Round(desconto, 2);
    }

    public async Task<CarrinhoDto> RemoveCouponAsync(string? token)
    {
        var carrinho = await GetOrCreateCartEntityAsync(token);

        carrinho.CupomId = null;
        carrinho.CupomCodigo = null;
        carrinho.CupomDesconto = 0;
        carrinho.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();

        return await GetCartByTokenAsync(carrinho.Token);
    }

    #region Private Methods

    private async Task<Carrinho> GetOrCreateCartEntityAsync(string? token)
    {
        Carrinho? carrinho = null;

        if (!string.IsNullOrEmpty(token))
        {
            carrinho = await _context.Carrinhos
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Token == token && c.Ativo && c.Status == StatusCarrinho.Ativo);
        }

        if (carrinho == null)
        {
            carrinho = new Carrinho
            {
                Token = Guid.NewGuid().ToString(),
                Status = StatusCarrinho.Ativo,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now,
                Ativo = true
            };

            _context.Carrinhos.Add(carrinho);
            await _context.SaveChangesAsync();
        }

        return carrinho;
    }

    private async Task<Carrinho?> GetCartEntityByTokenAsync(string token)
    {
        return await _context.Carrinhos
            .Include(c => c.Itens)
                .ThenInclude(i => i.Produto)
            .Include(c => c.Itens)
                .ThenInclude(i => i.Grade)
            .Include(c => c.Cupom)
            .FirstOrDefaultAsync(c => c.Token == token && c.Ativo && c.Status == StatusCarrinho.Ativo);
    }

    private async Task<CarrinhoDto> GetCartByTokenAsync(string token)
    {
        var carrinho = await _context.Carrinhos
            .Include(c => c.Itens)
                .ThenInclude(i => i.Produto)
            .Include(c => c.Itens)
                .ThenInclude(i => i.Grade)
            .Include(c => c.Cupom)
            .FirstOrDefaultAsync(c => c.Token == token && c.Ativo);

        return carrinho != null ? MapToDto(carrinho) : new CarrinhoDto { Token = token };
    }

    private CarrinhoDto MapToDto(Carrinho carrinho)
    {
        var itens = carrinho.Itens.Select(i => new ItemCarrinhoDto
        {
            Id = i.Id,
            ProdutoId = i.ProdutoId,
            ProdutoNome = i.Produto?.TituloEcommerceProduto ?? "",
            ProdutoReferencia = i.Produto?.ReferenciaProduto ?? "",
            ProdutoImagem = i.Grade?.Img ?? i.Produto?.Img ?? "",
            ProdutoPreco = i.PrecoUnitario,
            ProdutoPrecoOriginal = i.Produto?.PrecoTabelaProduto ?? i.PrecoUnitario,
            GradeId = i.GradeId,
            CorNome = i.Grade?.CorPredominanteProduto,
            Tamanho = i.Grade?.TamanhoProduto,
            GradeImagem = i.Grade?.Img,
            EstoqueDisponivel = i.Grade?.QtdProduto ?? 0,
            QuantidadeMinima = i.Grade != null ? (int)i.Grade.QtdProduto : 1,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        var subtotal = itens.Sum(i => i.Subtotal);
        var desconto = carrinho.CupomDesconto;
        var total = subtotal - desconto;

        return new CarrinhoDto
        {
            Id = carrinho.Id,
            Token = carrinho.Token,
            Status = carrinho.Status.ToString(),
            Itens = itens,
            Subtotal = subtotal,
            TotalItens = itens.Sum(i => i.Quantidade),
            CupomId = carrinho.CupomId,
            CupomCodigo = carrinho.CupomCodigo,
            CupomDescricao = carrinho.Cupom?.Descricao,
            CupomTipo = carrinho.Cupom?.TipoDesconto,
            CupomValor = carrinho.Cupom?.ValorDesconto,
            CupomDesconto = desconto,
            Total = total > 0 ? total : 0
        };
    }

    #endregion
}
