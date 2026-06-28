using UnityEngine;

// Game-feel de impacto centralizado: hit-stop (freeze-frame) + micro screen-shake.
//
// O hit-stop é SINGLE-PLAYER apenas. Em co-op o tempo é host-autoritativo/compartilhado;
// congelar Time.timeScale localmente dessincronizaria a horda (NGO não é gateado por
// timeScale, mas movimento/física sim). Em co-op o "peso" do golpe vem do shake + flash,
// que são seguros por-player.
//
// Reservado a momentos que importam (crítico, morte de boss/elite) — NUNCA em todo kill de
// mob, senão uma horda morrendo junto viraria stutter.
public static class Impacto
{
    static float ultimoHitStop = -10f;
    const float COOLDOWN_HITSTOP = 0.10f; // teto ~10 freezes/seg → sem stutter

    // Acerto crítico do player: freeze curtinho (SP) + tremor bem sutil (local, nos dois lados).
    public static void Critico()
    {
        HitStop(0.035f);
        CameraShaker.TremerLocal(0.06f, 0.09f);
    }

    // Morte de boss/elite: freeze maior (SP). O tremor de câmera é disparado por quem chama
    // (via CameraShaker.Tremer, que propaga pros clientes em co-op).
    public static void MorteBoss()
    {
        HitStop(0.07f);
    }

    static void HitStop(float duracao)
    {
        if (NetSpawn.EmRede) return;                                  // co-op: não congela o tempo
        if (Time.unscaledTime - ultimoHitStop < COOLDOWN_HITSTOP) return;
        ultimoHitStop = Time.unscaledTime;
        HitStopRunner.Get().Congelar(duracao);
    }
}

// Runner persistente: zera Time.timeScale por uns ms (em tempo REAL) e restaura.
public class HitStopRunner : MonoBehaviour
{
    static HitStopRunner _inst;
    public static HitStopRunner Get()
    {
        if (_inst == null)
        {
            var go = new GameObject("HitStopRunner");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<HitStopRunner>();
        }
        return _inst;
    }

    float congelarAte;
    bool ativo;
    float escalaAnterior = 1f;

    public void Congelar(float duracao)
    {
        // Não interferir com pause/menu (timeScale já 0 e não fomos nós que congelamos).
        if (!ativo && Time.timeScale == 0f) return;

        // Teto: nunca segurar mais que 0.12s no total (evita travar em rajada de crits).
        float alvo = Mathf.Min(Time.realtimeSinceStartup + duracao,
                               Time.realtimeSinceStartup + 0.12f);
        if (alvo > congelarAte) congelarAte = alvo;
        if (!ativo) StartCoroutine(Rotina());
    }

    System.Collections.IEnumerator Rotina()
    {
        ativo = true;
        escalaAnterior = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        while (Time.realtimeSinceStartup < congelarAte) yield return null;
        // Só restaura se continuamos nós (timeScale ainda 0) — segurança mínima contra pause.
        if (Time.timeScale == 0f) Time.timeScale = escalaAnterior;
        ativo = false;
    }
}
