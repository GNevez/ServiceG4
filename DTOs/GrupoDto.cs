namespace g4api.DTOs;

public class GrupoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Img { get; set; }
    public List<SubGrupoDto> SubGrupos { get; set; } = new();
}

public class SubGrupoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Img { get; set; }
}
