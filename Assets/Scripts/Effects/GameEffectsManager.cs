using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEffectsManager : MonoBehaviour
{
    public static GameEffectsManager Instance { get; private set; }

    [Header("Configuração de Nuvens (Vaivém)")]
    [Tooltip("Arraste os Transforms das nuvens do fundo aqui.")]
    [SerializeField] private List<Transform> backgroundClouds = new List<Transform>();
    [Tooltip("Velocidade da oscilação de vaivém.")]
    [SerializeField] private float cloudOscillationSpeed = 0.5f;
    [Tooltip("Distância máxima que a nuvem se desloca para cada lado.")]
    [SerializeField] private float cloudOscillationDistance = 3f;

    [Header("Árvores e Lápides (Impacto de Explosão)")]
    [SerializeField] private List<Transform> environmentalProps = new List<Transform>();
    [SerializeField] private float propShakeIntensity = 0.2f;
    [SerializeField] private float propShakeDuration = 0.3f;
    [SerializeField] private float bombEffectRadius = 4f;

    [Header("Configuração de Camera Shake")]
    [SerializeField] private Transform mainCameraTransform;
    private Vector3 cameraInitialPosition;

    private Dictionary<Transform, float> cloudInitialXPositions = new Dictionary<Transform, float>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (mainCameraTransform != null)
            cameraInitialPosition = mainCameraTransform.localPosition;

        foreach (Transform cloud in backgroundClouds)
        {
            if (cloud != null && !cloudInitialXPositions.ContainsKey(cloud))
            {
                cloudInitialXPositions.Add(cloud, cloud.position.x);
            }
        }
    }

    private void Update()
    {
        MoveCloudsBackAndForth();
    }

    private void MoveCloudsBackAndForth()
    {
        for (int i = 0; i < backgroundClouds.Count; i++)
        {
            Transform cloud = backgroundClouds[i];
            if (cloud == null) continue;

            if (!cloudInitialXPositions.ContainsKey(cloud))
            {
                cloudInitialXPositions[cloud] = cloud.position.x;
            }

            float startX = cloudInitialXPositions[cloud];

            float offsetX = Mathf.Sin((Time.time + (i * 1.5f)) * cloudOscillationSpeed) * cloudOscillationDistance;

            Vector3 pos = cloud.position;
            pos.x = startX + offsetX;
            cloud.position = pos;
        }
    }

    public void TriggerBombExplosionEffects(Vector2 explosionPosition)
    {
        TriggerCameraShake(0.25f, 0.2f);

        foreach (Transform prop in environmentalProps)
        {
            if (prop == null) continue;

            float distance = Vector2.Distance(explosionPosition, prop.position);
            if (distance <= bombEffectRadius)
            {
                StartCoroutine(ShakePropRoutine(prop));
            }
        }
    }

    private IEnumerator ShakePropRoutine(Transform prop)
    {
        Vector3 originalPos = prop.localPosition;
        float elapsed = 0f;

        while (elapsed < propShakeDuration)
        {
            if (prop == null) yield break;

            elapsed += Time.deltaTime;
            Vector2 randomOffset = Random.insideUnitCircle * propShakeIntensity;
            prop.localPosition = (Vector2)originalPos + randomOffset;

            yield return null;
        }

        if (prop != null)
            prop.localPosition = originalPos;
    }

    public void TriggerCameraShake(float duration, float magnitude)
    {
        StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        if (mainCameraTransform == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = cameraInitialPosition.x + Random.Range(-1f, 1f) * magnitude;
            float y = cameraInitialPosition.y + Random.Range(-1f, 1f) * magnitude;

            mainCameraTransform.localPosition = new Vector3(x, y, cameraInitialPosition.z);

            yield return null;
        }

        mainCameraTransform.localPosition = cameraInitialPosition;
    }
}