using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using g4api.Data;
using g4api.DTOs;
using g4api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace g4api.Services;

public class AuthService : IAuthService
{
    private readonly G4DbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(G4DbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Cadastro?> ValidateCredentialsAsync(string email, string senha)
    {
        try
        {
            var user = await _context.Cadastros
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return null;
            }

            if (user.IsBanned == 1)
            {
                return null;
            }

            if (!VerifyPassword(senha, user.Senha))
            {
                return null;
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar credenciais para: {Email}", email);
            return null;
        }
    }

    public async Task<Cadastro?> GetUserByIdAsync(int id)
    {
        try
        {
            return await _context.Cadastros.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário por ID: {Id}", id);
            return null;
        }
    }

    public async Task<Cadastro?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _context.Cadastros
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário por email: {Email}", email);
            return null;
        }
    }

    public string GenerateJwtToken(Cadastro user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
        var issuer = jwtSettings["Issuer"] ?? "g4api";
        var audience = jwtSettings["Audience"] ?? "g4-painel";
        var expirationHours = int.Parse(jwtSettings["ExpirationHours"] ?? "24");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var role = user.IsAdmin ? "Administrador" : "Cliente";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.NomeCompleto),
            new Claim(ClaimTypes.Role, role),
            new Claim("userId", user.Id.ToString()),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ValidateJwtToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
            var issuer = jwtSettings["Issuer"] ?? "g4api";
            var audience = jwtSettings["Audience"] ?? "g4-painel";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            var userIdClaim = principal.FindFirst("userId") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (SecurityTokenExpiredException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Cadastros.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!VerifyPassword(currentPassword, user.Senha))
            {
                return false;
            }

            user.Senha = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar senha do usuário: {UserId}", userId);
            return false;
        }
    }

    public async Task<Cadastro?> GetUserByCpfAsync(string cpf)
    {
        try
        {
            var cpfLimpo = cpf.Replace(".", "").Replace("-", "");
            return await _context.Cadastros
                .FirstOrDefaultAsync(u => u.Cpf.Replace(".", "").Replace("-", "") == cpfLimpo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário por CPF");
            return null;
        }
    }

    public async Task<Cadastro> RegisterAsync(RegisterRequestDto request)
    {
        var cadastro = new Cadastro
        {
            Nome = request.Nome,
            Sobrenome = request.Sobrenome,
            Email = request.Email,
            Senha = HashPassword(request.Senha),
            Cpf = request.Cpf,
            Telefone = request.Telefone,
            DataNascimento = request.DataNascimento,
            Cargo = "Cliente",
            IsBanned = 0,
            CodigoAutenticacao = "0"
        };

        _context.Cadastros.Add(cadastro);
        await _context.SaveChangesAsync();

        return cadastro;
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(10));
    }
}
