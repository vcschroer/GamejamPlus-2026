
using System.Collections;
using UnityEngine;

public class BossEffects : MonoBehaviour
{
    [Header("Referência da Sprite")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Configurações de Squash & Stretch")]
    [SerializeField] private float squashDuration = 0.1f;

    [SerializeField] private Vector2 attackScale = new Vector2(1.3f, 0.7f);
    [SerializeField] private Vector2 dashScale = new Vector2(0.7f, 1.3f);
    [SerializeField] private Vector2 impactScale = new Vector2(1.3f, 0.7f);
    [SerializeField] private Vector2 damageScale = new Vector2(0.8f, 1.2f);

    private Vector3 originalScale;
    private Coroutine currentRoutine;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (spriteTransform == null)
            spriteTransform = transform;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        originalScale = spriteTransform.localScale;
    }

    public void TriggerAttack()
    {
        PlayEffect(attackScale);
    }

    public void TriggerDash()
    {
        PlayEffect(dashScale);
    }

    public void TriggerImpact()
    {
        PlayEffect(impactScale);
    }

    public void TriggerDamage()
    {
        PlayEffect(damageScale);
        TriggerFlashOpacity();
    }

    public void TriggerFlashOpacity()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private void PlayEffect(Vector2 targetScale)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(SquashRoutine(targetScale));
    }

    private IEnumerator SquashRoutine(Vector2 targetScale)
    {
        float timer = 0f;

        float xSign = Mathf.Sign(originalScale.x);
        float ySign = Mathf.Sign(originalScale.y);

        Vector3 targetVector = new Vector3(
            targetScale.x * xSign,
            targetScale.y * ySign,
            originalScale.z
        );

        Vector3 startScale = spriteTransform.localScale;

        while (timer < squashDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / squashDuration);

            spriteTransform.localScale = Vector3.Lerp(
                startScale,
                targetVector,
                t
            );

            yield return null;
        }

        timer = 0f;

        while (timer < squashDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / squashDuration);

            spriteTransform.localScale = Vector3.Lerp(
                targetVector,
                originalScale,
                t
            );

            yield return null;
        }

        spriteTransform.localScale = originalScale;
        currentRoutine = null;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor = spriteRenderer.color;

        for (int i = 0; i < 3; i++)
        {
            Color c = originalColor;
            c.a = 0.3f;
            spriteRenderer.color = c;

            yield return new WaitForSeconds(0.08f);

            c.a = 1f;
            spriteRenderer.color = c;

            yield return new WaitForSeconds(0.08f);
        }

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}
