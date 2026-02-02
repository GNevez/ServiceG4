using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace g4api.Extensions;

public static class StringExtensions
{
    public static string GenerateSlug(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalizar e remover acentos
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // Converter para minúsculas
        result = result.ToLowerInvariant();

        // Remover caracteres inválidos
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");

        // Converter espaços múltiplos em um único espaço
        result = Regex.Replace(result, @"\s+", " ").Trim();

        // Converter espaços em hífens
        result = Regex.Replace(result, @"\s", "-");

        // Remover hífens múltiplos
        result = Regex.Replace(result, @"-+", "-");

        return result;
    }
}
