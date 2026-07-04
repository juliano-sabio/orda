using System.Collections.Generic;
using UnityEngine;

// Cache de sprites procedurais (VFX). Os geradores de partícula (GerarDisco/GerarSprite/
// GerarCristal/GerarAnel/...) criavam um Texture2D NOVO a cada partícula via Sprite.Create e
// NUNCA liberavam → milhares de texturas órfãs por run → memória sobe, GC bufa, FPS cai (e
// "coisas bugadas" carregam pra próxima run). Aqui memoizamos por (tag, tamanho, cor
// quantizada): a mesma forma/cor reusa o MESMO sprite → zero vazamento, visual idêntico.
//
// Uso (preserve a fórmula original num método *Raw e delegue):
//   static Sprite GerarDisco(int sz, Color cor) => FXTexCache.Obter("FantasmaFogo.Disco", sz, cor, GerarDiscoRaw);
//   static Sprite GerarDiscoRaw(int sz, Color cor) { ...fórmula original intacta... }
//
//   static Sprite GerarDisco(int sz) => FXTexCache.Obter("Aureola.Disco", sz, GerarDiscoRaw);
//   static Sprite GerarEspada()      => FXTexCache.Obter("Espada.Espada", GerarEspadaRaw);
//
// O `tag` deve ser ÚNICO por (classe, forma) — use "NomeDaClasse.Forma". Fórmulas diferentes
// com o mesmo tag colidiriam (retornariam o sprite errado).
public static class FXTexCache
{
    static readonly Dictionary<long, Sprite> _cache = new Dictionary<long, Sprite>();

    // Gerador com (tamanho, cor).
    public static Sprite Obter(string tag, int tamanho, Color cor, System.Func<int, Color, Sprite> gerar)
    {
        long chave = Chave(tag, tamanho, QuantCor(cor));
        if (_cache.TryGetValue(chave, out var s) && s != null) return s;
        s = gerar(tamanho, cor);
        _cache[chave] = s;
        return s;
    }

    // Gerador só com (tamanho) — cor fixa embutida.
    public static Sprite Obter(string tag, int tamanho, System.Func<int, Sprite> gerar)
    {
        long chave = Chave(tag, tamanho, 0);
        if (_cache.TryGetValue(chave, out var s) && s != null) return s;
        s = gerar(tamanho);
        _cache[chave] = s;
        return s;
    }

    // Gerador sem parâmetros — forma/cor fixas.
    public static Sprite Obter(string tag, System.Func<Sprite> gerar)
    {
        long chave = Chave(tag, 0, 0);
        if (_cache.TryGetValue(chave, out var s) && s != null) return s;
        s = gerar();
        _cache[chave] = s;
        return s;
    }

    static long Chave(string tag, int tamanho, int corQuant)
    {
        long h = tag != null ? tag.GetHashCode() & 0xFFFFFFFFL : 0L;
        return (h << 24) ^ ((long)(tamanho & 0xFFF) << 12) ^ (corQuant & 0xFFF);
    }

    // Quantiza a cor em 16 níveis por canal — colapsa variações mínimas (ex.: trilhas com
    // Random.Range no verde/laranja) em poucos buckets, mantendo o visual praticamente igual.
    static int QuantCor(Color c)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 15f), 0, 15);
        int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 15f), 0, 15);
        int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 15f), 0, 15);
        int a = Mathf.Clamp(Mathf.RoundToInt(c.a * 15f), 0, 15);
        return (r << 12) | (g << 8) | (b << 4) | a;
    }
}
