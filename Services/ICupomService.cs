using g4api.Models;

namespace g4api.Services;

public interface ICupomService
{
    Task<Cupom?> GetCupomByCodigoAsync(string codigo);
    Task<bool> ValidarCupomAsync(string codigo, decimal subtotal, int? clienteId);
    Task<decimal> CalcularDescontoAsync(string codigo, decimal subtotal);
    Task RegistrarUsoAsync(int cupomId, int? usuarioId, int pedidoId);
    Task IncrementarUsosAsync(int cupomId);
}
