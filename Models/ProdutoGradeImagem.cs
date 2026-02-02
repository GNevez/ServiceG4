using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("produto_grade_imagem")]
public class ProdutoGradeImagem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_produto_grade")]
    public int IdProdutoGrade { get; set; }

    [Column("ordem")]
    public int Ordem { get; set; }

    [Column("caminho")]
    [MaxLength(255)]
    public string Caminho { get; set; } = "";

    [Column("criado_em")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    [ForeignKey("IdProdutoGrade")]
    public virtual ProdutoGrade? Grade { get; set; }
}
