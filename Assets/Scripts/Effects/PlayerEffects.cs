using System.Collections;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    [Header("Referência da Sprite")]
    [SerializeField] private Transform spriteTransform;

    [Header("Configurações de Squash & Stretch")]
    [SerializeField] private float squashDuration = 0.1f;
    [SerializeField] private Vector2 jumpSquashScale = new Vector2(0.7f, 1.3f);
    [SerializeField] private Vector2 landSquashScale = new Vector2(1.3f, 0.7f);
    [SerializeField] private Vector2 wallSquashScale = new Vector2(0.8f, 1.2f);
    [SerializeField] private Vector2 throwSquashScale = new Vector2(1.3f, 0.7f);  

    private Vector3 originalScale;
    private Coroutine currentRoutine;

    private void Awake()
    {
        if (spriteTransform == null)
            spriteTransform = transform;

        originalScale = spriteTransform.localScale;
    }

    public void TriggerJump()
    {
        PlayEffect(jumpSquashScale);
    }

    public void TriggerLand()
    {
        PlayEffect(landSquashScale);
    }

    public void TriggerWallSlide()
    {
        PlayEffect(wallSquashScale);
    }

    public void TriggerThrow() 
    {
        PlayEffect(throwSquashScale);
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
        float xSign = Mathf.Sign(spriteTransform.localScale.x);
        float ySign = Mathf.Sign(spriteTransform.localScale.y);

        Vector3 targetVector = new Vector3(targetScale.x * xSign, targetScale.y * ySign, 1f);
        Vector3 startScale = spriteTransform.localScale;

        while (timer < squashDuration)
        {
            timer += Time.deltaTime;
            float t = timer / squashDuration;
            spriteTransform.localScale = Vector3.Lerp(startScale, targetVector, t);
            yield return null;
        }

        timer = 0f;
        while (timer < squashDuration)
        {
            timer += Time.deltaTime;
            float t = timer / squashDuration;
            spriteTransform.localScale = Vector3.Lerp(targetVector, originalScale, t);
            yield return null;
        }

        spriteTransform.localScale = originalScale;
        currentRoutine = null;
    }
}