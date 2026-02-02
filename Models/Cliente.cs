using System.ComponentModel.DataAnnotations;

namespace g4api.Models;

public class Cliente
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(14)]
    public string? Cpf { get; set; }
    
    [MaxLength(20)]
    public string? Telefone { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    
    public DateTime? DataAtualizacao { get; set; }
}
