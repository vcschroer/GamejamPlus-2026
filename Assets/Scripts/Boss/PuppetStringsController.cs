using UnityEngine;

public class PuppetStringsController : MonoBehaviour
{
    [System.Serializable]
    public class PuppetString
    {
        public GameObject stringObject;
        public Transform animatedTransform;
        [Min(0f)] public float verticalMovementAmount = 0.12f;
        [Min(0f)] public float horizontalMovementAmount = 0.05f;
        [Min(0f)] public float movementSpeed = 1.5f;
        public float phaseOffset;

        [HideInInspector] public Vector3 initialLocalPosition;
        [HideInInspector] public bool initialPositionCaptured;
    }

    [Header("Cordas (ordem: esquerda, meio, direita)")]
    [SerializeField] private PuppetString leftString = new PuppetString { phaseOffset = 0f, movementSpeed = 1.35f, verticalMovementAmount = 0.12f, horizontalMovementAmount = 0.04f };
    [SerializeField] private PuppetString middleString = new PuppetString { phaseOffset = 2.1f, movementSpeed = 1.8f, verticalMovementAmount = 0.1f, horizontalMovementAmount = 0.03f };
    [SerializeField] private PuppetString rightString = new PuppetString { phaseOffset = 4.3f, movementSpeed = 1.55f, verticalMovementAmount = 0.14f, horizontalMovementAmount = 0.05f };

    [Header("Configuração")]
    [SerializeField] private bool removeLeftStringFirst = true;

    private int lastHealth = -1;
    public bool LeftStringLost { get; private set; }

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

        lastHealth = currentHealth;

        float thirdHealth = maxHealth / 3f;
        int remainingStringCount;

        if (currentHealth > thirdHealth * 2f)
            remainingStringCount = 3;
        else if (currentHealth > thirdHealth)
            remainingStringCount = 2;
        else
            remainingStringCount = 1;

        if (removeLeftStringFirst)
        {
            SetStringActive(leftString, remainingStringCount == 3);
            SetStringActive(middleString, remainingStringCount >= 2);
            SetStringActive(rightString, remainingStringCount >= 1);
            LeftStringLost = remainingStringCount <= 2;
        }
        else
        {
            SetStringActive(leftString, remainingStringCount >= 1);
            SetStringActive(middleString, remainingStringCount >= 2);
            SetStringActive(rightString, remainingStringCount == 3);
            LeftStringLost = remainingStringCount == 1;
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

        float yOffset = Mathf.Sin(Time.time * puppetString.movementSpeed + puppetString.phaseOffset) * puppetString.verticalMovementAmount;
        float xOffset = Mathf.Cos(Time.time * (puppetString.movementSpeed * 0.8f) + puppetString.phaseOffset) * puppetString.horizontalMovementAmount;

        Vector3 position = puppetString.initialLocalPosition;
        position.y += yOffset;
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

    private void SetStringActive(PuppetString puppetString, bool active)
    {
        if (puppetString == null || puppetString.stringObject == null)
            return;

        if (puppetString.stringObject.activeSelf != active)
            puppetString.stringObject.SetActive(active);
    }
}