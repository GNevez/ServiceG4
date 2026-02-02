using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("produto_sub_grupo")]
public class ProdutoSubGrupo
{
    [Key]
    [Column("idProdutoSubGrupo")]
    public int IdProdutoSubGrupo { get; set; }

    [Column("idProdutoGrupo")]
    public int IdProdutoGrupo { get; set; }

    [Column("descricaosubgrupoProduto")]
    public string? DescricaoSubGrupoProduto { get; set; }

    [Column("img")]
    public string? Img { get; set; }

    // Navigation property
    [ForeignKey("IdProdutoGrupo")]
    public virtual ProdutoGrupo? Grupo { get; set; }
}
