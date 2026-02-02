using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("Rotulos")]
public class Rotulo
{
    [Key]
    public int Id { get; set; }
    
    public int? IdPedido { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string IdRecibo { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? IdAtendimento { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string NomeArquivo { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string CaminhoArquivo { get; set; } = string.Empty;
    
    public DateTime DataGeracao { get; set; } = DateTime.Now;
    
    public int QuantidadeRotulos { get; set; } = 1;
    
    [MaxLength(1000)]
    public string? CodigosObjeto { get; set; }
    
    [MaxLength(1000)]
    public string? IdsPrePostagem { get; set; }
    
    [MaxLength(10)]
    public string TipoRotulo { get; set; } = "P";
    
    [MaxLength(10)]
    public string FormatoRotulo { get; set; } = "ET";
    
    public long TamanhoBytes { get; set; }
    
    [MaxLength(500)]
    public string? Observacao { get; set; }
    
    // Navigation
    [ForeignKey("IdPedido")]
    public Pedido? Pedido { get; set; }
}
