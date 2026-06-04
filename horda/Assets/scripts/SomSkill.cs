using UnityEngine;

/// <summary>
/// Toca sons de skill carregados de Resources/Sons/.
/// Uso: SomSkill.Tocar(SomSkill.Tipo.Disparo, posicao);
/// </summary>
public static class SomSkill
{
    public enum Tipo { Impacto, Disparo, Explosao }

    static AudioClip _impacto, _disparo, _explosao;

    static AudioClip Carregar(string nome)
    {
        var clip = Resources.Load<AudioClip>("Sons/" + nome);
        if (clip == null) Debug.LogWarning($"[SomSkill] Som não encontrado: Sons/{nome}");
        return clip;
    }

    static AudioClip Impacto  => _impacto  ??= Carregar("som_impacto");
    static AudioClip Disparo  => _disparo  ??= Carregar("som_disparo");
    static AudioClip Explosao => _explosao ??= Carregar("som_explosao");

    public static void Tocar(Tipo tipo, Vector2 posicao, float volume = 0.7f)
    {
        AudioClip clip = tipo switch
        {
            Tipo.Impacto  => Impacto,
            Tipo.Disparo  => Disparo,
            Tipo.Explosao => Explosao,
            _             => null
        };
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, posicao, volume);
    }
}
