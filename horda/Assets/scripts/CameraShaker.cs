using UnityEngine;

// Roda depois do Cinemachine (ordem -100) para sobrescrever a posição final da câmera
[DefaultExecutionOrder(1000)]
public class CameraShaker : MonoBehaviour
{
    private float intensidade;
    private float duracao;
    private float tempoRestante;

    // Dispara o shake na Camera.main. Em co-op, a maioria das chamadas vem da lógica
    // host-only (bosses/eventos) → o host propaga o tremor pros clientes.
    public static void Tremer(float intensidade, float duracao)
    {
        TremerLocal(intensidade, duracao);

        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.IsServer && CoopPauseManager.Instance != null)
            CoopPauseManager.Instance.TremerClientRpc(intensidade, duracao);
    }

    // Preferência "CameraShake" (aba Jogo das opções). Cacheada; o toggle chama
    // AtualizarPreferencia() pra refletir ao vivo.
    static int _ativoCache = -1; // -1 = ainda não lido
    public static bool Ativo
    {
        get { if (_ativoCache < 0) _ativoCache = PlayerPrefs.GetInt("CameraShake", 1); return _ativoCache == 1; }
    }
    public static void AtualizarPreferencia() => _ativoCache = PlayerPrefs.GetInt("CameraShake", 1);

    // Aplica o shake só na câmera local (sem propagar). Usado pelo broadcast co-op.
    public static void TremerLocal(float intensidade, float duracao)
    {
        if (!Ativo) return; // opção "Camera Shake" desligada
        if (Camera.main == null) return;
        CameraShaker s = Camera.main.GetComponent<CameraShaker>();
        if (s == null) s = Camera.main.gameObject.AddComponent<CameraShaker>();
        s.intensidade   = intensidade;
        s.duracao       = duracao;
        s.tempoRestante = duracao;
    }

    void LateUpdate()
    {
        if (tempoRestante <= 0f) return;

        // O Cinemachine já resetou a posição da câmera neste frame — basta somar o offset
        float pct = tempoRestante / duracao;
        float mag = intensidade * pct;
        transform.localPosition += new Vector3(
            Random.Range(-mag, mag),
            Random.Range(-mag, mag),
            0f
        );

        tempoRestante -= Time.deltaTime;
    }
}
