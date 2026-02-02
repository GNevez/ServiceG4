namespace g4api.DTOs;

#region Response DTOs

public class CarrinhoDto
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = "Ativo";
    public List<ItemCarrinhoDto> Itens { get; set; } = new();
    public decimal Subtotal { get; set; }
    public int TotalItens { get; set; }
    
    // Cupom
    public int? CupomId { get; set; }
    public string? CupomCodigo { get; set; }
    public string? CupomDescricao { get; set; }
    public string? CupomTipo { get; set; } // "percentual" ou "valor_fixo"
    public decimal? CupomValor { get; set; } // Valor ou percentual do cupom
    public decimal CupomDesconto { get; set; } // Desconto calculado
    
    public decimal Total { get; set; }
}

public class ItemCarrinhoDto
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public string ProdutoReferencia { get; set; } = string.Empty;
    public string ProdutoImagem { get; set; } = string.Empty;
    public decimal ProdutoPreco { get; set; }
    public decimal ProdutoPrecoOriginal { get; set; }
    
    public int? GradeId { get; set; }
    public string? CorNome { get; set; }
    public string? Tamanho { get; set; }
    public string? GradeImagem { get; set; }
    public decimal EstoqueDisponivel { get; set; }
    public int QuantidadeMinima { get; set; } = 1;
    
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

#endregion

#region Request DTOs

public class AdicionarItemCarrinhoDto
{
    public int ProdutoId { get; set; }
    public int? GradeId { get; set; }
    public int Quantidade { get; set; } = 1;
}

public class AtualizarItemCarrinhoDto
{
    public int ItemId { get; set; }
    public int Quantidade { get; set; }
}

public class AplicarCupomDto
{
    public string Codigo { get; set; } = string.Empty;
}

#endregion
