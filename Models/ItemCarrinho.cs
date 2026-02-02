using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("carrinho_item")]
public class ItemCarrinho
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("carrinho_id")]
    public int CarrinhoId { get; set; }

    [Column("produto_id")]
    public int ProdutoId { get; set; }

    [Column("grade_id")]
    public int? GradeId { get; set; }

    [Column("quantidade")]
    public int Quantidade { get; set; } = 1;

    [Column("preco_unitario")]
    public decimal PrecoUnitario { get; set; }

    [Column("data_adicao")]
    public DateTime DataAdicao { get; set; } = DateTime.Now;

    [Column("data_atualizacao")]
    public DateTime? DataAtualizacao { get; set; }

    // Navigation properties
    [ForeignKey("CarrinhoId")]
    public virtual Carrinho? Carrinho { get; set; }

    [ForeignKey("ProdutoId")]
    public virtual Produto? Produto { get; set; }

    [ForeignKey("GradeId")]
    public virtual ProdutoGrade? Grade { get; set; }
}
