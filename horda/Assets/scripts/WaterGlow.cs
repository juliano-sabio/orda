using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Tilemap))]
public class WaterGlow : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(0f, 0.85f, 0.3f, 1f);
    [SerializeField] private float baseIntensity = 0.35f;
    [SerializeField] private float pulseAmplitude = 0.15f;
    [SerializeField] private float pulseSpeed = 0.8f;
    [SerializeField] private float outerRadius = 2f;
    [SerializeField] private float innerRadius = 0.2f;
    [SerializeField] private float gridSpacing = 6f;

    private readonly List<(Light2D light, float phase)> _lights = new List<(Light2D, float)>();

    void OnEnable()    => BuildLights();
    void OnDisable()   => ClearLights();
    void OnValidate()  => BuildLights();

    void Update()
    {
        if (!Application.isPlaying) return;

        float t = Time.time * pulseSpeed;
        foreach (var (light, phase) in _lights)
            light.intensity = baseIntensity + pulseAmplitude * Mathf.Sin(t + phase);
    }

    void BuildLights()
    {
        ClearLights();

        Tilemap tilemap = GetComponent<Tilemap>();
        tilemap.CompressBounds();

        var tiles = new HashSet<Vector3Int>();
        foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(cell)) tiles.Add(cell);

        if (tiles.Count == 0) return;

        BoundsInt bounds = tilemap.cellBounds;
        Vector3 min = tilemap.CellToWorld(bounds.min);
        Vector3 max = tilemap.CellToWorld(bounds.max);

        float spanX = max.x - min.x;
        float spanY = max.y - min.y;
        int cols = Mathf.Max(1, Mathf.RoundToInt(spanX / gridSpacing));
        int rows = Mathf.Max(1, Mathf.RoundToInt(spanY / gridSpacing));

        for (int r = 0; r <= rows; r++)
        {
            for (int c = 0; c <= cols; c++)
            {
                Vector3 worldPos = new Vector3(
                    min.x + c * (spanX / cols),
                    min.y + r * (spanY / rows),
                    0f
                );

                if (!tiles.Contains(tilemap.WorldToCell(worldPos))) continue;

                var go = new GameObject("water_glow_light");
                go.transform.SetParent(transform);
                go.transform.position = worldPos;

                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = glowColor;
                light.intensity = baseIntensity;
                light.pointLightInnerRadius = innerRadius;
                light.pointLightOuterRadius = outerRadius;
                light.blendStyleIndex = 0;

                // fase deslocada por posição para criar efeito de onda
                float phase = (c * 1.3f + r * 2.1f) % (2f * Mathf.PI);
                _lights.Add((light, phase));
            }
        }
    }

    void ClearLights()
    {
        _lights.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != "water_glow_light") continue;
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
