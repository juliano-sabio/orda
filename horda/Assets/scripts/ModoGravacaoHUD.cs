using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// MODO GRAVAÇÃO — tecla F8 alterna esconder o HUD do jogo, deixando a tela limpa só com os
// números de dano (esses ficam no DamageCanvas em WorldSpace e NÃO são tocados aqui).
//
// Esconde: HUD de vida, XP, skills, dash, passiva, painel de evento, contador de mortes,
// progresso de missão, cronômetro, etc. Preserva o menu de pause (pra você ainda conseguir
// pausar/sair durante a gravação). Aperte F8 de novo pra restaurar tudo. Não altera nada
// permanente no jogo.
//
// Enquanto ligado, ele RE-ESCONDE periodicamente o que for surgindo: contador de mortes,
// passiva e painel de evento são criados só na hora (ex.: no 1º abate/evento).
//
// IMPORTANTE: alguns HUDs (ex.: contador de mortes) têm um "self-heal" no próprio Update que
// re-ativa o GameObject todo frame. Por isso, pra CANVAS, desligamos o COMPONENTE Canvas
// (Canvas.enabled = false) em vez do GameObject — o self-heal mexe no activeSelf, não no
// Canvas.enabled, então não desfaz o nosso esconder. Pra objetos comuns usamos SetActive.
public class ModoGravacaoHUD : MonoBehaviour
{
    const KeyCode TECLA = KeyCode.F8;
    const float   INTERVALO = 0.25f; // re-varre a tela a cada 0.25s enquanto ligado

    static ModoGravacaoHUD _i;

    bool  escondido;
    float proxVarredura;
    readonly List<GameObject> objsOcultos   = new List<GameObject>();
    readonly List<Canvas>     canvasOcultos = new List<Canvas>();

    // Nunca escondemos objetos com esses trechos no nome. Além do menu de pause, preservamos
    // as CARTAS de escolha (level-up de skill, evolução, status, etc.) — elas devem continuar
    // aparecendo durante a gravação pra você conseguir jogar/escolher normalmente.
    static readonly string[] PRESERVAR =
    {
        "Pause", "Settings",
        "Card", "SkillChoice", "SkillSelection", "SkillAcquired", "StatusPanel", "Evo"
    };

    // Canvases de HUD que ficam FORA do UIManager_Canvas (contador de mortes, progresso de
    // missão, painel de evento, ícones de skill).
    static readonly string[] HUD_CANVAS = { "ContadorMortesCanvas", "MissaoEspiritoCanvas", "EventoCanvas", "SkillIconsHUD_Canvas", "BarraDeLuzCanvas" };

    // Objetos avulsos de HUD (não são Canvas) escondidos por nome via Find.
    static readonly string[] AUX_OBJ = { "TimerBadge" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_i != null) return;
        var go = new GameObject("ModoGravacaoHUD");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<ModoGravacaoHUD>();
    }

    void OnEnable()  { SceneManager.sceneLoaded += AoTrocarCena; }
    void OnDisable() { SceneManager.sceneLoaded -= AoTrocarCena; }

    // Ao trocar de cena o HUD é recriado (visível) — zera o estado pra o F8 voltar a esconder.
    void AoTrocarCena(Scene s, LoadSceneMode m) { objsOcultos.Clear(); canvasOcultos.Clear(); escondido = false; }

    void Update()
    {
        if (Input.GetKeyDown(TECLA))
        {
            if (escondido) Restaurar();
            else { escondido = true; Varrer(broad: true); proxVarredura = Time.unscaledTime + INTERVALO; }
        }

        // Enquanto ligado, mantém a tela limpa (pega HUD que surge/reaparece depois).
        if (escondido && Time.unscaledTime >= proxVarredura)
        {
            proxVarredura = Time.unscaledTime + INTERVALO;
            Varrer(broad: false);
        }
    }

    // Esconde o HUD atualmente visível. 'broad' faz também a varredura pesada por nome (passiva),
    // usada só no momento em que liga (o resto do tempo usamos a versão leve).
    void Varrer(bool broad)
    {
        // 1) Filhos ativos do canvas principal: vida, XP, skills, dash, passiva, elemento, etc.
        var ui = GameObject.Find("UIManager_Canvas");
        if (ui != null)
            foreach (Transform f in ui.transform)
                Esconder(f.gameObject);

        // 2) Canvases de HUD separados (contador de mortes, missão, evento, ícones de skill).
        foreach (var cv in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (!cv.gameObject.scene.IsValid()) continue; // ignora prefabs/assets
            if (CasaHudCanvas(cv.gameObject.name)) Esconder(cv.gameObject);
        }

        // 3) Objetos avulsos de HUD (cronômetro).
        foreach (var nome in AUX_OBJ)
            Esconder(GameObject.Find(nome));

        // 4) A passiva pode estar num canvas próprio com nome variável — varre por nome (pesado).
        if (broad)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!t.gameObject.scene.IsValid()) continue;
                if (t.name.IndexOf("passiv", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                Esconder(t.gameObject);
            }
        }
    }

    void Esconder(GameObject go)
    {
        if (go == null) return;
        if (Preservado(go.name)) return;

        // Canvas → desliga o componente (imune ao self-heal que re-ativa o GameObject).
        var cv = go.GetComponent<Canvas>();
        if (cv != null)
        {
            if (!cv.enabled) return;
            cv.enabled = false;
            canvasOcultos.Add(cv);
            return;
        }

        // Objeto comum → desativa o GameObject.
        if (!go.activeSelf) return;
        go.SetActive(false);
        objsOcultos.Add(go);
    }

    void Restaurar()
    {
        foreach (var cv in canvasOcultos)
            if (cv != null) cv.enabled = true;
        foreach (var go in objsOcultos)
            if (go != null) go.SetActive(true);
        canvasOcultos.Clear();
        objsOcultos.Clear();
        escondido = false;
    }

    static bool Preservado(string nome)
    {
        foreach (var p in PRESERVAR)
            if (nome.IndexOf(p, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    static bool CasaHudCanvas(string nome)
    {
        foreach (var h in HUD_CANVAS)
            if (nome.IndexOf(h, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
}
