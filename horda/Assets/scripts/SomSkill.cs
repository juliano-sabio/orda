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
        LancaDisparoDark, LancaImpactoDark,
        EspadaCorteDark, EspadaImpactoDark,
        EstrelaQuedaDark, EstrelaImpactoDark,
        CorteLancamentoDark, CorteImpactoDark,
        GarraErupcaoDark, GarraDissiparDark
    }

    static AudioClip _impacto, _disparo, _explosao;
    static AudioClip _missilDisp, _missilImp, _missilExp;
    static AudioClip _corrCanal, _corrDesc, _corrTick;
    static AudioClip _chicEstalo, _chicImp;
    static AudioClip _pulso;
    static AudioClip _lancaDisp, _lancaImp;
    static AudioClip _espCorte, _espImp;
    static AudioClip _estQueda, _estImp;
    static AudioClip _corteLanc, _corteImp;
    static AudioClip _garraErup, _garraDiss;

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
    static AudioClip EspCorte   => _espCorte   ??= Carregar("espada_corte_dark");
    static AudioClip EspImp     => _espImp     ??= Carregar("espada_impacto_dark");
    static AudioClip EstQueda   => _estQueda   ??= Carregar("estrela_queda_dark");
    static AudioClip EstImp     => _estImp     ??= Carregar("estrela_impacto_dark");
    static AudioClip CorteLanc  => _corteLanc  ??= Carregar("corte_lancamento_dark");
    static AudioClip CorteImp   => _corteImp   ??= Carregar("corte_impacto_dark");
    static AudioClip GarraErup  => _garraErup  ??= Carregar("garra_erupcao_dark");
    static AudioClip GarraDiss  => _garraDiss  ??= Carregar("garra_dissipar_dark");

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
            Tipo.EspadaCorteDark   => EspCorte,
            Tipo.EspadaImpactoDark => EspImp,
            Tipo.EstrelaQuedaDark   => EstQueda,
            Tipo.EstrelaImpactoDark => EstImp,
            Tipo.CorteLancamentoDark => CorteLanc,
            Tipo.CorteImpactoDark    => CorteImp,
            Tipo.GarraErupcaoDark  => GarraErup,
            Tipo.GarraDissiparDark => GarraDiss,
            _             => null
        };
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, posicao, volume);
    }
}
