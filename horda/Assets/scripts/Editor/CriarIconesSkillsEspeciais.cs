#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Cria ícones para EscudoEspinhoso, Aureola e BarreiraReflexiva
/// e os atribui nos respectivos SkillData assets.
/// Menu: Tools/Skills/Criar Icones Skills Especiais
/// </summary>
public static class CriarIconesSkillsEspeciais
{
    const int SZ = 64;

    static readonly (string assetPath, string iconPath, Color cor, string forma)[] SKILLS = {
        (
            "Assets/scripts/scriptables_object/skills/EscudoEspinhoso.asset",
            "Assets/Skills/EscudoEspinhosoIcon.png",
            new Color(0.25f, 0.90f, 0.30f), "escudoespinho"
        ),
        (
            "Assets/scripts/scriptables_object/skills/aureola.asset",
            "Assets/Skills/AureolaIcon.png",
            new Color(1.00f, 0.88f, 0.22f), "aureola"
        ),
        (
            "Assets/scripts/scriptables_object/skills/barreira_reflexiva.asset",
            "Assets/Skills/BarreiraReflexivaIcon.png",
            new Color(0.35f, 0.88f, 1.00f), "reflexiva"
        ),
    };

    [MenuItem("Tools/Skills/Criar Icones Skills Especiais")]
    public static void Criar()
    {
        int ok = 0;
        foreach (var (assetPath, iconPath, cor, forma) in SKILLS)
        {
            var sprite = GerarIcone(iconPath, cor, forma);
            if (sprite == null) continue;

            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (skill != null)
            {
                skill.icon = sprite;
                EditorUtility.SetDirty(skill);
            }
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ {ok} ícones criados e atribuídos!");
    }

    static Sprite GerarIcone(string iconPath, Color cor, string forma)
    {
        if (File.Exists(Path.Combine(Application.dataPath, "../" + iconPath)))
        { AssetDatabase.DeleteAsset(iconPath); AssetDatabase.Refresh(); }

        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        GerarFundo(pixels, SZ, cx, cor);

        switch (forma)
        {
            case "escudoespinho": DesenharEscudoEspinho(pixels, SZ, cx, cor); break;
            case "aureola":       DesenharAureola(pixels, SZ, cx, cor);       break;
            case "reflexiva":     DesenharReflexiva(pixels, SZ, cx, cor);     break;
        }

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels); tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + iconPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = SZ;
            imp.filterMode = FilterMode.Bilinear; imp.alphaIsTransparency = true;
            imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    // ── Fundo ────────────────────────────────────────────────────────────────

    static void GerarFundo(Color[] p, int sz, float cx, Color cor)
    {
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float dx = x - cx, dy = y - cx;
            float d = Mathf.Sqrt(dx*dx + dy*dy), t = Mathf.Clamp01(d/cx);
            float a = Mathf.Clamp01(1f - d/(cx*1.01f));
            if (a <= 0f) { p[y*sz+x] = Color.clear; continue; }
            Color c0 = new Color(cor.r*0.55f, cor.g*0.55f, cor.b*0.65f);
            Color c1 = new Color(cor.r*0.25f, cor.g*0.25f, cor.b*0.32f);
            Color c2 = new Color(cor.r*0.08f, cor.g*0.08f, cor.b*0.13f);
            Color bg = t < 0.45f ? Color.Lerp(c0,c1,t/0.45f) : Color.Lerp(c1,c2,(t-0.45f)/0.55f);
            float sheen = Mathf.Clamp01(1f-(dx+1.5f*dy+sz*0.8f)/(sz*1.2f))*0.13f;
            bg.r=Mathf.Clamp01(bg.r+sheen); bg.g=Mathf.Clamp01(bg.g+sheen); bg.b=Mathf.Clamp01(bg.b+sheen*1.2f);
            float rim = Mathf.Clamp01((t-0.80f)/0.20f);
            bg = Color.Lerp(bg, new Color(cor.r*0.55f,cor.g*0.55f,cor.b*0.75f), rim*0.55f);
            p[y*sz+x] = new Color(bg.r,bg.g,bg.b,a);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static void Px(Color[] p, int sz, int x, int y, Color c, float f=1f)
    { if(x<0||x>=sz||y<0||y>=sz) return; p[y*sz+x]=Color.Lerp(p[y*sz+x],c,Mathf.Clamp01(f)); }

    static void Linha(Color[] p, int sz, Vector2 de, Vector2 ate, Color cor, float esp=2f)
    {
        int steps = Mathf.Max(1,(int)(Vector2.Distance(de,ate)*2f));
        for(int i=0;i<=steps;i++){
            Vector2 pt=Vector2.Lerp(de,ate,i/(float)steps); int r=Mathf.CeilToInt(esp);
            for(int dy=-r;dy<=r;dy++) for(int dx=-r;dx<=r;dx++)
                Px(p,sz,(int)pt.x+dx,(int)pt.y+dy,cor,Mathf.Clamp01(esp-Mathf.Sqrt(dx*dx+dy*dy)));
        }
    }

    static void Anel(Color[] p, int sz, Vector2 c, float r, Color cor, float esp=1.5f)
    {
        int segs=Mathf.Max(24,(int)(r*Mathf.PI*2));
        for(int i=0;i<segs;i++){
            float a=i/(float)segs*Mathf.PI*2f; Vector2 pt=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
            int ri=Mathf.CeilToInt(esp);
            for(int dy=-ri;dy<=ri;dy++) for(int dx=-ri;dx<=ri;dx++)
                Px(p,sz,(int)pt.x+dx,(int)pt.y+dy,cor,Mathf.Clamp01(esp-Mathf.Sqrt(dx*dx+dy*dy)));
        }
    }

    static void Elipse(Color[] p, int sz, float ex, float ey, float rx, float ry, Color cor, float borda=2f)
    {
        for(int y=Mathf.Max(0,(int)(ey-ry-2));y<=Mathf.Min(sz-1,(int)(ey+ry+2));y++)
        for(int x=Mathf.Max(0,(int)(ex-rx-2));x<=Mathf.Min(sz-1,(int)(ex+rx+2));x++){
            float d=Mathf.Sqrt(Mathf.Pow((x-ex)/rx,2)+Mathf.Pow((y-ey)/ry,2));
            float f=Mathf.Clamp01((1f-d)*borda);
            if(f>0f){float hl=Mathf.Clamp01(1f+(ex-x+ey-y)/(rx*4f))*0.3f; Px(p,sz,x,y,Color.Lerp(cor,Color.white,hl),f);}
        }
    }

    // ── Formas ───────────────────────────────────────────────────────────────

    static void DesenharEscudoEspinho(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Color espinho = Color.Lerp(cor, Color.white, 0.8f);
        int cy = (int)cx;

        for (int y = cy-20; y <= cy+18; y++) for (int x = (int)cx-16; x <= (int)cx+16; x++)
        {
            if (x<0||x>=sz||y<0||y>=sz) continue;
            float nx=Mathf.Abs(x-cx)/16f, ny=(y-cy)/20f;
            float larg=ny<-0.4f?(1f+ny*2.5f):ny>0.6f?((1f-ny)/0.4f):1f;
            float d=nx/Mathf.Max(larg,0.01f);
            if(d<=1f){ float b=Mathf.Clamp01(1f-d)*Mathf.Lerp(1f,0.45f,(y-cy+20f)/40f); Px(p,sz,x,y,Color.Lerp(cor,brilho,b*0.5f),Mathf.Clamp01(b*1.5f)); }
        }

        float[] angulos = {-90f,-30f,30f,90f,150f,210f};
        foreach (float angDeg in angulos)
        {
            float ang = angDeg*Mathf.Deg2Rad;
            Vector2 base1 = new Vector2(cx+Mathf.Cos(ang)*14f, cy+Mathf.Sin(ang)*17f);
            Vector2 ponta = new Vector2(cx+Mathf.Cos(ang)*23f, cy+Mathf.Sin(ang)*27f);
            Linha(p,sz,base1,ponta,espinho,1.8f);
            Elipse(p,sz,ponta.x,ponta.y,1.5f,1.5f,Color.white,3f);
        }
        Linha(p,sz,new Vector2(cx-6,cy-2),new Vector2(cx+6,cy-2),Color.white,1.3f);
        Linha(p,sz,new Vector2(cx,cy-8),  new Vector2(cx,cy+6),  Color.white,1.3f);
    }

    static void DesenharAureola(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Vector2 centro = new Vector2(cx, cx);
        Anel(p,sz,centro,21f,brilho,3.5f);
        Anel(p,sz,centro,14f,new Color(brilho.r,brilho.g,brilho.b,0.55f),1.5f);
        for(int i=0;i<8;i++){
            float ang=i/8f*Mathf.PI*2f;
            Vector2 inner=centro+new Vector2(Mathf.Cos(ang),Mathf.Sin(ang))*21f;
            Vector2 outer=centro+new Vector2(Mathf.Cos(ang),Mathf.Sin(ang))*28f;
            Linha(p,sz,inner,outer,brilho,i%2==0?2f:1f);
        }
        Elipse(p,sz,cx,cx+3,3.5f,4.5f,new Color(brilho.r,brilho.g,brilho.b,0.6f),2f);
        Elipse(p,sz,cx,cx+10,2.8f,2.8f,new Color(brilho.r,brilho.g,brilho.b,0.6f),2f);
        Elipse(p,sz,cx,cx,4f,4f,brilho,2.5f);
        Elipse(p,sz,cx,cx,2f,2f,Color.white,4f);
    }

    static void DesenharReflexiva(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.70f);
        Vector2 centro = new Vector2(cx,cx);
        for(int i=0;i<6;i++){
            float a1=(i/6f+0.0833f)*Mathf.PI*2f, a2=((i+1)/6f+0.0833f)*Mathf.PI*2f;
            Linha(p,sz,centro+new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*21f, centro+new Vector2(Mathf.Cos(a2),Mathf.Sin(a2))*21f, brilho,2.8f);
        }
        for(int i=0;i<6;i++){
            float a1=(i/6f+0.0833f)*Mathf.PI*2f, a2=((i+1)/6f+0.0833f)*Mathf.PI*2f;
            Linha(p,sz,centro+new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*12f, centro+new Vector2(Mathf.Cos(a2),Mathf.Sin(a2))*12f, new Color(brilho.r,brilho.g,brilho.b,0.5f),1.5f);
        }
        Linha(p,sz,new Vector2(cx-18,cx-18),new Vector2(cx+5,cx+5),brilho,2.5f);
        Linha(p,sz,new Vector2(cx+5,cx+5),new Vector2(cx+18,cx-10),brilho,2.5f);
        Linha(p,sz,new Vector2(cx-17,cx-17),new Vector2(cx+18,cx-10),Color.white,0.9f);
        Elipse(p,sz,cx+5,cx+5,3.5f,3.5f,Color.white,3.5f);
        for(int i=0;i<6;i+=2){
            float a=(i/6f+0.0833f)*Mathf.PI*2f;
            Vector2 v=centro+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*21f;
            Elipse(p,sz,v.x,v.y,2f,2f,brilho,3f);
        }
    }
}
#endif
