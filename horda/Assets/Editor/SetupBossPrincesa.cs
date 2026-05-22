using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Rendering.Universal;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public static class SetupBossPrincesa
{
    const string PASTA = "Assets/prefebs/boss/bossprincesa";

    [MenuItem("Tools/Setup Boss Princesa")]
    static void Setup()
    {
        var paradaClip   = CriarClip("princesa slime parada", "parada_princesa",   loopTime: true);
        var voandoClip   = CriarClip("princesaslimevoando",   "voando_princesa",   loopTime: true);
        var atacandoClip = CriarClip("princesa slime ataque", "atacando_princesa", loopTime: false);

        AtualizarController(paradaClip, voandoClip, atacandoClip);

        var sprite = CriarSpriteProjetil();
        AtualizarPrefabProjetil(sprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Boss Princesa: setup concluido!");
    }

    // ─────────────────────────────────────────────────
    // ANIMAÇÕES
    // ─────────────────────────────────────────────────

    static AnimationClip CriarClip(string nomeAse, string nomeClip, bool loopTime)
    {
        string asePath  = $"{PASTA}/{nomeAse}.ase";
        string clipPath = $"{PASTA}/{nomeClip}.anim";

        var sprites = AssetDatabase.LoadAllAssetsAtPath(asePath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"Nenhum sprite encontrado em: {asePath}");
            return null;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = 8f;

        var binding   = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keyframes = sprites.Select((s, i) => new ObjectReferenceKeyframe
        {
            time  = i / 8f,
            value = s
        }).ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings      = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loopTime;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        Debug.Log($"{nomeClip}.anim criado com {sprites.Length} frames (loop={loopTime}).");
        return clip;
    }

    static void AtualizarController(AnimationClip parada, AnimationClip voando, AnimationClip atacando)
    {
        string ctrlPath = $"{PASTA}/bossprincesa.controller";

        // Recria o controller via API para garantir YAML 100% gerado pelo Unity
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        // Parâmetros
        ctrl.AddParameter("voando",        AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("prepararAtaque", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("canalizar",      AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        // Estados
        var sParada   = sm.AddState("parada");
        var sVoando   = sm.AddState("voando");
        var sAtacando = sm.AddState("atacando");

        sParada.motion   = parada;
        sVoando.motion   = voando;
        sAtacando.motion = atacando;

        sm.defaultState = sParada;

        // parada → voando (voando = true)
        var t1 = sParada.AddTransition(sVoando);
        t1.AddCondition(AnimatorConditionMode.If, 0, "voando");
        t1.duration = 0.05f;
        t1.hasExitTime = false;

        // voando → parada (voando = false)
        var t2 = sVoando.AddTransition(sParada);
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "voando");
        t2.duration = 0.05f;
        t2.hasExitTime = false;

        // parada → atacando (prepararAtaque)
        var t3 = sParada.AddTransition(sAtacando);
        t3.AddCondition(AnimatorConditionMode.If, 0, "prepararAtaque");
        t3.duration = 0.05f;
        t3.hasExitTime = false;

        // parada → atacando (canalizar)
        var t4 = sParada.AddTransition(sAtacando);
        t4.AddCondition(AnimatorConditionMode.If, 0, "canalizar");
        t4.duration = 0.05f;
        t4.hasExitTime = false;

        // atacando → parada (exit time 90%)
        var t5 = sAtacando.AddTransition(sParada);
        t5.hasExitTime = true;
        t5.exitTime    = 0.9f;
        t5.duration    = 0.05f;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("Controller recriado via API (sem erros de YAML manual).");
    }

    // ─────────────────────────────────────────────────
    // SPRITE DO PROJETIL
    // ─────────────────────────────────────────────────

    static Sprite CriarSpriteProjetil()
    {
        string spritePath = $"{PASTA}/projetil_princesa.png";
        string absPath    = Application.dataPath + spritePath.Substring("Assets".Length);

        const int TAM = 16;
        float cx = (TAM - 1) / 2f;
        float cy = (TAM - 1) / 2f;

        var tex = new Texture2D(TAM, TAM, TextureFormat.RGBA32, false);

        for (int y = 0; y < TAM; y++)
        {
            for (int x = 0; x < TAM; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float t    = Mathf.Clamp01(dist / (TAM / 2f));

                if (t >= 1f) { tex.SetPixel(x, y, Color.clear); continue; }

                // centro rosa-roxo brilhante → borda roxo escuro
                Color centro = new Color(1f,    0.35f, 1f);
                Color borda  = new Color(0.35f, 0f,    0.6f);
                Color c      = Color.Lerp(centro, borda, t * t);
                c.a          = 1f - Mathf.Pow(t, 0.55f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        File.WriteAllBytes(absPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(spritePath);

        // Configura como sprite pixel art
        var ti = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Single;
            ti.filterMode          = FilterMode.Point;
            ti.spritePixelsPerUnit = 16;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        Debug.Log("Sprite do projetil criado: " + spritePath);
        return sprite;
    }

    static void AtualizarPrefabProjetil(Sprite sprite)
    {
        if (sprite == null) return;

        string prefabPath = $"{PASTA}/ProjetilPrincesa.prefab";

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var sr = scope.prefabContentsRoot.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
            sr.color  = Color.white;
        }
        Debug.Log("ProjetilPrincesa.prefab atualizado com o sprite.");
    }

    // ─────────────────────────────────────────────────
    // PROJÉTEIS ESPECIAIS
    // ─────────────────────────────────────────────────

    [MenuItem("Tools/Setup Projeteis Especiais Princesa")]
    static void SetupProjeteis()
    {
        var configs = new List<(string nomeAse, string nomeClip, string nomePrefab, Color cor)>
        {
            ("Projetilprincesa",          "anim_projetil_princesa",  "ProjetilPrincesa",          Color.white),
            ("projetilespecialraiz",      "anim_especial_raiz",      "ProjetilEspecialRaiz",      new Color(0.3f, 1f,    0.3f)),
            ("projetilespecialqueima",    "anim_especial_queima",    "ProjetilEspecialQueima",    new Color(1f,   0.4f,  0.1f)),
            ("projetilespecialempurrar",  "anim_especial_empurrao",  "ProjetilEspecialEmpurrao",  new Color(0.2f, 0.85f, 1f)),
        };

        foreach (var (nomeAse, nomeClip, nomePrefab, cor) in configs)
        {
            var clip  = CriarClip(nomeAse, nomeClip, loopTime: true);
            if (clip == null) continue;

            var ctrl  = CriarControllerSimples(nomeClip, clip);
            CriarPrefabProjetilEspecial(nomePrefab, ctrl, cor);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefabs dos projeteis especiais criados!");
    }

    static AnimatorController CriarControllerSimples(string nome, AnimationClip clip)
    {
        string ctrlPath = $"{PASTA}/{nome}.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
            AssetDatabase.DeleteAsset(ctrlPath);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var sm   = ctrl.layers[0].stateMachine;
        var st   = sm.AddState("idle");
        st.motion = clip;
        sm.defaultState = st;

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static void CriarPrefabProjetilEspecial(string nome, AnimatorController ctrl, Color cor)
    {
        string prefabPath = $"{PASTA}/{nome}.prefab";

        var go = new GameObject(nome);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.color = cor;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale  = 0f;
        rb.linearDamping = 0f;
        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.25f;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        // Script de homing só no projétil principal (sem "Especial" no nome)
        bool ehEspecial = nome.Contains("Especial");
        if (!ehEspecial)
            go.AddComponent<ProjetilHomingPrincesa>();

        // Luz 2D nos projéteis especiais
        if (ehEspecial)
        {
            var luzGO = new GameObject("Luz");
            luzGO.transform.SetParent(go.transform, false);

            var luz = luzGO.AddComponent<Light2D>();
            luz.lightType        = Light2D.LightType.Point;
            luz.color            = cor;
            luz.intensity        = 1.8f;
            luz.pointLightInnerRadius = 0f;
            luz.pointLightOuterRadius = 1.5f;
            luz.shadowsEnabled   = false;
        }

        // Sprite inicial = primeiro frame do clip
        if (ctrl.animationClips.Length > 0)
        {
            var curves = AnimationUtility.GetObjectReferenceCurveBindings(ctrl.animationClips[0]);
            if (curves.Length > 0)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(ctrl.animationClips[0], curves[0]);
                if (keys.Length > 0)
                    sr.sprite = keys[0].value as Sprite;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"{nome}.prefab criado/atualizado.");
    }
}
