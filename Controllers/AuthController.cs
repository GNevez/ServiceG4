using g4api.DTOs;
using g4api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var user = await _authService.ValidateCredentialsAsync(request.Email, request.Senha);

            if (user == null)
            {
                return Unauthorized(new LoginResponseDto
                {
                    Success = false,
                    Message = "Credenciais inválidas"
                });
            }

            var token = _authService.GenerateJwtToken(user);
            var role = user.IsAdmin ? "Administrador" : "Cliente";

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            };

            Response.Cookies.Append("auth_token", token, cookieOptions);

            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso",
                Id = user.Id.ToString(),
                Email = user.Email,
                Nome = user.NomeCompleto,
                Role = role,
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login: {Email}", request.Email);
            return StatusCode(500, new LoginResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor"
            });
        }
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidateTokenResponseDto>> ValidateToken()
    {
        try
        {
            var token = GetTokenFromRequest();

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ValidateTokenResponseDto { Valid = false });
            }

            var userId = _authService.ValidateJwtToken(token);

            if (userId == null)
            {
                return Unauthorized(new ValidateTokenResponseDto { Valid = false });
            }

            var user = await _authService.GetUserByIdAsync(userId.Value);

            if (user == null || user.IsBanned == 1)
            {
                return Unauthorized(new ValidateTokenResponseDto { Valid = false });
            }

            var role = user.IsAdmin ? "Administrador" : "Cliente";

            return Ok(new ValidateTokenResponseDto
            {
                Valid = true,
                Id = user.Id.ToString(),
                Email = user.Email,
                Nome = user.NomeCompleto,
                Role = role
            });
        }
        catch
        {
            return Unauthorized(new ValidateTokenResponseDto { Valid = false });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        return Ok(new { success = true, message = "Logout realizado com sucesso" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        try
        {
            var token = GetTokenFromRequest();

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Token não encontrado" });
            }

            var userId = _authService.ValidateJwtToken(token);

            if (userId == null)
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var user = await _authService.GetUserByIdAsync(userId.Value);

            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado" });
            }

            return Ok(new UserInfoDto
            {
                Id = user.Id,
                Nome = user.Nome,
                Sobrenome = user.Sobrenome,
                Email = user.Email,
                Cpf = user.Cpf,
                Telefone = user.Telefone,
                DataNascimento = user.DataNascimento,
                Cargo = user.Cargo,
                IsAdmin = user.IsAdmin
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        try
        {
            var token = GetTokenFromRequest();

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Token não encontrado" });
            }

            var userId = _authService.ValidateJwtToken(token);

            if (userId == null)
            {
                return Unauthorized(new { message = "Token inválido" });
            }

            var result = await _authService.ChangePasswordAsync(userId.Value, request.SenhaAtual, request.NovaSenha);

            if (!result)
            {
                return BadRequest(new { message = "Senha atual incorreta ou erro ao alterar senha" });
            }

            return Ok(new { success = true, message = "Senha alterada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar senha");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }
    }

    [HttpGet("check-admin")]
    [Authorize(Roles = "Administrador")]
    public IActionResult CheckAdmin()
    {
        return Ok(new { isAdmin = true, message = "Usuário é administrador" });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var existingEmail = await _authService.GetUserByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return BadRequest(new RegisterResponseDto
                {
                    Success = false,
                    Message = "Este email já está cadastrado"
                });
            }

            var existingCpf = await _authService.GetUserByCpfAsync(request.Cpf);
            if (existingCpf != null)
            {
                return BadRequest(new RegisterResponseDto
                {
                    Success = false,
                    Message = "Este CPF já está cadastrado"
                });
            }

            var user = await _authService.RegisterAsync(request);

            return Ok(new RegisterResponseDto
            {
                Success = true,
                Message = "Cadastro realizado com sucesso",
                Id = user.Id,
                Email = user.Email,
                Nome = user.NomeCompleto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário");
            return StatusCode(500, new RegisterResponseDto
            {
                Success = false,
                Message = "Erro interno do servidor"
            });
        }
    }

    private string? GetTokenFromRequest()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return Request.Cookies["auth_token"];
    }
}
