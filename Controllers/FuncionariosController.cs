using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g4api.Data;
using g4api.Models;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FuncionariosController : ControllerBase
{
    private readonly G4DbContext _context;

    public FuncionariosController(G4DbContext context)
    {
        _context = context;
    }

    // GET: api/funcionarios
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioDto>>> GetFuncionarios()
    {
        var funcionarios = await _context.Cadastros
            .Where(c => c.Cargo != "Cliente" && c.IsBanned == 0)
            .OrderBy(c => c.Nome)
            .Select(c => new FuncionarioDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Sobrenome = c.Sobrenome,
                NomeCompleto = c.Nome + " " + c.Sobrenome,
                Email = c.Email,
                Cpf = c.Cpf,
                Telefone = c.Telefone,
                DataNascimento = c.DataNascimento,
                Cargo = c.Cargo,
                IsBanned = c.IsBanned == 1
            })
            .ToListAsync();

        return Ok(funcionarios);
    }

    // GET: api/funcionarios/inativos
    [HttpGet("inativos")]
    public async Task<ActionResult<IEnumerable<FuncionarioDto>>> GetFuncionariosInativos()
    {
        var funcionarios = await _context.Cadastros
            .Where(c => c.Cargo != "Cliente" && c.IsBanned == 1)
            .OrderBy(c => c.Nome)
            .Select(c => new FuncionarioDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Sobrenome = c.Sobrenome,
                NomeCompleto = c.Nome + " " + c.Sobrenome,
                Email = c.Email,
                Cpf = c.Cpf,
                Telefone = c.Telefone,
                DataNascimento = c.DataNascimento,
                Cargo = c.Cargo,
                IsBanned = c.IsBanned == 1
            })
            .ToListAsync();

        return Ok(funcionarios);
    }

    // GET: api/funcionarios/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<FuncionarioDto>> GetFuncionario(int id)
    {
        var funcionario = await _context.Cadastros
            .Where(c => c.Id == id && c.Cargo != "Cliente")
            .Select(c => new FuncionarioDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Sobrenome = c.Sobrenome,
                NomeCompleto = c.Nome + " " + c.Sobrenome,
                Email = c.Email,
                Cpf = c.Cpf,
                Telefone = c.Telefone,
                DataNascimento = c.DataNascimento,
                Cargo = c.Cargo,
                IsBanned = c.IsBanned == 1
            })
            .FirstOrDefaultAsync();

        if (funcionario == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        return Ok(funcionario);
    }

    // POST: api/funcionarios
    [HttpPost]
    public async Task<ActionResult<FuncionarioDto>> CreateFuncionario([FromBody] FuncionarioCreateDto dto)
    {
        // Verificar se email já existe
        var emailExists = await _context.Cadastros.AnyAsync(c => c.Email == dto.Email);
        if (emailExists)
            return BadRequest(new { message = "Este e-mail já está cadastrado" });

        // Verificar se CPF já existe
        var cpfExists = await _context.Cadastros.AnyAsync(c => c.Cpf == dto.Cpf);
        if (cpfExists)
            return BadRequest(new { message = "Este CPF já está cadastrado" });

        var funcionario = new Cadastro
        {
            Nome = dto.Nome,
            Sobrenome = dto.Sobrenome,
            Email = dto.Email,
            Cpf = dto.Cpf,
            Telefone = dto.Telefone,
            DataNascimento = dto.DataNascimento,
            Cargo = dto.Cargo ?? "Funcionario",
            Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
            IsBanned = 0,
            CodigoAutenticacao = "0"
        };

        _context.Cadastros.Add(funcionario);
        await _context.SaveChangesAsync();

        var result = new FuncionarioDto
        {
            Id = funcionario.Id,
            Nome = funcionario.Nome,
            Sobrenome = funcionario.Sobrenome,
            NomeCompleto = funcionario.Nome + " " + funcionario.Sobrenome,
            Email = funcionario.Email,
            Cpf = funcionario.Cpf,
            Telefone = funcionario.Telefone,
            DataNascimento = funcionario.DataNascimento,
            Cargo = funcionario.Cargo,
            IsBanned = false
        };

        return CreatedAtAction(nameof(GetFuncionario), new { id = funcionario.Id }, result);
    }

    // PUT: api/funcionarios/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFuncionario(int id, [FromBody] FuncionarioUpdateDto dto)
    {
        var funcionario = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.Id == id && c.Cargo != "Cliente");

        if (funcionario == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        // Verificar se email já existe (de outro usuário)
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != funcionario.Email)
        {
            var emailExists = await _context.Cadastros.AnyAsync(c => c.Email == dto.Email && c.Id != id);
            if (emailExists)
                return BadRequest(new { message = "Este e-mail já está cadastrado" });
        }

        // Verificar se CPF já existe (de outro usuário)
        if (!string.IsNullOrEmpty(dto.Cpf) && dto.Cpf != funcionario.Cpf)
        {
            var cpfExists = await _context.Cadastros.AnyAsync(c => c.Cpf == dto.Cpf && c.Id != id);
            if (cpfExists)
                return BadRequest(new { message = "Este CPF já está cadastrado" });
        }

        // Atualizar campos
        if (!string.IsNullOrEmpty(dto.Nome))
            funcionario.Nome = dto.Nome;
        
        if (!string.IsNullOrEmpty(dto.Sobrenome))
            funcionario.Sobrenome = dto.Sobrenome;
        
        if (!string.IsNullOrEmpty(dto.Email))
            funcionario.Email = dto.Email;
        
        if (!string.IsNullOrEmpty(dto.Cpf))
            funcionario.Cpf = dto.Cpf;
        
        if (dto.Telefone != null)
            funcionario.Telefone = dto.Telefone;
        
        if (dto.DataNascimento.HasValue)
            funcionario.DataNascimento = dto.DataNascimento;
        
        if (!string.IsNullOrEmpty(dto.Cargo))
            funcionario.Cargo = dto.Cargo;

        // Atualizar senha se fornecida
        if (!string.IsNullOrEmpty(dto.Senha))
            funcionario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/funcionarios/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFuncionario(int id)
    {
        var funcionario = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.Id == id && c.Cargo != "Cliente");

        if (funcionario == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        _context.Cadastros.Remove(funcionario);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/funcionarios/{id}/ban
    [HttpPatch("{id}/ban")]
    public async Task<IActionResult> BanFuncionario(int id)
    {
        var funcionario = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.Id == id && c.Cargo != "Cliente");

        if (funcionario == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        funcionario.IsBanned = 1;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/funcionarios/{id}/unban
    [HttpPatch("{id}/unban")]
    public async Task<IActionResult> UnbanFuncionario(int id)
    {
        var funcionario = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.Id == id && c.Cargo != "Cliente");

        if (funcionario == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        funcionario.IsBanned = 0;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/funcionarios/{id}/cargo
    [HttpPatch("{id}/cargo")]
    public async Task<IActionResult> UpdateCargo(int id, [FromBody] UpdateCargoDto dto)
    {
        var funcionario = await _context.Cadastros
            .FirstOrDefaultAsync(c => c.Id == id && c.Cargo != "Cliente");

        if (funcionario == null)
            return NotFound(new { message = "Funcionário não encontrado" });

        if (string.IsNullOrEmpty(dto.Cargo))
            return BadRequest(new { message = "Cargo é obrigatório" });

        funcionario.Cargo = dto.Cargo;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/funcionarios/cargos
    [HttpGet("cargos")]
    public ActionResult<IEnumerable<string>> GetCargosDisponiveis()
    {
        var cargos = new List<string>
        {
            "Administrador",
            "Gerente",
            "Vendedor",
            "Estoquista",
            "Atendente",
            "Financeiro"
        };

        return Ok(cargos);
    }
}

// DTOs
public class FuncionarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string Cargo { get; set; } = string.Empty;
    public bool IsBanned { get; set; }
}

public class FuncionarioCreateDto
{
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Cargo { get; set; }
}

public class FuncionarioUpdateDto
{
    public string? Nome { get; set; }
    public string? Sobrenome { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Cargo { get; set; }
}

public class UpdateCargoDto
{
    public string Cargo { get; set; } = string.Empty;
}
