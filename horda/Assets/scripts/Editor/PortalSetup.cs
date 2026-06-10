using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PortalSetup
{
    [MenuItem("Tools/Criar Portais Pentagrama")]
    public static void CriarPortais()
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        var p1 = MakePortal("portal_A", new Vector3(-6f, 0f, 0f), mat);
        var p2 = MakePortal("portal_B", new Vector3( 6f, 0f, 0f), mat);
        p1.GetComponent<PortalController>().SetDestination(p2.GetComponent<PortalController>());
        p2.GetComponent<PortalController>().SetDestination(p1.GetComponent<PortalController>());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Portais A e B criados e linkados.");
    }

    [MenuItem("Tools/Criar Portais C e D")]
    public static void CriarPortaisCD()
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        var p3 = MakePortal("portal_C", new Vector3(-6f, -5f, 0f), mat);
        var p4 = MakePortal("portal_D", new Vector3( 6f, -5f, 0f), mat);
        p3.GetComponent<PortalController>().SetDestination(p4.GetComponent<PortalController>());
        p4.GetComponent<PortalController>().SetDestination(p3.GetComponent<PortalController>());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Portais C e D criados e linkados.");
    }

    static Vector3[] PentagramPoints(float r)
    {
        int[] ord = { 0, 2, 4, 1, 3 };
        var pts = new Vector3[6];
        for (int i = 0; i < 5; i++)
        {
            float a = (2f * Mathf.PI / 5f * ord[i]) - Mathf.PI / 2f;
            pts[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }
        pts[5] = pts[0];
        return pts;
    }

    static GameObject MakePortal(string name, Vector3 pos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        // Pentagrama
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.positionCount = 6;
        lr.SetPositions(PentagramPoints(1.2f));
        lr.startWidth = 0.1f;
        lr.endWidth   = 0.1f;
        lr.startColor = new Color(0f, 0.9f, 0.1f);
        lr.endColor   = new Color(0f, 0.6f, 0.05f);
        lr.material = mat;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = 4;

        // Anel externo
        var ring = new GameObject("ring");
        ring.transform.SetParent(go.transform);
        ring.transform.localPosition = Vector3.zero;
        var lr2 = ring.AddComponent<LineRenderer>();
        lr2.useWorldSpace = false;
        lr2.loop = true;
        var cpts = new Vector3[40];
        for (int i = 0; i < 40; i++)
        {
            float a = 2f * Mathf.PI / 40 * i;
            cpts[i] = new Vector3(Mathf.Cos(a) * 1.35f, Mathf.Sin(a) * 1.35f, 0f);
        }
        lr2.positionCount = 40;
        lr2.SetPositions(cpts);
        lr2.startWidth = 0.06f;
        lr2.endWidth   = 0.06f;
        lr2.startColor = lr2.endColor = new Color(0f, 0.9f, 0.1f);
        lr2.material = mat;
        lr2.sortingLayerName = "Default";
        lr2.sortingOrder = 4;

        // Collider trigger
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.4f;

        // Luz verde
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(0f, 0.85f, 0.1f);
        light.intensity = 1.2f;
        light.pointLightInnerRadius = 0.3f;
        light.pointLightOuterRadius = 2.8f;
        light.blendStyleIndex = 0;

        go.AddComponent<PortalController>();
        return go;
    }
}
