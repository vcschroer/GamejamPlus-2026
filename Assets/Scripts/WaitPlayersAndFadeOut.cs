using System.Collections;
using UnityEngine;
using TMPro;

public class WaitPlayersAndFadeOutText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float fadeDuration = 1f;

    private bool fadingStarted = false;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (fadingStarted)
            return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length >= 2)
        {
            fadingStarted = true;
            StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        Color startColor = textMesh != null ? textMesh.color : Color.white;
        float startAlpha = startColor.a;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (textMesh != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(startAlpha, 0f, t);
                textMesh.color = newColor;
            }

            yield return null;
        }

        if (textMesh != null)
        {
            Color finalColor = startColor;
            finalColor.a = 0f;
            textMesh.color = finalColor;
        }

        gameObject.SetActive(false);
    }
}