namespace g4api.Models;

public enum DevolucaoStatus
{
    Solicitado = 0,
    SolicitacaoEnviada = 1,  // Código de postagem reversa gerado
    Enviado = 2,              // Cliente postou o produto
    EmAnalise = 3,            // Produto recebido, em análise
    ReembolsoEmitido = 4,     // Reembolso aprovado e emitido
    Rejeitado = 5,            // Devolução rejeitada
    Reembolsado = 6           // Reembolso concluído
}

public class Devolucao
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string Cpf { get; set; } = null!;
    public string NomeCliente { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefone { get; set; }
    public string? Motivo { get; set; }
    public string? Observacoes { get; set; }
    public DevolucaoStatus Status { get; set; } = DevolucaoStatus.Solicitado;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }

    // Dados do Correios - Logística Reversa
    public string? IdPrePostagem { get; set; }           // ID da pré-postagem reversa nos Correios
    public string? CodigoPostagem { get; set; }          // Código de autorização de postagem (gerado pelos Correios)
    public string? CodigoRastreamento { get; set; }      // Código de rastreamento do envio
    public DateTime? DataLimitePostagem { get; set; }    // Data limite para o cliente postar
    public string? UrlEtiqueta { get; set; }             // URL da etiqueta para impressão (se aplicável)
    public string? RespostaCorreiosJson { get; set; }    // JSON completo da resposta dos Correios

    // Relacionamentos
    public Pedido Pedido { get; set; } = null!;
    public ICollection<DevolucaoItem> Itens { get; set; } = new List<DevolucaoItem>();
}

public class DevolucaoItem
{
    public int Id { get; set; }
    public int DevolucaoId { get; set; }
    public int ItemCarrinhoId { get; set; }
    public int ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = null!;
    public int? GradeId { get; set; }
    public string? GradeDescricao { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }

    // Relacionamentos
    public Devolucao Devolucao { get; set; } = null!;
    public ItemCarrinho? ItemCarrinho { get; set; }
}
