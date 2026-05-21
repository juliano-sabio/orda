using UnityEngine;

// Roda depois do Cinemachine (ordem -100) para sobrescrever a posição final da câmera
[DefaultExecutionOrder(1000)]
public class CameraShaker : MonoBehaviour
{
    private float intensidade;
    private float duracao;
    private float tempoRestante;

    // Dispara o shake na Camera.main (adiciona o componente automaticamente se necessário)
    public static void Tremer(float intensidade, float duracao)
    {
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
