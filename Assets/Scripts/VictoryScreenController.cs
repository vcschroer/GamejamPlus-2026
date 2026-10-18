using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VictoryScreenController : MonoBehaviour
{
    [Header("Configuração de Painel e Fade")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Configuração do Texto/Imagem")]
    [SerializeField] private RectTransform victoryTextRect;
    [SerializeField] private float textMoveSpeed = 400f;
    [SerializeField] private float startPosY = -150f;
    [SerializeField] private float targetPosY = 300f;

    [Header("TEMPO DE ESPERA (Ajuste aqui o tempo na tela)")]
    [SerializeField] private float waitTimeOnScreen = 5.0f;

    [Header("Cena do Menu")]
    [SerializeField] private string menuSceneName = "Menu";

    private void Start()
    {
        if (victoryTextRect != null)
        {
            Vector2 pos = victoryTextRect.anchoredPosition;
            pos.y = startPosY;
            victoryTextRect.anchoredPosition = pos;
        }

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color c = fadePanel.color;
            c.a = 1f;
            fadePanel.color = c;
        }

        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        if (fadePanel != null)
        {
            float elapsed = 0f;
            Color c = fadePanel.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                fadePanel.color = c;
                yield return null;
            }

            c.a = 0f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(false);
        }

        if (victoryTextRect != null)
        {
            Vector2 pos = victoryTextRect.anchoredPosition;
            while (pos.y < targetPosY)
            {
                pos.y += textMoveSpeed * Time.unscaledDeltaTime;
                if (pos.y > targetPosY) pos.y = targetPosY;
                victoryTextRect.anchoredPosition = pos;
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(waitTimeOnScreen);

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadePanel.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadePanel.color = c;
                yield return null;
            }

            c.a = 1f;
            fadePanel.color = c;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}