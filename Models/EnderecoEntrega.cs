using System.ComponentModel.DataAnnotations;

namespace g4api.Models;

public class EnderecoEntrega
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Cep { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Logradouro { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Numero { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Complemento { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Bairro { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Cidade { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2)]
    public string Uf { get; set; } = string.Empty;
    
    // Dados do destinatário
    [Required]
    [MaxLength(150)]
    public string NomeDestinatario { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? TelefoneDestinatario { get; set; }
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
