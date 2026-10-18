using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameEffectsManager : MonoBehaviour
{
    public static GameEffectsManager Instance { get; private set; }

    [Header("Configuração de Nuvens")]
    [SerializeField] private List<Transform> backgroundClouds = new List<Transform>();
    [SerializeField] private float cloudOscillationSpeed = 0.5f;
    [SerializeField] private float cloudOscillationDistance = 3f;

    [Header("Configuração das Árvores")]
    [SerializeField] private List<Transform> trees = new List<Transform>();
    [SerializeField] private float treeWindSpeed = 2f;
    [SerializeField] private float treeWindAngle = 3f;

    [Header("Configuração das Cortinas ")]
    [SerializeField] private List<Transform> curtains = new List<Transform>();
    [SerializeField] private float curtainWindSpeed = 1.5f;
    [SerializeField] private float curtainWindAngle = 2.5f;

    [Header("Configuração das Lápides")]
    [SerializeField] private List<Transform> tombstones = new List<Transform>();
    [SerializeField] private float tombstoneSinkDepth = 1.5f;
    [SerializeField] private float tombstoneSinkDuration = 0.15f;
    [SerializeField] private float tombstoneUndergroundWaitTime = 2f;
    [SerializeField] private float tombstoneRiseDuration = 0.5f;
    [SerializeField] private float bombEffectRadius = 4f;

    [Header("Configuração de Camera Shake")]
    [SerializeField] private Transform mainCameraTransform;
    private Vector3 cameraInitialPosition;

    private Dictionary<Transform, float> cloudInitialXPositions = new Dictionary<Transform, float>();
    private Dictionary<Transform, Quaternion> treeInitialRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Quaternion> curtainInitialRotations = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> tombstoneInitialPositions = new Dictionary<Transform, Vector3>();

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
                cloudInitialXPositions.Add(cloud, cloud.position.x);
        }

        foreach (Transform tree in trees)
        {
            if (tree != null && !treeInitialRotations.ContainsKey(tree))
                treeInitialRotations.Add(tree, tree.rotation);
        }

        foreach (Transform curtain in curtains)
        {
            if (curtain != null && !curtainInitialRotations.ContainsKey(curtain))
                curtainInitialRotations.Add(curtain, curtain.rotation);
        }

        foreach (Transform tombstone in tombstones)
        {
            if (tombstone != null && !tombstoneInitialPositions.ContainsKey(tombstone))
                tombstoneInitialPositions.Add(tombstone, tombstone.position);
        }
    }

    private void Update()
    {
        MoveCloudsBackAndForth();
        AnimateTreesWind();
        AnimateCurtainsWind();
    }

    private void OnDisable()
    {
    }

    private void MoveCloudsBackAndForth()
    {
        for (int i = 0; i < backgroundClouds.Count; i++)
        {
            Transform cloud = backgroundClouds[i];
            if (cloud == null) continue;

            if (!cloudInitialXPositions.ContainsKey(cloud))
                cloudInitialXPositions[cloud] = cloud.position.x;

            float startX = cloudInitialXPositions[cloud];
            float offsetX = Mathf.Sin((Time.time + (i * 1.5f)) * cloudOscillationSpeed) * cloudOscillationDistance;

            Vector3 pos = cloud.position;
            pos.x = startX + offsetX;
            cloud.position = pos;
        }
    }

    private void AnimateTreesWind()
    {
        for (int i = 0; i < trees.Count; i++)
        {
            Transform tree = trees[i];
            if (tree == null) continue;

            if (!treeInitialRotations.ContainsKey(tree))
                treeInitialRotations[tree] = tree.rotation;

            Quaternion baseRot = treeInitialRotations[tree];
            float zRotation = Mathf.Sin((Time.time + (i * 2.0f)) * treeWindSpeed) * treeWindAngle;

            tree.rotation = baseRot * Quaternion.Euler(0f, 0f, zRotation);
        }
    }

    private void AnimateCurtainsWind()
    {
        for (int i = 0; i < curtains.Count; i++)
        {
            Transform curtain = curtains[i];
            if (curtain == null) continue;

            if (!curtainInitialRotations.ContainsKey(curtain))
                curtainInitialRotations[curtain] = curtain.rotation;

            Quaternion baseRot = curtainInitialRotations[curtain];
            float zRotation = Mathf.Sin((Time.time + (i * 1.2f)) * curtainWindSpeed) * curtainWindAngle;

            curtain.rotation = baseRot * Quaternion.Euler(0f, 0f, zRotation);
        }
    }

    public void TriggerBombExplosionEffects(Vector2 explosionPosition)
    {
        TriggerCameraShake(0.35f, 0.25f);

        foreach (Transform tombstone in tombstones)
        {
            if (tombstone == null) continue;

            float distance = Vector2.Distance(explosionPosition, tombstone.position);
            if (distance <= bombEffectRadius)
            {
                StartCoroutine(TombstoneExplosionRoutine(tombstone));
            }
        }
    }

    private IEnumerator TombstoneExplosionRoutine(Transform tombstone)
    {
        if (!tombstoneInitialPositions.ContainsKey(tombstone)) yield break;

        Vector3 startPos = tombstoneInitialPositions[tombstone];
        Vector3 targetPos = startPos - new Vector3(0f, tombstoneSinkDepth, 0f);

        float elapsed = 0f;
        while (elapsed < tombstoneSinkDuration)
        {
            if (tombstone == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / tombstoneSinkDuration;
            tombstone.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        tombstone.position = targetPos;

        yield return new WaitForSeconds(tombstoneUndergroundWaitTime);

        elapsed = 0f;
        while (elapsed < tombstoneRiseDuration)
        {
            if (tombstone == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / tombstoneRiseDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            tombstone.position = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }
        tombstone.position = startPos;
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

    public void TriggerPlayerDamageShake()
    {
        TriggerCameraShake(0.2f, 0.15f);
    }
}