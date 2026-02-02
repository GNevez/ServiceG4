using Microsoft.EntityFrameworkCore;
using g4api.Models;

namespace g4api.Data;

public class G4DbContext : DbContext
{
    public G4DbContext(DbContextOptions<G4DbContext> options) : base(options)
    {
    }

    public DbSet<ProdutoGrupo> ProdutoGrupos { get; set; }
    public DbSet<ProdutoSubGrupo> ProdutoSubGrupos { get; set; }
    public DbSet<Produto> Produtos { get; set; }
    public DbSet<ProdutoGrade> ProdutoGrades { get; set; }
    public DbSet<Carrinho> Carrinhos { get; set; }
    public DbSet<ItemCarrinho> ItensCarrinho { get; set; }
    public DbSet<Cupom> Cupons { get; set; }
    public DbSet<CupomUso> CuponsUso { get; set; }
    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<EnderecoEntrega> EnderecosEntrega { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Cadastro> Cadastros { get; set; }
    public DbSet<PrePostagem> PrePostagens { get; set; }
    public DbSet<Rotulo> Rotulos { get; set; }
    public DbSet<FilaImpressao> FilaImpressao { get; set; }
    public DbSet<Devolucao> Devolucoes { get; set; }
    public DbSet<DevolucaoItem> DevolucaoItens { get; set; }
    public DbSet<ProdutoGradeImagem> ProdutoGradeImagens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<ProdutoGrupo>(entity =>
        {
            entity.HasKey(e => e.IdProdutoGrupo);
            entity.ToTable("produto_grupo");
            entity.Property(e => e.IdProdutoGrupo).HasColumnType("int(11)");
            entity.Property(e => e.DescricaoGrupoProduto).HasMaxLength(50);
            entity.Property(e => e.Img).HasMaxLength(255);
        });

        modelBuilder.Entity<ProdutoSubGrupo>(entity =>
        {
            entity.HasKey(e => e.IdProdutoSubGrupo);
            entity.ToTable("produto_sub_grupo");
            entity.Property(e => e.IdProdutoSubGrupo).HasColumnType("int(11)");
            entity.Property(e => e.IdProdutoGrupo).HasColumnType("int(11)");
            entity.Property(e => e.DescricaoSubGrupoProduto).HasMaxLength(50);
            entity.Property(e => e.Img).HasMaxLength(255);

            entity.HasOne(d => d.Grupo)
                .WithMany(p => p.SubGrupos)
                .HasForeignKey(d => d.IdProdutoGrupo)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(e => e.IdProduto);
            entity.ToTable("produto");
        });

        modelBuilder.Entity<ProdutoGrade>(entity =>
        {
            entity.HasKey(e => e.IdProdutoGrade);
            entity.ToTable("produto_grade");
            
            entity.HasMany(g => g.Imagens)
                .WithOne(i => i.Grade)
                .HasForeignKey(i => i.IdProdutoGrade)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProdutoGradeImagem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("produto_grade_imagem");
            entity.Property(e => e.Caminho).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Ordem).HasColumnType("int");
        });

        // Carrinho
        modelBuilder.Entity<Carrinho>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("carrinho");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CupomCodigo).HasMaxLength(50);
            entity.Property(e => e.CupomDesconto).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<ItemCarrinho>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("carrinho_item");
            entity.Property(e => e.PrecoUnitario).HasColumnType("decimal(10,2)");

            entity.HasOne(i => i.Carrinho)
                .WithMany(c => c.Itens)
                .HasForeignKey(i => i.CarrinhoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Grade)
                .WithMany()
                .HasForeignKey(i => i.GradeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Cupons
        modelBuilder.Entity<Cupom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("cupons");
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(255);
            entity.Property(e => e.TipoDesconto).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ValorDesconto).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ValorMinimoCompra).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ValorMaximoDesconto).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<CupomUso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("cupons_uso");
            entity.Property(e => e.ValorDescontoAplicado).HasColumnType("decimal(10,2)");

            entity.HasOne(u => u.Cupom)
                .WithMany(c => c.Usos)
                .HasForeignKey(u => u.CupomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Pedidos
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("pedidos");
            entity.HasIndex(e => e.CodigoPedido).IsUnique();
            entity.Property(e => e.CodigoPedido).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.PrecoFrete).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalPedido).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DescontoCupom).HasColumnType("decimal(10,2)");
            entity.Property(e => e.MetodoPagamento).HasMaxLength(30).IsRequired();
            entity.Property(e => e.NomeCliente).HasMaxLength(150).IsRequired();
            entity.Property(e => e.EmailCliente).HasMaxLength(150).IsRequired();

            entity.HasOne(p => p.Carrinho)
                .WithMany()
                .HasForeignKey(p => p.CarrinhoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.EnderecoEntrega)
                .WithMany()
                .HasForeignKey(p => p.EnderecoEntregaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Endereços de Entrega
        modelBuilder.Entity<EnderecoEntrega>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("enderecos_entrega");
            entity.Property(e => e.Cep).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Logradouro).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Numero).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Complemento).HasMaxLength(100);
            entity.Property(e => e.Bairro).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Cidade).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Uf).HasMaxLength(2).IsRequired();
            entity.Property(e => e.NomeDestinatario).HasMaxLength(150).IsRequired();
            entity.Property(e => e.TelefoneDestinatario).HasMaxLength(20);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("clientes");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Cpf);
            entity.Property(e => e.Nome).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Cpf).HasMaxLength(14);
            entity.Property(e => e.Telefone).HasMaxLength(20);
        });

        // Cadastros (usuários do sistema - admins e clientes)
        modelBuilder.Entity<Cadastro>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("cadastros");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Nome).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Sobrenome).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Senha).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Cpf).HasMaxLength(14).IsRequired();
            entity.Property(e => e.Telefone).HasColumnName("numero").HasMaxLength(20);
            entity.Property(e => e.DataNascimento).HasColumnName("nascimento");
            entity.Property(e => e.IsBanned).HasColumnName("isBanned").HasDefaultValue(0);
            entity.Property(e => e.Cargo).HasMaxLength(20).HasDefaultValue("Cliente");
            entity.Property(e => e.CodigoAutenticacao).HasColumnName("Aut").HasMaxLength(40).HasDefaultValue("0");
        });

        // Pré-Postagens dos Correios
        modelBuilder.Entity<PrePostagem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("pre_postagens");
            entity.Property(e => e.CodigoRastreamento).HasMaxLength(50);
            entity.Property(e => e.IdPrePostagem).HasMaxLength(100);
            entity.Property(e => e.NumeroEtiqueta).HasMaxLength(50);
            entity.Property(e => e.CodigoServico).HasMaxLength(20);
            entity.Property(e => e.NomeServico).HasMaxLength(100);
            entity.Property(e => e.Peso).HasColumnType("decimal(10,3)");
            entity.Property(e => e.Altura).HasColumnType("int");
            entity.Property(e => e.Largura).HasColumnType("int");
            entity.Property(e => e.Comprimento).HasColumnType("int");
            entity.Property(e => e.ValorDeclarado).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.MensagemErro).HasMaxLength(1000);
            entity.Property(e => e.RespostaCorreiosJson).HasColumnType("longtext");
            
            entity.HasOne(e => e.Pedido)
                .WithMany()
                .HasForeignKey(e => e.PedidoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Rótulos dos Correios
        modelBuilder.Entity<Rotulo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("rotulos");
            entity.Property(e => e.IdRecibo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IdAtendimento).HasMaxLength(100);
            entity.Property(e => e.NomeArquivo).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CaminhoArquivo).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CodigosObjeto).HasMaxLength(1000);
            entity.Property(e => e.IdsPrePostagem).HasMaxLength(1000);
            entity.Property(e => e.TipoRotulo).HasMaxLength(10);
            entity.Property(e => e.FormatoRotulo).HasMaxLength(10);
            entity.Property(e => e.Observacao).HasMaxLength(500);
            
            entity.HasIndex(e => e.IdRecibo).IsUnique();
            
            entity.HasOne(e => e.Pedido)
                .WithMany()
                .HasForeignKey(e => e.IdPedido)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Fila de Impressão
        modelBuilder.Entity<FilaImpressao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("fila_impressao");
            entity.Property(e => e.NomeArquivo).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CaminhoArquivo).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.MensagemErro).HasMaxLength(1000);
            entity.Property(e => e.ImpressoraDestino).HasMaxLength(100);
            entity.Property(e => e.ClienteId).HasMaxLength(100);
            entity.Property(e => e.CodigoPedido).HasMaxLength(50);

            entity.HasOne(e => e.Rotulo)
                .WithMany()
                .HasForeignKey(e => e.RotuloId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Devoluções
        modelBuilder.Entity<Devolucao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("devolucoes");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PedidoId).HasColumnName("pedido_id");
            entity.Property(e => e.Cpf).HasColumnName("cpf").HasMaxLength(20).IsRequired();
            entity.Property(e => e.NomeCliente).HasColumnName("nome_cliente").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Telefone).HasColumnName("telefone").HasMaxLength(30);
            entity.Property(e => e.Motivo).HasColumnName("motivo").HasColumnType("text");
            entity.Property(e => e.Observacoes).HasColumnName("observacoes").HasColumnType("text");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<int>();
            entity.Property(e => e.DataCriacao).HasColumnName("data_criacao");
            entity.Property(e => e.DataAtualizacao).HasColumnName("data_atualizacao");
            entity.Property(e => e.IdPrePostagem).HasColumnName("id_pre_postagem").HasMaxLength(100);
            entity.Property(e => e.CodigoPostagem).HasColumnName("codigo_postagem").HasMaxLength(100);
            entity.Property(e => e.CodigoRastreamento).HasColumnName("codigo_rastreamento").HasMaxLength(50);
            entity.Property(e => e.DataLimitePostagem).HasColumnName("data_limite_postagem");
            entity.Property(e => e.UrlEtiqueta).HasColumnName("url_etiqueta").HasMaxLength(500);
            entity.Property(e => e.RespostaCorreiosJson).HasColumnName("resposta_correios_json").HasColumnType("longtext");

            entity.HasOne(e => e.Pedido)
                .WithMany()
                .HasForeignKey(e => e.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DevolucaoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("devolucao_itens");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DevolucaoId).HasColumnName("devolucao_id");
            entity.Property(e => e.ItemCarrinhoId).HasColumnName("item_carrinho_id");
            entity.Property(e => e.ProdutoId).HasColumnName("produto_id");
            entity.Property(e => e.ProdutoNome).HasColumnName("produto_nome").HasMaxLength(500).IsRequired();
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.GradeDescricao).HasColumnName("grade_descricao").HasMaxLength(200);
            entity.Property(e => e.Quantidade).HasColumnName("quantidade");
            entity.Property(e => e.PrecoUnitario).HasColumnName("preco_unitario").HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Devolucao)
                .WithMany(d => d.Itens)
                .HasForeignKey(e => e.DevolucaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ItemCarrinho)
                .WithMany()
                .HasForeignKey(e => e.ItemCarrinhoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
