using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

    [Header("Bolinhas de Vida do Player 1 (Esquerda)")]
    [SerializeField] private Image[] p1LifeIcons;
    [SerializeField] private Color p1Color = new Color(0.2f, 0.6f, 1f);

    [Header("Bolinhas de Vida do Player 2 (Direita)")]
    [SerializeField] private Image[] p2LifeIcons;
    [SerializeField] private Color p2Color = new Color(1f, 0.2f, 0.3f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ApplyColors();
    }

    private void ApplyColors()
    {
        if (p1LifeIcons != null)
        {
            foreach (Image img in p1LifeIcons)
            {
                if (img != null) img.color = p1Color;
            }
        }

        if (p2LifeIcons != null)
        {
            foreach (Image img in p2LifeIcons)
            {
                if (img != null) img.color = p2Color;
            }
        }
    }

    public void UpdateHealth(PlayerMovement.PlayerID playerID, int currentHealth)
    {
        if (playerID == PlayerMovement.PlayerID.Player1)
        {
            UpdatePlayer1Icons(currentHealth);
        }
        else if (playerID == PlayerMovement.PlayerID.Player2)
        {
            UpdatePlayer2Icons(currentHealth);
        }
    }

    private void UpdatePlayer1Icons(int currentHealth)
    {
        if (p1LifeIcons == null) return;

        for (int i = 0; i < p1LifeIcons.Length; i++)
        {
            if (p1LifeIcons[i] == null) continue;

            bool shouldBeActive = i < currentHealth;

            if (shouldBeActive && !p1LifeIcons[i].gameObject.activeSelf)
            {
                p1LifeIcons[i].gameObject.SetActive(true);
                StartCoroutine(PopUpInAnimation(p1LifeIcons[i].transform));
            }
            else if (!shouldBeActive && p1LifeIcons[i].gameObject.activeSelf)
            {
                StartCoroutine(PopUpOutAnimation(p1LifeIcons[i].gameObject));
            }
        }
    }

    private void UpdatePlayer2Icons(int currentHealth)
    {
        if (p2LifeIcons == null) return;

        int totalIcons = p2LifeIcons.Length;

        for (int i = 0; i < totalIcons; i++)
        {
            if (p2LifeIcons[i] == null) continue;

            int healthIndex = totalIcons - 1 - i;
            bool shouldBeActive = healthIndex < currentHealth;

            if (shouldBeActive && !p2LifeIcons[i].gameObject.activeSelf)
            {
                p2LifeIcons[i].gameObject.SetActive(true);
                StartCoroutine(PopUpInAnimation(p2LifeIcons[i].transform));
            }
            else if (!shouldBeActive && p2LifeIcons[i].gameObject.activeSelf)
            {
                StartCoroutine(PopUpOutAnimation(p2LifeIcons[i].gameObject));
            }
        }
    }

    private IEnumerator PopUpInAnimation(Transform targetTransform)
    {
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1.25f, Mathf.Sin(t * Mathf.PI * 0.5f));
            targetTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        targetTransform.localScale = Vector3.one;
    }

    private IEnumerator PopUpOutAnimation(GameObject targetObj)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Transform targetTransform = targetObj.transform;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 1.4f, Mathf.Sin(t * Mathf.PI));
            targetTransform.localScale = Vector3.one * (1f - t) * scale;
            yield return null;
        }

        targetTransform.localScale = Vector3.one;
        targetObj.SetActive(false);
    }
}