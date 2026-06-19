using UnityEngine;

// Barra de revive acima do player caído. Lê PlayerNet.Caido/ReviveProgresso.
public class ReviveBarUI : MonoBehaviour
{
    [SerializeField] float altura = 1.2f;
    [SerializeField] float largura = 1.0f;

    PlayerNet net;
    Transform raiz;
    Transform fill;

    void Awake()
    {
        net = GetComponent<PlayerNet>();
        var sprite = MakeSquareSprite();

        raiz = NovoSR("ReviveBar_BG", new Color(0.08f, 0.05f, 0.05f, 0.9f), sprite, 0).transform;
        raiz.SetParent(transform, false);
        raiz.localPosition = new Vector3(0f, altura, 0f);
        raiz.localScale = new Vector3(largura, 0.14f, 1f);

        fill = NovoSR("ReviveBar_Fill", new Color(0.95f, 0.82f, 0.30f, 1f), sprite, 1).transform;
        fill.SetParent(raiz, false);
        fill.localScale = Vector3.one;
        raiz.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        bool caido = net != null && net.Caido;
        if (raiz.gameObject.activeSelf != caido) raiz.gameObject.SetActive(caido);
        if (!caido) return;
        float p = Mathf.Clamp01(net.ReviveProgresso);
        fill.localScale = new Vector3(p, 1f, 1f);
        fill.localPosition = new Vector3(-(1f - p) * 0.5f, 0f, 0f); // ancora à esquerda
        raiz.rotation = Quaternion.identity; // não gira com o player
    }

    GameObject NovoSR(string nome, Color cor, Sprite sp, int ordem)
    {
        var go = new GameObject(nome);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp; sr.color = cor; sr.sortingOrder = 50 + ordem;
        return go;
    }

    static Sprite _sq;
    static Sprite MakeSquareSprite()
    {
        if (_sq != null) return _sq;
        var tex = new Texture2D(2, 2);
        var px = new Color[4]; for (int i = 0; i < 4; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        _sq = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _sq;
    }
}
