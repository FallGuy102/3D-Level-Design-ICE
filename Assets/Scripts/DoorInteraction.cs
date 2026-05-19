using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    public enum DoorMode
    {
        Normal,
        OneSided,
        Keyed
    }

    public enum AllowedSide
    {
        PositiveLocalZ,
        NegativeLocalZ
    }

    [Header("Mode")]
    [SerializeField] private DoorMode doorMode = DoorMode.Normal;
    [SerializeField] private AllowedSide allowedSide = AllowedSide.PositiveLocalZ;
    [SerializeField] private string keyId = "Key01";

    [Header("Motion")]
    [SerializeField] private Transform rotateTarget;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 6f;
    [SerializeField] private bool canClose = true;

    [Header("Text")]
    [SerializeField] private string promptText = "按 E 开门";
    [SerializeField] private string closePromptText = "按 E 关门";
    [SerializeField] private string wrongSideText = "无法从此面打开";
    [SerializeField] private string lockedText = "锁住了";

    private Quaternion closedRotation;
    private Quaternion openedRotation;
    private bool isOpen;
    private bool initialized;

    public string PromptText => isOpen && canClose ? closePromptText : promptText;
    public bool IsOpen => isOpen;
    public bool IsInteractable => !isOpen || canClose;

    private void Awake()
    {
        InitializeRotation();
    }

    private void OnValidate()
    {
        if (rotateTarget == null)
            rotateTarget = transform;
    }

    private void InitializeRotation()
    {
        if (initialized)
            return;

        if (rotateTarget == null)
            rotateTarget = transform;

        closedRotation = rotateTarget.localRotation;
        openedRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        initialized = true;
    }

    private void Update()
    {
        InitializeRotation();

        Quaternion targetRotation = isOpen ? openedRotation : closedRotation;
        rotateTarget.localRotation = Quaternion.Slerp(
            rotateTarget.localRotation,
            targetRotation,
            1f - Mathf.Exp(-openSpeed * Time.deltaTime));
    }

    public bool TryInteract(GameObject player, out string message)
    {
        InitializeRotation();
        message = string.Empty;

        if (isOpen)
        {
            if (!canClose)
                return true;

            isOpen = false;
            return true;
        }

        if (doorMode == DoorMode.OneSided && !IsPlayerOnAllowedSide(player.transform.position))
        {
            message = wrongSideText;
            return false;
        }

        if (doorMode == DoorMode.Keyed && !PlayerHasKey(player))
        {
            message = lockedText;
            return false;
        }

        isOpen = true;
        return true;
    }

    public bool CanOpenFrom(GameObject player, out string blockedMessage)
    {
        blockedMessage = string.Empty;

        if (isOpen)
            return true;

        if (doorMode == DoorMode.OneSided && !IsPlayerOnAllowedSide(player.transform.position))
        {
            blockedMessage = wrongSideText;
            return false;
        }

        if (doorMode == DoorMode.Keyed && !PlayerHasKey(player))
        {
            blockedMessage = lockedText;
            return false;
        }

        return true;
    }

    private bool IsPlayerOnAllowedSide(Vector3 playerPosition)
    {
        Vector3 localPoint = transform.InverseTransformPoint(playerPosition);
        return allowedSide == AllowedSide.PositiveLocalZ ? localPoint.z >= 0f : localPoint.z <= 0f;
    }

    private bool PlayerHasKey(GameObject player)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return true;

        DoorKeyInventory inventory = player.GetComponent<DoorKeyInventory>();
        return inventory != null && inventory.HasKey(keyId);
    }
}
