using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("cupons_uso")]
public class CupomUso
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("cupom_id")]
    public int CupomId { get; set; }

    [Column("usuario_id")]
    public int? UsuarioId { get; set; }

    [Column("pedido_id")]
    public int? PedidoId { get; set; }

    [Required]
    [Column("valor_desconto_aplicado")]
    public decimal ValorDescontoAplicado { get; set; }

    [Required]
    [Column("data_uso")]
    public DateTime DataUso { get; set; }

    // Navigation
    [ForeignKey("CupomId")]
    public virtual Cupom? Cupom { get; set; }

    // TODO: Adicionar navegação para Usuario e Pedido quando os modelos forem criados
    // [ForeignKey("UsuarioId")]
    // public virtual Usuario? Usuario { get; set; }

    // [ForeignKey("PedidoId")]
    // public virtual Pedido? Pedido { get; set; }
}
