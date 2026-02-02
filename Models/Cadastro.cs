using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("cadastros")]
public class Cadastro
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    [Column("sobrenome")]
    public string Sobrenome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(120)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    [Column("senha")]
    public string Senha { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(14)]
    [Column("cpf")]
    public string Cpf { get; set; } = string.Empty;
    
    [MaxLength(20)]
    [Column("numero")]
    public string? Telefone { get; set; }
    
    [Column("nascimento")]
    public DateTime? DataNascimento { get; set; }
    
    [Column("isBanned")]
    public int IsBanned { get; set; } = 0;
    
    [MaxLength(20)]
    [Column("cargo")]
    public string Cargo { get; set; } = "Cliente";
    
    [MaxLength(40)]
    [Column("Aut")]
    public string? CodigoAutenticacao { get; set; } = "0";
    
    [NotMapped]
    public bool IsAdmin => Cargo?.ToLower() == "admin" || Cargo?.ToLower() == "administrador";
    
    [NotMapped]
    public string NomeCompleto => $"{Nome} {Sobrenome}".Trim();
}
