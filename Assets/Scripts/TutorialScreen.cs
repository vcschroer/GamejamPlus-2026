using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TutorialScreen : MonoBehaviour
{
    [Header("Configuração das Imagens")]
    [SerializeField] private Image[] tutorialImages;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float cooldownTime = 1.0f;

    [Header("Cena de Destino")]
    [SerializeField] private string nextSceneName;

    private int currentIndex = -1;
    private bool canProceed = true;
    private bool isTransitioning = false;

    private void Start()
    {
        foreach (Image img in tutorialImages)
        {
            if (img != null)
            {
                Color c = img.color;
                c.a = 0;
                img.color = c;
                img.gameObject.SetActive(false);
            }
        }

        ShowNextTutorialStep();
    }

    private void Update()
    {
        if (isTransitioning || !canProceed) return;

        if (AnyInputPressed())
        {
            if (currentIndex < tutorialImages.Length - 1)
            {
                ShowNextTutorialStep();
                StartCoroutine(CooldownRoutine());
            }
            else
            {
                StartCoroutine(FinishTutorialAndLoadScene());
            }
        }
    }

    private bool AnyInputPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            return true;

        if (Gamepad.current != null && Gamepad.current.allControls.Count > 0)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ShowNextTutorialStep()
    {
        currentIndex++;
        if (currentIndex < tutorialImages.Length && tutorialImages[currentIndex] != null)
        {
            StartCoroutine(FadeIn(tutorialImages[currentIndex]));
        }
    }

    private IEnumerator FadeIn(Image img)
    {
        img.gameObject.SetActive(true);
        float elapsedTime = 0f;
        Color color = img.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            img.color = color;
            yield return null;
        }

        color.a = 1f;
        img.color = color;
    }

    private IEnumerator CooldownRoutine()
    {
        canProceed = false;
        yield return new WaitForSeconds(cooldownTime);
        canProceed = true;
    }

    private IEnumerator FinishTutorialAndLoadScene()
    {
        isTransitioning = true;

        SceneTransition transition = SceneTransition.Instance;
        if (transition == null)
        {
            transition = Object.FindFirstObjectByType<SceneTransition>();
        }

        if (transition != null)
        {
            yield return StartCoroutine(transition.PlayOutroAndDoAction(() => {
                SceneManager.LoadScene(nextSceneName);
            }));
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}