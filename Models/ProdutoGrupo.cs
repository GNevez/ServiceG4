using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("produto_grupo")]
public class ProdutoGrupo
{
    [Key]
    [Column("idProdutoGrupo")]
    public int IdProdutoGrupo { get; set; }

    [Column("descricaogrupoProduto")]
    public string? DescricaoGrupoProduto { get; set; }

    [Column("img")]
    public string? Img { get; set; }

    // Navigation property
    public virtual ICollection<ProdutoSubGrupo> SubGrupos { get; set; } = new List<ProdutoSubGrupo>();
}
