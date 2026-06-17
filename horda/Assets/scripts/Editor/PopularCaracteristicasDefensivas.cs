using UnityEngine;
using UnityEditor;

public static class PopularCaracteristicasDefensivas
{
    [MenuItem("Tools/Elementos/Popular Caracteristicas Defensivas")]
    public static void Popular()
    {
        var reg = Resources.Load<ElementRegistry>("Elements/ElementRegistry");
        if (reg == null) { Debug.LogError("ElementRegistry não encontrado em Resources/Elements/ElementRegistry"); return; }

        Set(reg, ElementType.Fogo,
            D("Aura Ígnea", DefensiveCharacteristicType.AuraIgnea, DefensiveTrigger.AuraContinua, 2.5f, 4f),
            D("Retaliação em Chamas", DefensiveCharacteristicType.RetaliacaoChamas, DefensiveTrigger.OnAtingido, 5f, 3f));
        Set(reg, ElementType.Ar,
            D("Esquiva Ventosa", DefensiveCharacteristicType.EsquivaVentosa, DefensiveTrigger.OnAtivar, 0.25f, 6f),
            D("Sopro Repulsor", DefensiveCharacteristicType.SoproRepulsor, DefensiveTrigger.OnAtivar, 3f, 12f));
        Set(reg, ElementType.Terra,
            D("Pele de Pedra", DefensiveCharacteristicType.PeleDePedra, DefensiveTrigger.OnAtivar, 0.30f, 6f),
            D("Fundação Firme", DefensiveCharacteristicType.FundacaoFirme, DefensiveTrigger.OnAtivar, 6f, 0f));
        Set(reg, ElementType.Agua,
            D("Maré Restauradora", DefensiveCharacteristicType.MareRestauradora, DefensiveTrigger.OnAtivar, 0.20f, 0f),
            D("Fluxo Vital", DefensiveCharacteristicType.FluxoVital, DefensiveTrigger.AuraContinua, 3f, 0f));
        Set(reg, ElementType.Raio,
            D("Descarga Reativa", DefensiveCharacteristicType.DescargaReativa, DefensiveTrigger.OnAtingido, 1f, 0.5f),
            D("Corrente Reflexiva", DefensiveCharacteristicType.CorrenteReflexiva, DefensiveTrigger.OnAtingido, 4f, 10f));
        Set(reg, ElementType.Gelo,
            D("Armadura Gélida", DefensiveCharacteristicType.ArmaduraGelida, DefensiveTrigger.OnAtivar, 40f, 0f),
            D("Toque Congelante", DefensiveCharacteristicType.ToqueCongelante, DefensiveTrigger.OnAtingido, 2f, 0f));
        Set(reg, ElementType.Planta,
            D("Espinhos", DefensiveCharacteristicType.Espinhos, DefensiveTrigger.OnAtingido, 12f, 0f),
            D("Raízes Protetoras", DefensiveCharacteristicType.RaizesProtetoras, DefensiveTrigger.OnAtivar, 3f, 2.5f));
        Set(reg, ElementType.Trevas,
            D("Drenagem Sombria", DefensiveCharacteristicType.DrenagemSombria, DefensiveTrigger.OnAtingido, 8f, 0f),
            D("Manto Amaldiçoado", DefensiveCharacteristicType.MantoAmaldicoado, DefensiveTrigger.AuraContinua, 3.5f, 0.3f));
        Set(reg, ElementType.Luz,
            D("Bênção Sagrada", DefensiveCharacteristicType.BencaoSagrada, DefensiveTrigger.OnAtivar, 15f, 25f),
            D("Luz Ofuscante", DefensiveCharacteristicType.LuzOfuscante, DefensiveTrigger.AuraContinua, 3f, 0.4f));
        Set(reg, ElementType.Corrompido,
            D("Caos Defensivo", DefensiveCharacteristicType.CaosDefensivo, DefensiveTrigger.OnAtivar, 0f, 0f),
            D("Praga Reativa", DefensiveCharacteristicType.PragaReativa, DefensiveTrigger.OnAtingido, 3.5f, 8f));

        EditorUtility.SetDirty(reg);
        AssetDatabase.SaveAssets();
        Debug.Log("Caracteristicas defensivas populadas no ElementRegistry.");
    }

    static DefensiveCharacteristic D(string nome, DefensiveCharacteristicType tipo, DefensiveTrigger g, float v1, float v2)
        => new DefensiveCharacteristic { nome = nome, descricao = nome, tipo = tipo, gatilho = g, valor1 = v1, valor2 = v2 };

    static void Set(ElementRegistry reg, ElementType el, DefensiveCharacteristic a, DefensiveCharacteristic b)
    {
        var def = reg.Get(el);
        if (def == null) { Debug.LogWarning("ElementDefinition ausente: " + el); return; }
        def.caracteristicasDefensivas = new[] { a, b };
    }
}
