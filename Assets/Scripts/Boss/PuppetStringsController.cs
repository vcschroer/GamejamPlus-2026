using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuppetStringsController : MonoBehaviour
{
    [System.Serializable]
    public class PuppetString
    {
        public GameObject stringObject;
        public Transform animatedTransform;
        [Min(0f)] public float horizontalMovementAmount = 0.05f;
        [Min(0f)] public float movementSpeed = 1.5f;
        public float phaseOffset;

        [HideInInspector] public Vector3 initialLocalPosition;
        [HideInInspector] public bool initialPositionCaptured;
    }

    [Header("Cordas (ordem: esquerda, meio, direita)")]
    [SerializeField] private PuppetString leftString = new PuppetString { phaseOffset = 0f, movementSpeed = 1.35f, horizontalMovementAmount = 0.04f };
    [SerializeField] private PuppetString middleString = new PuppetString { phaseOffset = 2.1f, movementSpeed = 1.8f, horizontalMovementAmount = 0.03f };
    [SerializeField] private PuppetString rightString = new PuppetString { phaseOffset = 4.3f, movementSpeed = 1.55f, horizontalMovementAmount = 0.05f };

    [Header("Configuração de Ruptura & Piscar")]
    [SerializeField] private bool removeLeftStringFirst = true;
    [SerializeField] private float warningBlinkDuration = 1.2f;
    [SerializeField] private float warningBlinkSpeed = 12f;

    [Header("Inclinação do Boss por Corda Perdida")]
    [SerializeField] private float leanAnglePerString = 12f;

    private int lastHealth = -1;
    public bool LeftStringLost { get; private set; }
    public float CurrentLeanAngle { get; private set; }

    private HashSet<Coroutine> activeBlinks = new HashSet<Coroutine>();

    private void Awake()
    {
        CaptureInitialPosition(leftString);
        CaptureInitialPosition(middleString);
        CaptureInitialPosition(rightString);
    }

    private void Update()
    {
        AnimateString(leftString);
        AnimateString(middleString);
        AnimateString(rightString);
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (lastHealth == currentHealth)
            return;

        int previousStringCount = GetStringCountForHealth(lastHealth, maxHealth);
        lastHealth = currentHealth;
        int targetStringCount = GetStringCountForHealth(currentHealth, maxHealth);

        if (targetStringCount < previousStringCount)
        {
            HandleStringBreak(previousStringCount, targetStringCount);
        }
        else
        {
            ApplyInstantState(targetStringCount);
        }
    }

    private int GetStringCountForHealth(int health, int maxHealth)
    {
        if (health < 0) health = 0;
        float thirdHealth = maxHealth / 3f;

        if (health > thirdHealth * 2f)
            return 3;
        else if (health > thirdHealth)
            return 2;
        else
            return 1;
    }

    private void ApplyInstantState(int stringCount)
    {
        if (removeLeftStringFirst)
        {
            SetStringInstant(leftString, stringCount >= 3);
            SetStringInstant(middleString, stringCount >= 2);
            SetStringInstant(rightString, stringCount >= 1);
            LeftStringLost = stringCount <= 2;
        }
        else
        {
            SetStringInstant(leftString, stringCount >= 1);
            SetStringInstant(middleString, stringCount >= 2);
            SetStringInstant(rightString, stringCount >= 3);
            LeftStringLost = stringCount == 1;
        }

        int lostCount = 3 - stringCount;
        CurrentLeanAngle = lostCount * leanAnglePerString * (removeLeftStringFirst ? 1f : -1f);
    }

    private void HandleStringBreak(int previousCount, int targetCount)
    {
        PuppetString stringToBreak = null;

        if (removeLeftStringFirst)
        {
            if (previousCount == 3 && targetCount == 2) stringToBreak = leftString;
            else if (previousCount == 2 && targetCount == 1) stringToBreak = middleString;
            else if (previousCount == 3 && targetCount == 1) stringToBreak = leftString;
        }
        else
        {
            if (previousCount == 3 && targetCount == 2) stringToBreak = rightString;
            else if (previousCount == 2 && targetCount == 1) stringToBreak = middleString;
            else if (previousCount == 3 && targetCount == 1) stringToBreak = rightString;
        }

        ApplyInstantState(targetCount);

        if (stringToBreak != null && stringToBreak.stringObject != null)
        {
            StartCoroutine(BreakStringWithWarningRoutine(stringToBreak));
        }
    }

    private IEnumerator BreakStringWithWarningRoutine(PuppetString puppetString)
    {
        if (puppetString.stringObject == null) yield break;

        GameObject obj = puppetString.stringObject;
        obj.SetActive(true);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        float elapsed = 0f;
        while (elapsed < warningBlinkDuration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(0.2f, 1f, (Mathf.Sin(elapsed * warningBlinkSpeed) + 1f) * 0.5f);
                Color c = originalColor;
                c.a = alpha;
                sr.color = c;
            }
            yield return null;
        }

        if (sr != null) sr.color = originalColor;
        obj.SetActive(false);
    }

    private void SetStringInstant(PuppetString puppetString, bool active)
    {
        if (puppetString == null || puppetString.stringObject == null)
            return;

        if (puppetString.stringObject.activeSelf != active)
            puppetString.stringObject.SetActive(active);

        if (active)
        {
            SpriteRenderer sr = puppetString.stringObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
    }

    private void CaptureInitialPosition(PuppetString puppetString)
    {
        if (puppetString == null) return;
        Transform target = GetTargetTransform(puppetString);
        if (target == null) return;

        puppetString.initialLocalPosition = target.localPosition;
        puppetString.initialPositionCaptured = true;
    }

    private void AnimateString(PuppetString puppetString)
    {
        if (puppetString == null || !puppetString.initialPositionCaptured)
            return;

        Transform target = GetTargetTransform(puppetString);
        if (target == null || !target.gameObject.activeInHierarchy)
            return;

        float xOffset = Mathf.Cos(Time.time * puppetString.movementSpeed + puppetString.phaseOffset) * puppetString.horizontalMovementAmount;

        Vector3 position = puppetString.initialLocalPosition;
        position.x += xOffset;

        target.localPosition = position;
    }

    private Transform GetTargetTransform(PuppetString puppetString)
    {
        if (puppetString.animatedTransform != null)
            return puppetString.animatedTransform;

        if (puppetString.stringObject != null)
            return puppetString.stringObject.transform;

        return null;
    }
}