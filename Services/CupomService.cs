using g4api.Data;
using g4api.Models;
using Microsoft.EntityFrameworkCore;

namespace g4api.Services;

public class CupomService : ICupomService
{
    private readonly G4DbContext _context;

    public CupomService(G4DbContext context)
    {
        _context = context;
    }

    public async Task<Cupom?> GetCupomByCodigoAsync(string codigo)
    {
        return await _context.Cupons
            .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == codigo.ToUpper());
    }

    public async Task<bool> ValidarCupomAsync(string codigo, decimal subtotal, int? clienteId)
    {
        var cupom = await GetCupomByCodigoAsync(codigo);

        if (cupom == null)
            return false;

        // Verifica se o cupom está ativo
        if (!cupom.Ativo)
            return false;

        // Verifica se o cupom já iniciou
        if (cupom.DataInicio > DateTime.Now)
            return false;

        // Verifica se o cupom expirou
        if (cupom.DataExpiracao.HasValue && cupom.DataExpiracao.Value < DateTime.Now)
            return false;

        // Verifica quantidade máxima de usos
        if (cupom.QuantidadeMaximaUsos.HasValue && cupom.QuantidadeUsosAtual >= cupom.QuantidadeMaximaUsos.Value)
            return false;

        // Verifica valor mínimo de compra
        if (cupom.ValorMinimoCompra.HasValue && subtotal < cupom.ValorMinimoCompra.Value)
            return false;

        // Verifica uso por usuário
        if (clienteId.HasValue && cupom.UsoPorUsuario > 0)
        {
            var usosDoUsuario = await _context.CuponsUso
                .CountAsync(u => u.CupomId == cupom.Id && u.UsuarioId == clienteId);

            if (usosDoUsuario >= cupom.UsoPorUsuario)
                return false;
        }

        return true;
    }

    public async Task<decimal> CalcularDescontoAsync(string codigo, decimal subtotal)
    {
        var cupom = await GetCupomByCodigoAsync(codigo);

        if (cupom == null)
            return 0;

        decimal desconto;

        if (cupom.IsPercentual)
        {
            // Desconto percentual
            desconto = subtotal * (cupom.ValorDesconto / 100);
        }
        else
        {
            // Desconto valor fixo
            desconto = cupom.ValorDesconto;
        }

        // Aplica limite máximo de desconto se existir
        if (cupom.ValorMaximoDesconto.HasValue && desconto > cupom.ValorMaximoDesconto.Value)
        {
            desconto = cupom.ValorMaximoDesconto.Value;
        }

        // O desconto não pode ser maior que o subtotal
        if (desconto > subtotal)
        {
            desconto = subtotal;
        }

        return Math.Round(desconto, 2);
    }

    public async Task RegistrarUsoAsync(int cupomId, int? usuarioId, int pedidoId)
    {
        var cupomUso = new CupomUso
        {
            CupomId = cupomId,
            UsuarioId = usuarioId,
            PedidoId = pedidoId,
            DataUso = DateTime.Now
        };

        _context.CuponsUso.Add(cupomUso);
        await _context.SaveChangesAsync();

        // Incrementa o contador de usos do cupom
        await IncrementarUsosAsync(cupomId);
    }

    public async Task IncrementarUsosAsync(int cupomId)
    {
        var cupom = await _context.Cupons.FindAsync(cupomId);
        if (cupom != null)
        {
            cupom.QuantidadeUsosAtual++;
            await _context.SaveChangesAsync();
        }
    }
}
