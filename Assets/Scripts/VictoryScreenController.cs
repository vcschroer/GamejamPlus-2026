using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryScreenController : MonoBehaviour
{
    [Header("Configuração de Cooldown e Painel")]
    [SerializeField] private float initialCooldown = 2.0f;
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Configuração do Texto")]
    [SerializeField] private RectTransform victoryTextRect;
    [SerializeField] private float textMoveSpeed = 100f;
    [SerializeField] private float targetPosY = 400f;

    [Header("Cena do Menu")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool canStartVictory = false;

    private void Start()
    {
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(false);
        }

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(initialCooldown);
        canStartVictory = true;
    }

    private void Update()
    {
        if (!canStartVictory) return;

        if (victoryTextRect != null)
        {
            Vector2 anchoredPos = victoryTextRect.anchoredPosition;
            anchoredPos.y += textMoveSpeed * Time.unscaledDeltaTime;
            victoryTextRect.anchoredPosition = anchoredPos;

            if (anchoredPos.y >= targetPosY)
            {
                canStartVictory = false;
                StartCoroutine(FadeAndLoadMenuRoutine());
            }
        }
    }

    private IEnumerator FadeAndLoadMenuRoutine()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadePanel.color;

            while (elapsed < fadeDuration)
            {
                elapsed += UnityEngine.Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadePanel.color = c;
                yield return null;
            }

            c.a = 1f;
            fadePanel.color = c;
        }

        SceneTransition transition = SceneTransition.Instance;
        if (transition == null)
        {
            transition = Object.FindFirstObjectByType<SceneTransition>();
        }

        if (transition != null)
        {
            yield return StartCoroutine(transition.PlayOutroAndDoAction(() => {
                SceneManager.LoadScene(menuSceneName);
            }));
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}