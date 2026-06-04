using System.Text;

/// <summary>
/// Remove acentos e caracteres especiais do português para compatibilidade
/// com fontes que não possuem esses glifos (ex: Retro Gaming SDF).
/// </summary>
public static class TextUtils
{
    public static string SemAcento(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        var sb = new StringBuilder(texto.Length);
        foreach (char c in texto)
        {
            switch (c)
            {
                case 'Á': case 'À': case 'Â': case 'Ã': sb.Append('A'); break;
                case 'É': case 'Ê':                     sb.Append('E'); break;
                case 'Í':                               sb.Append('I'); break;
                case 'Ó': case 'Ô': case 'Õ':           sb.Append('O'); break;
                case 'Ú':                               sb.Append('U'); break;
                case 'Ç':                               sb.Append('C'); break;
                case 'á': case 'à': case 'â': case 'ã': sb.Append('a'); break;
                case 'é': case 'ê':                     sb.Append('e'); break;
                case 'í':                               sb.Append('i'); break;
                case 'ó': case 'ô': case 'õ':           sb.Append('o'); break;
                case 'ú':                               sb.Append('u'); break;
                case 'ç':                               sb.Append('c'); break;
                default:                                sb.Append(c);  break;
            }
        }
        return sb.ToString();
    }
}
