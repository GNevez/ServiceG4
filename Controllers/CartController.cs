using g4api.DTOs;
using g4api.Services;
using Microsoft.AspNetCore.Mvc;

namespace g4api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Obtém o carrinho atual ou cria um novo
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CarrinhoDto>> GetCart()
    {
        var cartToken = Request.Cookies["cart_token"];
        var cart = await _cartService.GetOrCreateCartAsync(cartToken);

        if (string.IsNullOrEmpty(cartToken) || cart.Token != cartToken)
        {
            SetCartCookie(cart.Token);
        }

        return Ok(cart);
    }

    /// <summary>
    /// Adiciona um item ao carrinho
    /// </summary>
    [HttpPost("add")]
    public async Task<ActionResult<CarrinhoDto>> AddItem([FromBody] AdicionarItemCarrinhoDto item)
    {
        try
        {
            var cartToken = Request.Cookies["cart_token"];
            var cart = await _cartService.AddItemAsync(cartToken, item);

            if (string.IsNullOrEmpty(cartToken) || cart.Token != cartToken)
            {
                SetCartCookie(cart.Token);
            }

            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao adicionar item ao carrinho: {ex.Message}");
            return StatusCode(500, new { message = "Erro interno ao adicionar item" });
        }
    }

    /// <summary>
    /// Atualiza a quantidade de um item
    /// </summary>
    [HttpPut("update")]
    public async Task<ActionResult<CarrinhoDto>> UpdateItem([FromBody] AtualizarItemCarrinhoDto item)
    {
        var cartToken = Request.Cookies["cart_token"];
        
        if (string.IsNullOrEmpty(cartToken))
        {
            return BadRequest(new { message = "Token do carrinho não encontrado" });
        }

        try
        {
            var cart = await _cartService.UpdateItemQuantityAsync(cartToken, item);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove um item do carrinho
    /// </summary>
    [HttpDelete("remove/{itemId}")]
    public async Task<ActionResult<CarrinhoDto>> RemoveItem(int itemId)
    {
        var cartToken = Request.Cookies["cart_token"];
        
        if (string.IsNullOrEmpty(cartToken))
        {
            return BadRequest(new { message = "Token do carrinho não encontrado" });
        }

        try
        {
            var cart = await _cartService.RemoveItemAsync(cartToken, itemId);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Limpa todos os itens do carrinho
    /// </summary>
    [HttpDelete("clear")]
    public async Task<ActionResult<CarrinhoDto>> ClearCart()
    {
        var cartToken = Request.Cookies["cart_token"];
        
        if (string.IsNullOrEmpty(cartToken))
        {
            return BadRequest(new { message = "Token do carrinho não encontrado" });
        }

        try
        {
            var cart = await _cartService.ClearCartAsync(cartToken);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Valida se o token do carrinho é válido
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<bool>> ValidateCart()
    {
        var cartToken = Request.Cookies["cart_token"];
        var isValid = await _cartService.ValidateCartTokenAsync(cartToken);
        return Ok(isValid);
    }

    /// <summary>
    /// Aplica um cupom ao carrinho
    /// </summary>
    [HttpPost("apply-coupon")]
    public async Task<ActionResult<CarrinhoDto>> ApplyCoupon([FromBody] AplicarCupomDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            return BadRequest(new { message = "Código do cupom é obrigatório" });
        }

        var cartToken = Request.Cookies["cart_token"];
        
        try
        {
            var cart = await _cartService.ApplyCouponAsync(cartToken, request.Codigo.Trim());
            
            if (string.IsNullOrEmpty(cartToken) || cart.Token != cartToken)
            {
                SetCartCookie(cart.Token);
            }
            
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove o cupom do carrinho
    /// </summary>
    [HttpDelete("remove-coupon")]
    public async Task<ActionResult<CarrinhoDto>> RemoveCoupon()
    {
        var cartToken = Request.Cookies["cart_token"];
        var cart = await _cartService.RemoveCouponAsync(cartToken);
        
        if (string.IsNullOrEmpty(cartToken) || cart.Token != cartToken)
        {
            SetCartCookie(cart.Token);
        }
        
        return Ok(cart);
    }

    private void SetCartCookie(string cartToken)
    {
        var isHttps = Request.IsHttps;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTime.UtcNow.AddHours(1) // Carrinho válido por 1 hora
        };

        Response.Cookies.Append("cart_token", cartToken, cookieOptions);
    }
}
