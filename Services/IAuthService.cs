using g4api.DTOs;
using g4api.Models;

namespace g4api.Services;

public interface IAuthService
{
    Task<Cadastro?> ValidateCredentialsAsync(string email, string senha);
    Task<Cadastro?> GetUserByIdAsync(int id);
    Task<Cadastro?> GetUserByEmailAsync(string email);
    Task<Cadastro?> GetUserByCpfAsync(string cpf);
    Task<Cadastro> RegisterAsync(RegisterRequestDto request);
    string GenerateJwtToken(Cadastro user);
    int? ValidateJwtToken(string token);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    bool VerifyPassword(string password, string hashedPassword);
    string HashPassword(string password);
}
