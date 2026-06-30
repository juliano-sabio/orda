using UnityEngine;

// Bus central de SFX. Todos os efeitos sonoros tocam por aqui, então o slider "SFX" (PauseManager)
// controla um volume ÚNICO sem mexer no AudioListener (master) — assim música (MusicManager) e SFX
// ficam INDEPENDENTES. Não usa AudioMixer (criar/editar mixer por script é frágil); o controle é um
// fator de volume aplicado em cada playback. Persiste em PlayerPrefs "SFXVolume".
public static class AudioBus
{
    static float _sfx = -1f;

    public static float SfxVolume
    {
        get { if (_sfx < 0f) _sfx = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 0.6f)); return _sfx; }
    }

    public static void SetSfxVolume(float v)
    {
        _sfx = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("SFXVolume", _sfx);
    }

    // Drop-in pros AudioSource.PlayClipAtPoint espalhados — escala pelo volume de SFX.
    public static void PlaySfx(AudioClip clip, Vector3 pos, float vol = 1f)
    {
        if (clip == null) return;
        float v = vol * SfxVolume;
        if (v <= 0.0001f) return;
        AudioSource.PlayClipAtPoint(clip, pos, v);
    }

    // Drop-in pros source.PlayOneShot.
    public static void PlayOn(AudioSource src, AudioClip clip, float vol = 1f)
    {
        if (src == null || clip == null) return;
        float v = vol * SfxVolume;
        if (v <= 0.0001f) return;
        src.PlayOneShot(clip, v);
    }
}
