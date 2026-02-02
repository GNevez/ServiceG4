using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("carrinho")]
public class Carrinho
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("token")]
    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    [Column("status")]
    public StatusCarrinho Status { get; set; } = StatusCarrinho.Ativo;

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; } = DateTime.Now;

    [Column("data_atualizacao")]
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("cliente_id")]
    public int? ClienteId { get; set; }

    [Column("cupom_codigo")]
    [MaxLength(50)]
    public string? CupomCodigo { get; set; }

    [Column("cupom_desconto")]
    public decimal CupomDesconto { get; set; } = 0;

    [Column("cupom_id")]
    public int? CupomId { get; set; }

    // Navigation properties
    public virtual ICollection<ItemCarrinho> Itens { get; set; } = new List<ItemCarrinho>();
    
    [ForeignKey("CupomId")]
    public virtual Cupom? Cupom { get; set; }
}
