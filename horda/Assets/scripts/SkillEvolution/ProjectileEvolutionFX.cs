using System.Collections;
using UnityEngine;

// Espiral Furacão — projétil zigzagueia após sair da órbita
public class ZigzagMoveFX : MonoBehaviour
{
    ProjectileController2D pc;
    float elapsed;

    public void Iniciar(ProjectileController2D controller)
    {
        pc = controller;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // Aguarda projétil ser lançado (sair do modo orbital)
        yield return new WaitForSeconds(0.5f);
        while (pc != null && gameObject != null)
        {
            elapsed += Time.deltaTime;
            // Deslocamento lateral usando seno
            float offset = Mathf.Sin(elapsed * 12f) * 0.6f;
            Vector3 right = transform.right;
            transform.position += right * offset * Time.deltaTime;
            yield return null;
        }
    }
}

// Homing Eterno — relança o projétil 2x adicionais após atingir
public class HomingEternoFX : MonoBehaviour
{
    PassiveProjectileSkill2D behavior;
    ProjectileController2D   pc;
    int relancamentos = 0;

    public void Iniciar(PassiveProjectileSkill2D b, ProjectileController2D controller)
    {
        behavior = b;
        pc       = controller;
        StartCoroutine(Monitorar());
    }

    IEnumerator Monitorar()
    {
        while (pc != null && gameObject != null && relancamentos < 2)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Projétil destruído — relança
        if (relancamentos < 2 && behavior != null)
        {
            relancamentos++;
            yield return new WaitForSeconds(0.3f);
            if (behavior != null)
                behavior.ApplyEffect();
        }
    }
}
