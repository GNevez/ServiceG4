using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using g4api.DTOs;

namespace g4api.Models;

[Table("pre_postagens")]
public class PrePostagem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("pedido_id")]
    public int PedidoId { get; set; }
    
    [ForeignKey("PedidoId")]
    public Pedido Pedido { get; set; } = null!;
    
    [Column("codigo_rastreamento")]
    [MaxLength(50)]
    public string? CodigoRastreamento { get; set; }
    
    [Column("id_pre_postagem")]
    [MaxLength(100)]
    public string? IdPrePostagem { get; set; }
    
    [Column("numero_etiqueta")]
    [MaxLength(50)]
    public string? NumeroEtiqueta { get; set; }
    
    [Required]
    [Column("codigo_servico")]
    [MaxLength(10)]
    public string CodigoServico { get; set; } = "03220";
    
    [Column("nome_servico")]
    [MaxLength(50)]
    public string NomeServico { get; set; } = "SEDEX";
    
    [Column("peso")]
    public decimal Peso { get; set; } = 0.3m;
    
    [Column("altura")]
    public int Altura { get; set; } = 5;
    
    [Column("largura")]
    public int Largura { get; set; } = 15;
    
    [Column("comprimento")]
    public int Comprimento { get; set; } = 20;
    
    [Column("valor_declarado")]
    public decimal? ValorDeclarado { get; set; }
    
    [Column("status")]
    public StatusPrePostagem Status { get; set; } = StatusPrePostagem.Pendente;
    
    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    
    [Column("data_postagem")]
    public DateTime? DataPostagem { get; set; }
    
    [Column("data_entrega")]
    public DateTime? DataEntrega { get; set; }
    
    [Column("observacoes")]
    [MaxLength(500)]
    public string? Observacoes { get; set; }
    
    [Column("mensagem_erro")]
    [MaxLength(1000)]
    public string? MensagemErro { get; set; }
    
    [Column("resposta_correios_json")]
    public string? RespostaCorreiosJson { get; set; }
}
