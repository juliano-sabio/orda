using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

// Cria partículas flutuantes (poeira de luz) sobre cada grupo de árvores na segunda fase
public class ArvoreParticulasManager : MonoBehaviour
{
    static readonly Vector3Int[] Neighbors = {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0)
    };

    static ArvoreParticulasManager _instance;
    readonly List<GameObject> particulasGO = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_instance != null) return;
        var go = new GameObject("ArvoreParticulasManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ArvoreParticulasManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
        AtualizarCena(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AtualizarCena(scene);

    void AtualizarCena(Scene scene)
    {
        foreach (var go in particulasGO) if (go != null) Destroy(go);
        particulasGO.Clear();

        if (scene.name != "segunda_fase") return;

        ProcessarTilemap("arvore", null);
        ProcessarTilemap("maosverde", new Color(0.35f, 0.95f, 0.35f));
        ProcessarTilemap("maosvermeslhas", new Color(0.95f, 0.25f, 0.25f));
    }

    void ProcessarTilemap(string nomeObjeto, Color? corFixa)
    {
        var alvoGO = GameObject.Find(nomeObjeto) ?? EncontrarPorNome(nomeObjeto);
        if (alvoGO == null) return;

        var tilemap = alvoGO.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            ProcessarSprite(alvoGO, corFixa);
            return;
        }

        tilemap.CompressBounds();

        var all = new HashSet<Vector3Int>();
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(cell)) all.Add(cell);

        var visited = new HashSet<Vector3Int>();

        foreach (var start in all)
        {
            if (visited.Contains(start)) continue;

            // flood fill para achar o cluster de tiles conectados
            var cluster = new List<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                cluster.Add(cur);
                foreach (var dir in Neighbors)
                {
                    var nb = cur + dir;
                    if (all.Contains(nb) && !visited.Contains(nb))
                    {
                        visited.Add(nb);
                        queue.Enqueue(nb);
                    }
                }
            }

            Vector3 centroid = Vector3.zero;
            foreach (var c in cluster)
                centroid += tilemap.GetCellCenterWorld(c);
            centroid /= cluster.Count;

            float largura = Mathf.Sqrt(cluster.Count) * tilemap.cellSize.x;
            Color cor = corFixa ?? ObterCorMedia(tilemap, start);
            CriarParticulas(centroid, Mathf.Max(largura, 1.5f), cor);
        }
    }

    // Para objetos sem Tilemap (ex: sprite único): usa os bounds do(s) SpriteRenderer
    void ProcessarSprite(GameObject alvoGO, Color? corFixa)
    {
        var renderers = alvoGO.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float largura = Mathf.Max(bounds.size.x, bounds.size.y, 1.5f);
        Color cor = corFixa ?? new Color(0.95f, 0.25f, 0.25f);
        CriarParticulas(bounds.center, largura, cor);
    }

    // Busca por nome ignorando maiúsculas/minúsculas, incluindo objetos inativos
    static GameObject EncontrarPorNome(string nome)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var achado = BuscarFilho(root.transform, nome);
            if (achado != null) return achado.gameObject;
        }
        return null;
    }

    static Transform BuscarFilho(Transform pai, string nome)
    {
        if (string.Equals(pai.name, nome, System.StringComparison.OrdinalIgnoreCase))
            return pai;

        for (int i = 0; i < pai.childCount; i++)
        {
            var achado = BuscarFilho(pai.GetChild(i), nome);
            if (achado != null) return achado;
        }
        return null;
    }

    // Cor base da árvore: amostra a sprite do tile para tingir as partículas
    static Color ObterCorMedia(Tilemap tilemap, Vector3Int cell)
    {
        var fallback = new Color(0.55f, 0.62f, 0.22f); // verde-musgo padrão (distinto do verde da maosverde)

        var sprite = tilemap.GetSprite(cell);
        if (sprite == null || sprite.texture == null || !sprite.texture.isReadable)
            return fallback;

        try
        {
            var rect = sprite.textureRect;
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            int w = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int h = Mathf.Max(1, Mathf.RoundToInt(rect.height));

            var pixels = sprite.texture.GetPixels(x, y, w, h);
            float r = 0f, g = 0f, b = 0f;
            int total = 0;
            foreach (var p in pixels)
            {
                if (p.a < 0.1f) continue; // ignora pixels transparentes
                r += p.r; g += p.g; b += p.b;
                total++;
            }
            if (total == 0) return fallback;
            return new Color(r / total, g / total, b / total);
        }
        catch
        {
            return fallback;
        }
    }

    void CriarParticulas(Vector3 centro, float largura, Color cor)
    {
        var go = new GameObject("ParticulasArvore");
        go.transform.position = centro;
        particulasGO.Add(go);

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = true;
        main.playOnAwake      = true;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
        main.startColor       = new Color(cor.r, cor.g, cor.b, 0.9f);
        main.maxParticles     = 50;

        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(largura, largura * 0.6f, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.3f;
        noise.frequency   = 0.4f;
        noise.scrollSpeed = 0.3f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Color corClara = Color.Lerp(cor, Color.white, 0.4f);
        var gradiente = new Gradient();
        gradiente.SetKeys(
            new[] {
                new GradientColorKey(cor, 0f),
                new GradientColorKey(corClara, 0.5f),
                new GradientColorKey(cor, 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = gradiente;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 5;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) renderer.material = new Material(shader);

        ps.Play();
    }
}
