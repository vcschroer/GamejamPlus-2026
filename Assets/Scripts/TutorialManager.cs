using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStage
    {
        public GameObject stageGameObject;
    }

    [SerializeField] private List<TutorialStage> tutorialStages = new List<TutorialStage>();
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField] private string nextSceneName = "GameScene";
    [SerializeField] private float loadNextSceneDelay = 2f;

    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private BossController bossController;

    [Header("Texto de Vitória do Tutorial")]
    [SerializeField] private GameObject tutorialVictoryTextObject;

    private enum TutorialStep { ConnectPlayers, Move, Jump, Dash, BombPass, BossStagger, Completed }
    private TutorialStep currentStep = TutorialStep.ConnectPlayers;

    private bool bombSpawned = false;
    private bool bombPassed = false;
    private bool bossStaggerHit = false;

    private void Start()
    {
        for (int i = 0; i < tutorialStages.Count; i++)
        {
            if (tutorialStages[i].stageGameObject != null)
            {
                tutorialStages[i].stageGameObject.SetActive(i == 0);
                SetObjectAlpha(tutorialStages[i].stageGameObject, i == 0 ? 1f : 0f);
            }
        }

        if (tutorialVictoryTextObject != null)
        {
            tutorialVictoryTextObject.SetActive(false);
        }

        if (bossController != null)
        {
            bossController.gameObject.SetActive(false);
        }

        PlayerMovement[] initialPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in initialPlayers)
        {
            p.SetTutorialHealth(9999);
        }
    }

    private void Update()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (!p.isTutorialMode)
            {
                p.SetTutorialHealth(9999);
            }
        }

        CheckTutorialProgress();
    }

    private void CheckTutorialProgress()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        switch (currentStep)
        {
            case TutorialStep.ConnectPlayers:
                if (players.Length >= 2)
                {
                    AdvanceToStep(TutorialStep.Move);
                }
                break;

            case TutorialStep.Move:
                if (players.Length < 2) break;
                bool movedCompleted = true;
                foreach (var p in players)
                {
                    if (!p.tutorialMoved)
                    {
                        movedCompleted = false;
                        break;
                    }
                }
                if (movedCompleted)
                {
                    AdvanceToStep(TutorialStep.Jump);
                }
                break;

            case TutorialStep.Jump:
                if (players.Length < 2) break;
                bool jumpedCompleted = true;
                foreach (var p in players)
                {
                    if (!p.tutorialJumped)
                    {
                        jumpedCompleted = false;
                        break;
                    }
                }
                if (jumpedCompleted)
                {
                    AdvanceToStep(TutorialStep.Dash);
                }
                break;

            case TutorialStep.Dash:
                if (players.Length < 2) break;
                bool dashedCompleted = true;
                foreach (var p in players)
                {
                    if (!p.tutorialDashed)
                    {
                        dashedCompleted = false;
                        break;
                    }
                }
                if (dashedCompleted)
                {
                    AdvanceToStep(TutorialStep.BombPass);
                }
                break;

            case TutorialStep.BombPass:
                if (!bombSpawned && bombPrefab != null && players.Length > 0)
                {
                    PlayerMovement randomPlayer = players[Random.Range(0, players.Length)];
                    GameObject spawnedBomb = Instantiate(bombPrefab, randomPlayer.transform.position, Quaternion.identity);

                    Bomb bombComponent = spawnedBomb.GetComponent<Bomb>();
                    if (bombComponent != null)
                    {
                        bombComponent.PickUp(randomPlayer.transform);
                    }

                    bombSpawned = true;
                }

                if (bombPassed)
                {
                    AdvanceToStep(TutorialStep.BossStagger);
                }
                break;

            case TutorialStep.BossStagger:
                if (bossController != null && !bossController.gameObject.activeSelf)
                {
                    bossController.gameObject.SetActive(true);
                }
                break;
        }
    }

    public void NotifyBombPassedBetweenPlayers()
    {
        if (currentStep == TutorialStep.BombPass)
        {
            bombPassed = true;
        }
    }

    public void NotifyBossHitByBombInStagger()
    {
        if (currentStep == TutorialStep.BossStagger && !bossStaggerHit)
        {
            bossStaggerHit = true;
            AdvanceToStep(TutorialStep.Completed);
        }
    }

    private void AdvanceToStep(TutorialStep nextStep)
    {
        int previousIndex = (int)currentStep;
        currentStep = nextStep;
        int currentIndex = (int)currentStep;

        StartCoroutine(TransitionStepRoutine(previousIndex, currentIndex));
    }

    private IEnumerator TransitionStepRoutine(int prevIndex, int currIndex)
    {
        if (prevIndex < tutorialStages.Count && tutorialStages[prevIndex].stageGameObject != null)
        {
            yield return StartCoroutine(FadeObjectAlpha(tutorialStages[prevIndex].stageGameObject, 1f, 0f));
            tutorialStages[prevIndex].stageGameObject.SetActive(false);
        }

        if (currIndex < tutorialStages.Count && tutorialStages[currIndex].stageGameObject != null)
        {
            GameObject nextObj = tutorialStages[currIndex].stageGameObject;
            nextObj.SetActive(true);
            SetObjectAlpha(nextObj, 0f);
            yield return StartCoroutine(FadeObjectAlpha(nextObj, 0f, 1f));
        }

        if (currIndex >= tutorialStages.Count)
        {
            if (tutorialVictoryTextObject != null)
            {
                tutorialVictoryTextObject.SetActive(true);
                SetObjectAlpha(tutorialVictoryTextObject, 0f);
                yield return StartCoroutine(FadeObjectAlpha(tutorialVictoryTextObject, 0f, 1f));
            }

            yield return new WaitForSeconds(loadNextSceneDelay);

            if (SceneTransition.Instance != null)
            {
                yield return StartCoroutine(SceneTransition.Instance.PlayOutroAndDoAction(() =>
                {
                    SceneManager.LoadScene(nextSceneName);
                }));
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }

    private void SetObjectAlpha(GameObject obj, float alpha)
    {
        if (obj == null) return;

        Graphic[] graphics = obj.GetComponentsInChildren<Graphic>();
        foreach (Graphic g in graphics)
        {
            Color c = g.color;
            c.a = alpha;
            g.color = c;
        }

        SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer s in sprites)
        {
            Color c = s.color;
            c.a = alpha;
            s.color = c;
        }
    }

    private IEnumerator FadeObjectAlpha(GameObject obj, float startAlpha, float endAlpha)
    {
        if (obj == null) yield break;

        Graphic[] graphics = obj.GetComponentsInChildren<Graphic>();
        SpriteRenderer[] sprites = obj.GetComponentsInChildren<SpriteRenderer>();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);

            foreach (Graphic g in graphics)
            {
                if (g != null)
                {
                    Color c = g.color;
                    c.a = currentAlpha;
                    g.color = c;
                }
            }

            foreach (SpriteRenderer s in sprites)
            {
                if (s != null)
                {
                    Color c = s.color;
                    c.a = currentAlpha;
                    s.color = c;
                }
            }

            yield return null;
        }

        SetObjectAlpha(obj, endAlpha);
    }
}