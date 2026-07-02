#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Painel de debug (só no Editor) pra spawnar inimigos na hora e testar as
// habilidades / a replicação de VFX pro P2 em co-op. Aperte F9 pra mostrar/ocultar.
//
// O spawn passa pelo NetSpawn (host-autoritativo): em co-op quem spawna é o host
// e o inimigo é replicado pro cliente — do mesmo jeito que no jogo de verdade.
// Auto-cria em qualquer cena de jogo; não vai pro build (envolto em UNITY_EDITOR).
public class DebugSpawnMobs : MonoBehaviour
{
    static DebugSpawnMobs _inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_inst != null) return;
        var go = new GameObject("[DebugSpawnMobs]");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<DebugSpawnMobs>();
    }

    struct MobEntry { public string nome; public GameObject prefab; }

    readonly List<MobEntry> _mobs = new();
    bool    _carregado;
    bool    _visivel;
    Vector2 _scroll;
    string  _msg;
    float   _msgAte;

    void CarregarPrefabs()
    {
        _mobs.Clear();
        // Varre a pasta de inimigos (nome da pasta tem o typo "prefebs" mesmo).
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/prefebs/inimigos" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab   = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            // Só inimigos de verdade (têm InimigoController / NetworkObject).
            if (prefab.GetComponent<InimigoController>() == null) continue;
            _mobs.Add(new MobEntry { nome = prefab.name, prefab = prefab });
        }
        _mobs.Sort((a, b) => string.CompareOrdinal(a.nome, b.nome));
        _carregado = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            _visivel = !_visivel;
            if (_visivel && !_carregado) CarregarPrefabs();
        }
    }

    Vector3 PosDeSpawn(int i)
    {
        var alvo = PlayerStats.Local != null ? PlayerStats.Local.transform.position
                                             : Vector3.zero;
        // Anel de pontos ao redor do player pra não empilhar tudo no mesmo lugar.
        float ang = i * 0.9f;
        return alvo + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * 2.5f;
    }

    void Spawnar(MobEntry m)
    {
        if (!NetSpawn.PodeSpawnar)
        {
            Mostrar("Só o HOST spawna (você é cliente).");
            return;
        }
        var go = NetSpawn.Spawnar(m.prefab, PosDeSpawn(_mobs.IndexOf(m)));
        Mostrar(go != null ? $"Spawnou {m.nome}" : $"Falhou spawnar {m.nome}");
    }

    void Mostrar(string s) { _msg = s; _msgAte = Time.unscaledTime + 2.5f; }

    void OnGUI()
    {
        // Dica sempre visível num cantinho.
        var dicaEstilo = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        dicaEstilo.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        GUI.Label(new Rect(10, Screen.height - 22, 320, 20), "F9: painel de spawn de inimigos (debug)", dicaEstilo);

        if (!_visivel) return;

        const float W = 240f;
        float h = Mathf.Min(Screen.height - 40f, 120f + _mobs.Count * 26f);
        GUILayout.BeginArea(new Rect(10, 10, W, h), GUI.skin.box);

        GUILayout.Label("<b>SPAWN DE INIMIGOS</b> (debug)", new GUIStyle(GUI.skin.label) { richText = true });
        string papel = !NetSpawn.EmRede ? "single-player"
                     : NetSpawn.PodeSpawnar ? "HOST" : "CLIENTE (não spawna)";
        GUILayout.Label($"modo: {papel}");

        if (GUILayout.Button("↻ recarregar lista")) CarregarPrefabs();

        _scroll = GUILayout.BeginScrollView(_scroll);
        foreach (var m in _mobs)
            if (GUILayout.Button(m.nome))
                Spawnar(m);
        GUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(_msg) && Time.unscaledTime < _msgAte)
            GUILayout.Label(_msg);

        GUILayout.EndArea();
    }
}
#endif
