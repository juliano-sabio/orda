using System.Collections;
using UnityEngine;

// Poeira de corrida: pequenos "puffs" nos pés enquanto o player anda. Roda em TODAS as cópias
// (dono + fantoche): só observa a posição local do transform — que o NetworkTransform já
// sincroniza no fantoche — então a paridade co-op é POR CONSTRUÇÃO, sem nenhum RPC. Cada
// máquina mostra a poeira dos dois personagens.
//
// Decoplado do dash: ignora velocidades altas (= dash, que já tem o DashEffect próprio).
public class MovementDust : MonoBehaviour
{
    [Header("Cadência")]
    public float distanciaPorPuff = 0.5f;   // a cada X unidades andadas → 1 puff
    public float velocidadeMinima = 1.2f;   // abaixo disso, parado → sem poeira
    public float velocidadeMaxima = 12f;    // acima disso, é dash → DashEffect cuida

    [Header("Visual")]
    public Color cor = new Color(0.78f, 0.72f, 0.6f, 0.5f);
    public float tamanho = 0.24f;
    public float vida = 0.35f;
    public float offsetY = -0.32f;          // nos "pés", abaixo do centro

    SpriteRenderer srPlayer;
    moviment_player2 mov;
    Vector3 ultimaPos;
    float distAcumulada;
    static Sprite spritePuff;

    void Awake()
    {
        srPlayer = GetComponent<SpriteRenderer>();
        mov = GetComponent<moviment_player2>();
        ultimaPos = transform.position;
        if (spritePuff == null) spritePuff = CriarPuff();
    }

    void Update()
    {
        Vector3 pos = transform.position;
        float d = Vector2.Distance(pos, ultimaPos);
        float vel = Time.deltaTime > 0f ? d / Time.deltaTime : 0f;
        ultimaPos = pos;

        // Sem poeira quando imobilizado (teleporte de portal, paralisia) — senão o movimento
        // forçado deixa um rastro de poeira atravessando a tela.
        if (mov != null && mov.Imobilizado) { distAcumulada = 0f; return; }

        if (vel < velocidadeMinima || vel > velocidadeMaxima)
        {
            distAcumulada = 0f;
            return;
        }

        distAcumulada += d;
        if (distAcumulada >= distanciaPorPuff)
        {
            distAcumulada = 0f;
            SpawnPuff(pos);
        }
    }

    void SpawnPuff(Vector3 origem)
    {
        var go = new GameObject("MovDust");
        go.transform.position = origem + new Vector3(Random.Range(-0.08f, 0.08f), offsetY, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spritePuff;
        sr.color = cor;
        if (srPlayer != null)
        {
            sr.sortingLayerID = srPlayer.sortingLayerID;
            sr.sortingOrder = srPlayer.sortingOrder - 1; // atrás/abaixo do player
        }

        Vector2 drift = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.05f, 0.25f));
        StartCoroutine(AnimarPuff(sr, drift));
    }

    IEnumerator AnimarPuff(SpriteRenderer sr, Vector2 drift)
    {
        if (sr == null) yield break;
        Transform t = sr.transform;
        Color c0 = sr.color;
        float escala0 = tamanho * Random.Range(0.7f, 1.1f);
        for (float e = 0f; e < vida; e += Time.deltaTime)
        {
            if (sr == null) yield break;
            float p = e / vida;
            t.position += (Vector3)(drift * Time.deltaTime);
            t.localScale = Vector3.one * Mathf.Lerp(escala0 * 0.5f, escala0 * 1.4f, p);
            sr.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(c0.a, 0f, p));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    static Sprite CriarPuff()
    {
        int sz = 24;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2(sz / 2f, sz / 2f);
        float raio = sz / 2f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), c);
            float a = Mathf.Pow(Mathf.Clamp01(1f - dist / raio), 1.4f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
