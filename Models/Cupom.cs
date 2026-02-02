using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

public enum TipoDesconto
{
    Percentual,
    ValorFixo
}

[Table("cupons")]
public class Cupom
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("codigo")]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Column("descricao")]
    [MaxLength(255)]
    public string? Descricao { get; set; }

    [Required]
    [Column("tipo_desconto")]
    [MaxLength(20)]
    public string TipoDesconto { get; set; } = "percentual";

    [Required]
    [Column("valor_desconto")]
    public decimal ValorDesconto { get; set; }

    [Column("valor_minimo_compra")]
    public decimal? ValorMinimoCompra { get; set; }

    [Column("valor_maximo_desconto")]
    public decimal? ValorMaximoDesconto { get; set; }

    [Column("quantidade_maxima_usos")]
    public int? QuantidadeMaximaUsos { get; set; }

    [Column("quantidade_usos_atual")]
    public int QuantidadeUsosAtual { get; set; } = 0;

    [Column("uso_por_usuario")]
    public int UsoPorUsuario { get; set; } = 1;

    [Required]
    [Column("data_inicio")]
    public DateTime DataInicio { get; set; }

    [Column("data_expiracao")]
    public DateTime? DataExpiracao { get; set; }

    [Required]
    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Required]
    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; }

    [Column("data_atualizacao")]
    public DateTime? DataAtualizacao { get; set; }

    // Navigation
    public virtual ICollection<CupomUso> Usos { get; set; } = new List<CupomUso>();

    // Helper para verificar o tipo
    public bool IsPercentual => TipoDesconto.ToLower() == "percentual";
    public bool IsValorFixo => TipoDesconto.ToLower() == "valor_fixo" || TipoDesconto.ToLower() == "valorfixo";
}
