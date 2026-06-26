using UnityEngine;

/// <summary>
/// Toca sons de skill carregados de Resources/Sons/.
/// Uso: SomSkill.Tocar(SomSkill.Tipo.Disparo, posicao);
/// </summary>
public static class SomSkill
{
    public enum Tipo
    {
        Impacto, Disparo, Explosao,
        MissilDisparoDark, MissilImpactoDark, MissilExplosaoDark,
        CorrenteCanalDark, CorrenteDescargaDark, CorrenteTickDark,
        ChicoteEstaloDark, ChicoteImpactoDark,
        PulsoDark,
        LancaDisparoDark, LancaImpactoDark
    }

    static AudioClip _impacto, _disparo, _explosao;
    static AudioClip _missilDisp, _missilImp, _missilExp;
    static AudioClip _corrCanal, _corrDesc, _corrTick;
    static AudioClip _chicEstalo, _chicImp;
    static AudioClip _pulso;
    static AudioClip _lancaDisp, _lancaImp;

    static AudioClip Carregar(string nome)
    {
        var clip = Resources.Load<AudioClip>("Sons/" + nome);
        if (clip == null) Debug.LogWarning($"[SomSkill] Som não encontrado: Sons/{nome}");
        return clip;
    }

    static AudioClip Impacto  => _impacto  ??= Carregar("som_impacto");
    static AudioClip Disparo  => _disparo  ??= Carregar("som_disparo");
    static AudioClip Explosao => _explosao ??= Carregar("som_explosao");
    static AudioClip MissilDisp => _missilDisp ??= Carregar("missil_disparo_dark");
    static AudioClip MissilImp  => _missilImp  ??= Carregar("missil_impacto_dark");
    static AudioClip MissilExp  => _missilExp  ??= Carregar("missil_explosao_dark");
    static AudioClip CorrCanal => _corrCanal ??= Carregar("corrente_canal_dark");
    static AudioClip CorrDesc  => _corrDesc  ??= Carregar("corrente_descarga_dark");
    static AudioClip CorrTick  => _corrTick  ??= Carregar("corrente_tick_dark");
    static AudioClip ChicEstalo => _chicEstalo ??= Carregar("chicote_estalo_dark");
    static AudioClip ChicImp    => _chicImp    ??= Carregar("chicote_impacto_dark");
    static AudioClip Pulso      => _pulso      ??= Carregar("pulso_dark");
    static AudioClip LancaDisp  => _lancaDisp  ??= Carregar("lanca_disparo_dark");
    static AudioClip LancaImp   => _lancaImp   ??= Carregar("lanca_impacto_dark");

    public static void Tocar(Tipo tipo, Vector2 posicao, float volume = 0.7f)
    {
        AudioClip clip = tipo switch
        {
            Tipo.Impacto  => Impacto,
            Tipo.Disparo  => Disparo,
            Tipo.Explosao => Explosao,
            Tipo.MissilDisparoDark  => MissilDisp,
            Tipo.MissilImpactoDark  => MissilImp,
            Tipo.MissilExplosaoDark => MissilExp,
            Tipo.CorrenteCanalDark    => CorrCanal,
            Tipo.CorrenteDescargaDark => CorrDesc,
            Tipo.CorrenteTickDark     => CorrTick,
            Tipo.ChicoteEstaloDark  => ChicEstalo,
            Tipo.ChicoteImpactoDark => ChicImp,
            Tipo.PulsoDark => Pulso,
            Tipo.LancaDisparoDark => LancaDisp,
            Tipo.LancaImpactoDark => LancaImp,
            _             => null
        };
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, posicao, volume);
    }
}
