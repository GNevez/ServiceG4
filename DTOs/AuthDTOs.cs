using System.ComponentModel.DataAnnotations;

namespace g4api.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Token { get; set; }
}

public class ValidateTokenResponseDto
{
    public bool Valid { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserInfoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Cargo { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "A senha atual é obrigatória")]
    public string SenhaAtual { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A nova senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A nova senha deve ter no mínimo 6 caracteres")]
    public string NovaSenha { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
    [Compare("NovaSenha", ErrorMessage = "As senhas não conferem")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [MaxLength(20, ErrorMessage = "O nome deve ter no máximo 20 caracteres")]
    public string Nome { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "O sobrenome é obrigatório")]
    [MaxLength(20, ErrorMessage = "O sobrenome deve ter no máximo 20 caracteres")]
    public string Sobrenome { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(120, ErrorMessage = "O email deve ter no máximo 120 caracteres")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    public string Senha { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "A confirmação da senha é obrigatória")]
    [Compare("Senha", ErrorMessage = "As senhas não conferem")]
    public string ConfirmarSenha { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "O CPF é obrigatório")]
    [MaxLength(14, ErrorMessage = "O CPF deve ter no máximo 14 caracteres")]
    public string Cpf { get; set; } = string.Empty;
    
    [MaxLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres")]
    public string? Telefone { get; set; }
    
    public DateTime? DataNascimento { get; set; }
}

public class RegisterResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? Id { get; set; }
    public string? Email { get; set; }
    public string? Nome { get; set; }
}
