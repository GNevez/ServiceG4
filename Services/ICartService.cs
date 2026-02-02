using g4api.DTOs;

namespace g4api.Services;

public interface ICartService
{
    Task<CarrinhoDto> GetOrCreateCartAsync(string? token);
    Task<CarrinhoDto> AddItemAsync(string? token, AdicionarItemCarrinhoDto item);
    Task<CarrinhoDto> UpdateItemQuantityAsync(string token, AtualizarItemCarrinhoDto item);
    Task<CarrinhoDto> RemoveItemAsync(string token, int itemId);
    Task<CarrinhoDto> ClearCartAsync(string token);
    Task<bool> ValidateCartTokenAsync(string? token);
    Task<CarrinhoDto> ApplyCouponAsync(string? token, string codigo);
    Task<CarrinhoDto> RemoveCouponAsync(string? token);
}
