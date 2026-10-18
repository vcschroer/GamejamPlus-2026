using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Menu de Pause")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private InputAction pauseAction;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image blackPanelImage;
    [SerializeField] private Image gameOverTextImage;

    [SerializeField] private float blackFadeDuration = 1f;
    [SerializeField] private float textFadeDuration = 1f;
    [SerializeField] private float waitBeforeContinue = 3f;

    [Header("Cena do Menu Principal")]
    [SerializeField] private string menuSceneName = "Menu";

    private bool isPaused;
    private bool gameOverStarted;
    private bool waitingForContinue;

    private Coroutine gameOverCoroutine;

    private void Awake()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        pauseAction.performed += OnPausePerformed;
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        pauseAction.performed -= OnPausePerformed;
        pauseAction.Disable();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (gameOverStarted)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Update()
    {
        if (!waitingForContinue || gameOverStarted == false)
            return;

        if (WasAnyInputPressed())
        {
            waitingForContinue = false;
            LoadMenuWithTransition(menuSceneName);
        }
    }

    private bool WasAnyInputPressed()
    {
        if (Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                    return true;
            }
        }

        if (Gamepad.current != null &&
            Gamepad.current.allControls.Count > 0)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button &&
                    button.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Resume()
    {
        if (gameOverStarted)
            return;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        if (gameOverStarted)
            return;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void StartGameOver()
    {
        if (gameOverStarted)
            return;

        gameOverStarted = true;
        isPaused = false;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (blackPanelImage != null)
            SetImageAlpha(blackPanelImage, 0f);

        if (gameOverTextImage != null)
            SetImageAlpha(gameOverTextImage, 0f);

        gameOverCoroutine = StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        yield return StartCoroutine(
            FadeImage(blackPanelImage, 0f, 1f, blackFadeDuration));

        yield return StartCoroutine(
            FadeImage(gameOverTextImage, 0f, 1f, textFadeDuration));

        yield return new WaitForSecondsRealtime(waitBeforeContinue);

        waitingForContinue = true;
    }

    private IEnumerator FadeImage(
        Image image,
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (image == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);

        SetImageAlpha(image, startAlpha);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetImageAlpha(image, alpha);

            yield return null;
        }

        SetImageAlpha(image, targetAlpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public void LoadMenuWithTransition(string targetSceneName)
    {
        PlayUIClick();

        if (SceneTransition.Instance == null)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        StartCoroutine(LoadMenuSequence(targetSceneName));
    }

    private IEnumerator LoadMenuSequence(string targetSceneName)
    {
        Time.timeScale = 1f;

        if (gameOverCoroutine != null)
            StopCoroutine(gameOverCoroutine);

        yield return StartCoroutine(
            SceneTransition.Instance.PlayOutroAndDoAction(() =>
            {
                SceneManager.LoadScene(targetSceneName);
            }));
    }

    public void QuitGame()
    {
        PlayUIClick();
        Time.timeScale = 1f;
        Application.Quit();
    }

    public void PlayUIHover()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("ui hover");
    }

    public void PlayUIClick()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("ui click");
    }
}