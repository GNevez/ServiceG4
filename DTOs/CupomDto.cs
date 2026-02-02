namespace g4api.DTOs;

public class CupomDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string? Descricao { get; set; }
    public string TipoDesconto { get; set; } = "percentual";
    public decimal ValorDesconto { get; set; }
    public decimal? ValorMinimoCompra { get; set; }
    public decimal? ValorMaximoDesconto { get; set; }
    public int? QuantidadeMaximaUsos { get; set; }
    public int QuantidadeUsosAtual { get; set; }
    public int UsosPorUsuario { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public bool Ativo { get; set; }
}

public class CupomCreateDto
{
    public string Codigo { get; set; } = "";
    public string? Descricao { get; set; }
    public string TipoDesconto { get; set; } = "percentual";
    public decimal ValorDesconto { get; set; }
    public decimal? ValorMinimoCompra { get; set; }
    public decimal? ValorMaximoDesconto { get; set; }
    public int? QuantidadeMaximaUsos { get; set; }
    public int? UsosPorUsuario { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public bool? Ativo { get; set; }
}

public class CupomUpdateDto
{
    public string? Codigo { get; set; }
    public string? Descricao { get; set; }
    public string? TipoDesconto { get; set; }
    public decimal? ValorDesconto { get; set; }
    public decimal? ValorMinimoCompra { get; set; }
    public decimal? ValorMaximoDesconto { get; set; }
    public int? QuantidadeMaximaUsos { get; set; }
    public int? UsosPorUsuario { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public bool? Ativo { get; set; }
}
