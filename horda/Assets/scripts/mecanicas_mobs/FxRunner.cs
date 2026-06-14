using UnityEngine;

// Host persistente para coroutines de efeitos visuais (explosões, brasas, etc.)
// que precisam terminar mesmo depois que o GameObject que as iniciou é destruído
// (ex: inimigo morrendo) — evita sprites de efeito que ficam "travados" na tela.
public class FxRunner : MonoBehaviour
{
    static FxRunner _instance;

    public static FxRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("FxRunner");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<FxRunner>();
            }
            return _instance;
        }
    }
}
