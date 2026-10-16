using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class SceneTransition : MonoBehaviour
{
    [Header("Configurações de Transição")]
    [SerializeField] private Image transitionImage;
    [SerializeField] private float frameRate = 0.03f;
    [SerializeField] private string resourcesFolderName = "Transitions";

    private Sprite[] transitionFrames;
    public static SceneTransition Instance;

    private static bool shouldPlayIntro = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAndSortFrames();

            if (transitionImage != null)
                transitionImage.gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance == this)
        {
            if (shouldPlayIntro)
            {
                shouldPlayIntro = false; 
                StopAllCoroutines();
                StartCoroutine(PlayIntroAnimation());
            }
            else
            {
                if (transitionImage != null)
                    transitionImage.gameObject.SetActive(false);
            }
        }
    }

    private void LoadAndSortFrames()
    {
        Sprite[] loadedFrames = Resources.LoadAll<Sprite>(resourcesFolderName);
        transitionFrames = loadedFrames.OrderBy(sprite => sprite.name).ToArray();

        if (transitionFrames.Length == 0)
        {
            Debug.LogError($"Erro: Nenhum frame foi encontrado na pasta 'Resources/{resourcesFolderName}'.");
        }
    }

    public IEnumerator PlayOutroAndDoAction(System.Action onTransitionFinished)
    {
        shouldPlayIntro = true;

        if (transitionImage != null && transitionFrames != null && transitionFrames.Length > 0)
        {
            transitionImage.gameObject.SetActive(true);

            for (int i = 0; i < transitionFrames.Length; i++)
            {
                transitionImage.sprite = transitionFrames[i];
                yield return new WaitForSecondsRealtime(frameRate);
            }
        }

        onTransitionFinished?.Invoke();
    }

    private IEnumerator PlayIntroAnimation()
    {
        if (transitionImage != null && transitionFrames != null && transitionFrames.Length > 0)
        {
            transitionImage.gameObject.SetActive(true);

            for (int i = transitionFrames.Length - 1; i >= 0; i--)
            {
                transitionImage.sprite = transitionFrames[i];
                yield return new WaitForSecondsRealtime(frameRate);
            }

            transitionImage.gameObject.SetActive(false);
        }
    }
}