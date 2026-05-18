using UnityEngine;

public class MarcadorUltimate : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float pulseSpeed = 2f;
    private float pulseMin = 0.6f;
    private float pulseMax = 1.0f;
    private int segments = 36;
    private float radius = 0.4f;

    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 10;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = mat;

        DrawCircle(radius);
    }

    void Update()
    {
        float alpha = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        lineRenderer.startColor = new Color(0.2f, 0.6f, 1f, alpha);
        lineRenderer.endColor   = new Color(0.2f, 0.6f, 1f, alpha);
    }

    private void DrawCircle(float r)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lineRenderer.SetPosition(i, (Vector3)transform.position + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
        }
    }
}
