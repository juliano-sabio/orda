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
        GarraErupcaoDark, GarraDissiparDark,
        GeloDisparoDark, GeloImpactoDark,
        LaminaFuriaDark, LaminaImpactoDark,
        SombraCruzDisparoDark, SombraCruzImpactoDark,
        EspinhoPulsoDark,
        CartaSelecaoDark,
        EvolucaoSelecaoDark,
        PrincesaCanalizar, PrincesaDisparo, PrincesaFase2, PrincesaMorte,
        RaioCarga, RaioDisparo, RaioBounce,
        TempestadeInicio, TempestadeLoop, TempestadeRaio,
        DomoInicio, DomoLoop, DomoFim,
        NecropoleInicio, NecropoleLoop, NecropoleFantasma,
        DrenagemInicio, DrenagemLoop, DrenagemFim
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
    static AudioClip _geloDisp, _geloImp;
    static AudioClip _lamFuria, _lamImp;
    static AudioClip _scDisp, _scImp;
    static AudioClip _espinho;
    static AudioClip _cartaSel;
    static AudioClip _evoSel;
    static AudioClip _prCanal, _prDisp, _prFase2, _prMorte;
    static AudioClip _raioCarga, _raioDisp, _raioBounce;
    static AudioClip _tempInicio, _tempLoop, _tempRaio;
    static AudioClip _domoInicio, _domoLoop, _domoFim;
    static AudioClip _necInicio, _necLoop, _necFant;
    static AudioClip _drenInicio, _drenLoop, _drenFim;

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
    static AudioClip GeloDisp   => _geloDisp   ??= Carregar("gelo_disparo_dark");
    static AudioClip GeloImp    => _geloImp    ??= Carregar("gelo_impacto_dark");
    static AudioClip LamFuria   => _lamFuria   ??= Carregar("lamina_furia_dark");
    static AudioClip LamImp     => _lamImp     ??= Carregar("lamina_impacto_dark");
    static AudioClip ScDisp     => _scDisp     ??= Carregar("sombracruz_disparo_dark");
    static AudioClip ScImp      => _scImp      ??= Carregar("sombracruz_impacto_dark");
    static AudioClip EspinhoPulso => _espinho  ??= Carregar("espinho_pulso_dark");
    static AudioClip CartaSel   => _cartaSel   ??= Carregar("carta_select_dark");
    static AudioClip EvoSel     => _evoSel     ??= Carregar("evolucao_select_dark");
    static AudioClip PrCanal    => _prCanal    ??= Carregar("princesa_canalizar");
    static AudioClip PrDisp     => _prDisp     ??= Carregar("princesa_disparo");
    static AudioClip PrFase2    => _prFase2    ??= Carregar("princesa_fase2");
    static AudioClip PrMorte    => _prMorte    ??= Carregar("princesa_morte");
    static AudioClip RaioCargaC  => _raioCarga  ??= Carregar("raio_carga");
    static AudioClip RaioDispC   => _raioDisp   ??= Carregar("raio_disparo");
    static AudioClip RaioBounceC => _raioBounce ??= Carregar("raio_bounce");
    static AudioClip TempInicio  => _tempInicio ??= Carregar("tempestade_inicio");
    static AudioClip TempLoop     => _tempLoop   ??= Carregar("tempestade_loop");
    static AudioClip TempRaio     => _tempRaio   ??= Carregar("tempestade_raio");
    static AudioClip DomoInicioC => _domoInicio ??= Carregar("domo_inicio");
    static AudioClip DomoLoopC    => _domoLoop   ??= Carregar("domo_loop");
    static AudioClip DomoFimC     => _domoFim    ??= Carregar("domo_fim");
    static AudioClip NecInicio   => _necInicio  ??= Carregar("necropole_inicio");
    static AudioClip NecLoop      => _necLoop    ??= Carregar("necropole_loop");
    static AudioClip NecFant      => _necFant    ??= Carregar("necropole_fantasma");
    static AudioClip DrenInicio  => _drenInicio ??= Carregar("drenagem_inicio");
    static AudioClip DrenLoop     => _drenLoop   ??= Carregar("drenagem_loop");
    static AudioClip DrenFim      => _drenFim    ??= Carregar("drenagem_fim");

    static AudioClip ClipDe(Tipo tipo)
    {
        return tipo switch
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
            Tipo.GeloDisparoDark => GeloDisp,
            Tipo.GeloImpactoDark => GeloImp,
            Tipo.LaminaFuriaDark   => LamFuria,
            Tipo.LaminaImpactoDark => LamImp,
            Tipo.SombraCruzDisparoDark => ScDisp,
            Tipo.SombraCruzImpactoDark => ScImp,
            Tipo.EspinhoPulsoDark => EspinhoPulso,
            Tipo.CartaSelecaoDark => CartaSel,
            Tipo.EvolucaoSelecaoDark => EvoSel,
            Tipo.PrincesaCanalizar => PrCanal,
            Tipo.PrincesaDisparo   => PrDisp,
            Tipo.PrincesaFase2     => PrFase2,
            Tipo.PrincesaMorte     => PrMorte,
            Tipo.RaioCarga   => RaioCargaC,
            Tipo.RaioDisparo => RaioDispC,
            Tipo.RaioBounce  => RaioBounceC,
            Tipo.TempestadeInicio => TempInicio,
            Tipo.TempestadeLoop   => TempLoop,
            Tipo.TempestadeRaio   => TempRaio,
            Tipo.DomoInicio => DomoInicioC,
            Tipo.DomoLoop   => DomoLoopC,
            Tipo.DomoFim    => DomoFimC,
            Tipo.NecropoleInicio   => NecInicio,
            Tipo.NecropoleLoop     => NecLoop,
            Tipo.NecropoleFantasma => NecFant,
            Tipo.DrenagemInicio => DrenInicio,
            Tipo.DrenagemLoop   => DrenLoop,
            Tipo.DrenagemFim    => DrenFim,
            _             => null
        };
    }

    public static void Tocar(Tipo tipo, Vector2 posicao, float volume = 0.7f)
    {
        var clip = ClipDe(tipo);
        if (clip != null) AudioBus.PlaySfx(clip, posicao, volume);
    }

    // Som de UI/menu: toca em 2D e IGNORA o AudioListener.pause (menus de carta pausam o áudio).
    public static void TocarUI(Tipo tipo, float volume = 0.7f)
    {
        var clip = ClipDe(tipo);
        if (clip == null) return;
        var go = new GameObject("SomUI");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f;          // 2D
        src.ignoreListenerPause = true; // toca mesmo com o jogo pausado
        src.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }

    // Toca em LOOP (ex.: canalização que dura até ser interrompida). Retorna o AudioSource;
    // pare com Destroy(src.gameObject). Se 'parent' for dado, o som segue/é limpo com ele.
    public static AudioSource TocarLoop(Tipo tipo, Transform parent, float volume = 0.7f)
    {
        var clip = ClipDe(tipo);
        if (clip == null) return null;
        var go = new GameObject("SomLoop");
        if (parent != null) go.transform.SetParent(parent, false);
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.loop = true;
        src.spatialBlend = 0f;
        src.Play();
        return src;
    }
}
