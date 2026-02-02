using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("produto_grade")]
public class ProdutoGrade
{
    [Key]
    [Column("idProdutoGrade")]
    public int IdProdutoGrade { get; set; }

    [Column("idProduto")]
    public int IdProduto { get; set; }

    [Column("idProdutoPrinc")]
    public int IdProdutoPrincipal { get; set; }

    [Column("referencia_produto")]
    public string? ReferenciaProduto { get; set; }

    [Column("corpredominante_produto")]
    public string? CorPredominanteProduto { get; set; }

    [Column("tamanho_produto")]
    public string? TamanhoProduto { get; set; }

    [Column("qtd_produto")]
    public decimal QtdProduto { get; set; }

    [Column("img")]
    public string? Img { get; set; }

    // Navegação para imagens
    public virtual ICollection<ProdutoGradeImagem> Imagens { get; set; } = new List<ProdutoGradeImagem>();
}
