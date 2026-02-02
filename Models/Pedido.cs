using System.ComponentModel.DataAnnotations;

namespace g4api.Models;

public class Pedido
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(30)]
    public string CodigoPedido { get; set; } = string.Empty;
    
    public int CarrinhoId { get; set; }
    
    public int EnderecoEntregaId { get; set; }
    
    public StatusPedido Status { get; set; } = StatusPedido.AguardandoConfirmacao;
    
    public decimal? PrecoFrete { get; set; }
    
    public decimal TotalPedido { get; set; }
    
    public decimal DescontoCupom { get; set; } = 0m;
    
    public DateTime DataPedido { get; set; } = DateTime.UtcNow;
    
    public DateTime? DataAtualizacao { get; set; }
    
    [MaxLength(50)]
    public string? CodigoRastreamento { get; set; }
    
    [Required]
    [MaxLength(30)]
    public string MetodoPagamento { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }
    
    [MaxLength(500)]
    public string? MotivoCancelamento { get; set; }
    
    [MaxLength(50)]
    public string? MercadoPagoPaymentId { get; set; }
    
    // Snapshot dos dados do cliente no momento da compra
    [Required]
    [MaxLength(150)]
    public string NomeCliente { get; set; } = null!;
    
    [Required]
    [MaxLength(150)]
    public string EmailCliente { get; set; } = null!;
    
    [MaxLength(20)]
    public string? TelefoneCliente { get; set; }
    
    [MaxLength(20)]
    public string? CpfCliente { get; set; }
    
    // Propriedades de navegação
    public Carrinho Carrinho { get; set; } = null!;
    public EnderecoEntrega EnderecoEntrega { get; set; } = null!;
}
