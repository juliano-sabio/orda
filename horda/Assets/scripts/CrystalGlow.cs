using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Tilemap))]
public class CrystalGlow : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(0f, 1f, 0.2f, 1f);
    [SerializeField] private float innerRadius = 0.3f;
    [SerializeField] private float outerRadius = 2.5f;
    [SerializeField] private float intensity = 4f;
    [SerializeField] private float falloffIntensity = 0.9f;

    static readonly Vector3Int[] Neighbors = {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0)
    };

    void OnEnable()  => BuildLights();
    void OnDisable() => ClearLights();

    void BuildLights()
    {
        ClearLights();

        Tilemap tilemap = GetComponent<Tilemap>();
        tilemap.CompressBounds();

        var all = new HashSet<Vector3Int>();
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(cell)) all.Add(cell);

        var visited = new HashSet<Vector3Int>();

        foreach (var start in all)
        {
            if (visited.Contains(start)) continue;

            // flood fill para achar o cluster
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

            // centroid do cluster
            Vector3 centroid = Vector3.zero;
            foreach (var c in cluster)
                centroid += tilemap.GetCellCenterWorld(c);
            centroid /= cluster.Count;

            var go = new GameObject("crystal_glow_light");
            go.transform.SetParent(transform);
            go.transform.position = centroid;

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = glowColor;
            light.intensity = intensity;
            light.pointLightInnerRadius = innerRadius;
            light.pointLightOuterRadius = outerRadius;
            light.falloffIntensity = falloffIntensity;
            light.blendStyleIndex = 0;
        }
    }

    void ClearLights()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != "crystal_glow_light") continue;
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                var captured = child.gameObject;
                UnityEditor.EditorApplication.delayCall += () => { if (captured) DestroyImmediate(captured); };
#endif
            }
        }
    }
}
