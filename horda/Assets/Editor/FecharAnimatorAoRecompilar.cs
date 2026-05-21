using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class FecharAnimatorAoRecompilar
{
    static FecharAnimatorAoRecompilar()
    {
        AssemblyReloadEvents.afterAssemblyReload += Diagnostico;
    }

    static void Diagnostico()
    {
        var sb = new StringBuilder("[FecharAnimator] Janelas abertas apos reload:\n");
        foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            sb.AppendLine($"  - {w.GetType().FullName}");
        Debug.Log(sb.ToString());
    }
}
