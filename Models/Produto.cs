using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace g4api.Models;

[Table("produto")]
public class Produto
{
    [Key]
    [Column("idProduto")]
    public int IdProduto { get; set; }

    [Column("codigoProduto")]
    public int? CodigoProduto { get; set; }

    [Column("grupoProduto")]
    public string? GrupoProduto { get; set; }

    [Column("referenciaProduto")]
    public string? ReferenciaProduto { get; set; }

    [Column("tituloEcommerceProduto")]
    public string? TituloEcommerceProduto { get; set; }

    [Column("descricaoConsultaProduto")]
    public string? DescricaoConsultaProduto { get; set; }

    [Column("aplicacaoConsultaProduto")]
    public string? AplicacaoConsultaProduto { get; set; }

    [Column("dadosAdicionaisProduto")]
    public string? DadosAdicionaisProduto { get; set; }

    [Column("precoTabelaProduto")]
    public decimal PrecoTabelaProduto { get; set; }

    [Column("precoMinimoProduto")]
    public decimal PrecoMinimoProduto { get; set; }

    [Column("qtdminProduto")]
    public decimal? QtdMinProduto { get; set; }

    [Column("dataalt")]
    public DateTime? DataAlteracao { get; set; }

    [Column("eanProduto")]
    public string? EanProduto { get; set; }

    [Column("img")]
    public string? Img { get; set; }

    [Column("pesoProduto")]
    public decimal? PesoProduto { get; set; }

    [Column("alturaProduto")]
    public decimal? AlturaProduto { get; set; }

    [Column("larguraProduto")]
    public decimal? LarguraProduto { get; set; }

    [Column("comprimentoProduto")]
    public decimal? ComprimentoProduto { get; set; }

    [Column("fabricante")]
    public string? Fabricante { get; set; }

    [Column("corpredominanteProduto")]
    public string? CorPredominanteProduto { get; set; }

    [Column("tamanhoProduto")]
    public string? TamanhoProduto { get; set; }
}
