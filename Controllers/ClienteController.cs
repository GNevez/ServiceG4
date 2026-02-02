using g4api.Data;
using g4api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    private readonly G4DbContext _context;
    private readonly ILogger<ClienteController> _logger;

    public ClienteController(G4DbContext context, ILogger<ClienteController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> GetAll([FromQuery] bool? ativo)
    {
        try
        {
            var query = _context.Clientes.AsQueryable();
            if (ativo.HasValue)
            {
                query = query.Where(c => c.Ativo == ativo.Value);
            }
            else
            {
                query = query.Where(c => c.Ativo);
            }

            var clientes = await query
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Cpf = c.Cpf,
                    Telefone = c.Telefone,
                    DataCriacao = c.DataCriacao,
                    DataAtualizacao = c.DataAtualizacao,
                    TotalPedidos = _context.Pedidos.Count(p => p.EmailCliente == c.Email)
                })
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync();

            return Ok(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao listar clientes");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        try
        {
            var cliente = await _context.Clientes
                .Where(c => c.Id == id && c.Ativo)
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Cpf = c.Cpf,
                    Telefone = c.Telefone,
                    DataCriacao = c.DataCriacao,
                    DataAtualizacao = c.DataAtualizacao,
                    TotalPedidos = _context.Pedidos.Count(p => p.EmailCliente == c.Email)
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new { message = "Cliente não encontrado" });
            }

            return Ok(cliente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao buscar cliente {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("verificar-cpf/{cpf}")]
    public async Task<ActionResult<ClienteExistenteDto>> VerificarCpf(string cpf)
    {
        var cpfLimpo = cpf.Replace(".", "").Replace("-", "").Trim();
        
        if (string.IsNullOrEmpty(cpfLimpo))
        {
            return Ok(new { encontrado = false });
        }

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Cpf == cpfLimpo && c.Ativo);

        if (cliente == null)
        {
            return Ok(new { encontrado = false });
        }

        return Ok(new
        {
            encontrado = true,
            id = cliente.Id,
            nome = cliente.Nome,
            email = cliente.Email,
            cpf = cliente.Cpf,
            telefone = cliente.Telefone
        });
    }

    [HttpGet("verificar-email/{email}")]
    public async Task<ActionResult<ClienteExistenteDto>> VerificarEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Ok(new { encontrado = false });
        }

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Email == email && c.Ativo);

        if (cliente == null)
        {
            return Ok(new { encontrado = false });
        }

        return Ok(new
        {
            encontrado = true,
            id = cliente.Id,
            nome = cliente.Nome,
            email = cliente.Email,
            cpf = cliente.Cpf,
            telefone = cliente.Telefone
        });
    }

    [HttpPost]
    public async Task<ActionResult<ClienteExistenteDto>> Create([FromBody] CriarClienteDto dto)
    {
        try
        {
            var cpfLimpo = dto.Cpf?.Replace(".", "").Replace("-", "").Trim();
            
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => (c.Cpf == cpfLimpo || c.Email == dto.Email) && c.Ativo);
            
            if (clienteExistente != null)
            {
                return Conflict(new { message = "Cliente já cadastrado com este CPF ou email" });
            }

            var cliente = new Cliente
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Cpf = cpfLimpo,
                Telefone = dto.Telefone,
                DataCriacao = DateTime.UtcNow
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, new ClienteExistenteDto
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email,
                Cpf = cliente.Cpf,
                Telefone = cliente.Telefone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao criar cliente");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClienteExistenteDto>> Update(int id, [FromBody] AtualizarClienteDto dto)
    {
        try
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound(new { message = "Cliente não encontrado" });
            }

            var cpfLimpo = dto.Cpf?.Replace(".", "").Replace("-", "").Trim();

            cliente.Nome = dto.Nome;
            cliente.Email = dto.Email;
            cliente.Cpf = cpfLimpo;
            cliente.Telefone = dto.Telefone;
            cliente.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ClienteExistenteDto
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email,
                Cpf = cliente.Cpf,
                Telefone = cliente.Telefone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao atualizar cliente {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("{id}/ativo")]
    public async Task<ActionResult> SetAtivo(int id, [FromBody] SetAtivoDto dto)
    {
        try
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound(new { message = "Cliente não encontrado" });

            cliente.Ativo = dto.Ativo;
            cliente.DataAtualizacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { ativo = cliente.Ativo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao alterar status do cliente {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound(new { message = "Cliente não encontrado" });
            }

            // Verificar se tem pedidos vinculados
            var temPedidos = await _context.Pedidos.AnyAsync(p => p.EmailCliente == cliente.Email);
            if (temPedidos)
            {
                return BadRequest(new { message = "Não é possível excluir cliente com pedidos. Desative-o ao invés de excluir." });
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao excluir cliente {Id}", id);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("inativos")]
    public async Task<ActionResult<List<ClienteDto>>> GetInativos()
    {
        try
        {
            var clientes = await _context.Clientes
                .Where(c => !c.Ativo)
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Cpf = c.Cpf,
                    Telefone = c.Telefone,
                    DataCriacao = c.DataCriacao,
                    DataAtualizacao = c.DataAtualizacao,
                    TotalPedidos = _context.Pedidos.Count(p => p.EmailCliente == c.Email)
                })
                .OrderByDescending(c => c.DataAtualizacao)
                .ToListAsync();

            return Ok(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao listar clientes inativos");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("estatisticas")]
    public async Task<ActionResult> GetEstatisticas()
    {
        try
        {
            var totalAtivos = await _context.Clientes.CountAsync(c => c.Ativo);
            var totalInativos = await _context.Clientes.CountAsync(c => !c.Ativo);
            var novosUltimos30Dias = await _context.Clientes
                .CountAsync(c => c.DataCriacao >= DateTime.UtcNow.AddDays(-30));

            return Ok(new
            {
                totalAtivos,
                totalInativos,
                total = totalAtivos + totalInativos,
                novosUltimos30Dias
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao obter estatísticas");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<ClienteDto>>> Buscar([FromQuery] string termo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo))
            {
                return Ok(new List<ClienteDto>());
            }

            var termoLower = termo.ToLower();
            var clientes = await _context.Clientes
                .Where(c => c.Ativo && (
                    c.Nome.ToLower().Contains(termoLower) ||
                    c.Email.ToLower().Contains(termoLower) ||
                    (c.Cpf != null && c.Cpf.Contains(termo)) ||
                    (c.Telefone != null && c.Telefone.Contains(termo))
                ))
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Cpf = c.Cpf,
                    Telefone = c.Telefone,
                    DataCriacao = c.DataCriacao,
                    DataAtualizacao = c.DataAtualizacao,
                    TotalPedidos = _context.Pedidos.Count(p => p.EmailCliente == c.Email)
                })
                .Take(20)
                .ToListAsync();

            return Ok(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cliente] Erro ao buscar clientes");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

public class ClienteExistenteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
}

public class ClienteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public int TotalPedidos { get; set; }
}

public class CriarClienteDto
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
}

public class AtualizarClienteDto
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
}

public class SetAtivoDto
{
    public bool Ativo { get; set; }
}
