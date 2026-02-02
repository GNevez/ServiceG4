namespace g4api.Models;

public enum StatusPedido
{
    AguardandoConfirmacao = 0, // Aguardando confirmação do pagamento
    EmSeparacao = 1,           // Pagamento confirmado, separando produtos
    ACaminho = 2,              // Enviado para transportadora
    Finalizado = 3,            // Entregue
    Cancelado = 4              // Cancelado
}
