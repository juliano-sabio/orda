using UnityEngine;
using System.Collections;

public class ShieldAuraBehavior : SkillBehavior
{
    [Header("🛡️ Configurações da Auréola")]
    public float cooldownTime = 10f;
    public GameObject visualAuraSustentada;

    [Header("💥 Efeitos de Pixel Art")]
    public GameObject objetoAnimaçãoQuebra;
    public float duraçãoAnimaçãoQuebra = 0.5f;
    public AudioClip somQuebra;

    [Header("📐 Ajustes de Transform (AO VIVO)")]
    public Vector3 escalaDesejada = new Vector3(1, 1, 1);
    // 💡 AGORA VOCÊ PODE MUDAR A ALTURA AQUI! 
    // Tente colocar 1.2 no campo Y no Inspector do Unity
    public Vector3 offsetPosicao = new Vector3(0, 1.2f, 0);

    private bool isShieldActive = true;
    private bool isOnCooldown = false;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);

        ResetarTransform();

        if (visualAuraSustentada != null) visualAuraSustentada.SetActive(true);
        if (objetoAnimaçãoQuebra != null) objetoAnimaçãoQuebra.SetActive(false);
        isShieldActive = true;
    }

    // O LateUpdate garante que ela siga o player mesmo com interpolação de física
    void LateUpdate()
    {
        ResetarTransform();
    }

    private void ResetarTransform()
    {
        // 🚀 CORREÇÃO: Agora ele usa o offset em vez de forçar o Zero absoluto
        if (transform.localPosition != offsetPosicao)
            transform.localPosition = offsetPosicao;

        if (transform.localScale != escalaDesejada)
            transform.localScale = escalaDesejada;

        transform.localRotation = Quaternion.identity;
    }

    public override void ApplyEffect() { }

    public bool TryBlockDamage()
    {
        if (isShieldActive && !isOnCooldown)
        {
            BreakShield();
            return true;
        }
        return false;
    }

    private void BreakShield()
    {
        isShieldActive = false;
        isOnCooldown = true;

        if (visualAuraSustentada != null) visualAuraSustentada.SetActive(false);
        if (somQuebra != null) AudioSource.PlayClipAtPoint(somQuebra, transform.position);

        if (objetoAnimaçãoQuebra != null)
            StartCoroutine(GerenciarAnimaçãoQuebra());
        else
            StartCoroutine(CooldownRoutine());
    }

    private IEnumerator GerenciarAnimaçãoQuebra()
    {
        objetoAnimaçãoQuebra.SetActive(true);
        yield return new WaitForSeconds(duraçãoAnimaçãoQuebra);
        objetoAnimaçãoQuebra.SetActive(false);
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        float tempoRestante = Mathf.Max(0.1f, cooldownTime - duraçãoAnimaçãoQuebra);
        yield return new WaitForSeconds(tempoRestante);

        isShieldActive = true;
        isOnCooldown = false;

        if (visualAuraSustentada != null) visualAuraSustentada.SetActive(true);
    }
}