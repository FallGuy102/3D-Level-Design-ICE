using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerDoorInteractor : MonoBehaviour
{
    [SerializeField] private Camera interactCamera;
    [SerializeField] private float interactDistance = 2.2f;
    [SerializeField] private float nearbyDoorRadius = 1.8f;
    [SerializeField] private float nearbyDoorForwardDot = 0.25f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private KeyCode legacyInteractKey = KeyCode.E;
    [SerializeField] private Color promptColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color blockedColor = new Color(1f, 0.35f, 0.25f, 0.95f);
    [SerializeField] private float messageDuration = 1.4f;

    private DoorInteraction focusedDoor;
    private DoorKeyItem focusedKey;
    private MovingObjectInteraction focusedMover;
    private string transientMessage;
    private float messageTimer;
    private GUIStyle promptStyle;
    private readonly Collider[] nearbyHits = new Collider[16];

    private void Awake()
    {
        if (interactCamera == null)
            interactCamera = Camera.main;
    }

    private void Update()
    {
        UpdateFocus();

        if (messageTimer > 0f)
            messageTimer -= Time.deltaTime;

        if (WasInteractPressed())
            Interact();
    }

    private void UpdateFocus()
    {
        focusedDoor = null;
        focusedKey = null;
        focusedMover = null;

        Ray ray = GetInteractionRay();
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            focusedDoor = hit.collider.GetComponentInParent<DoorInteraction>();
            if (focusedDoor != null && !focusedDoor.IsInteractable)
                focusedDoor = null;

            if (focusedDoor == null)
                focusedKey = hit.collider.GetComponentInParent<DoorKeyItem>();

            if (focusedDoor == null && focusedKey == null)
            {
                focusedMover = hit.collider.GetComponentInParent<MovingObjectInteraction>();
                if (focusedMover != null && !focusedMover.IsInteractable)
                    focusedMover = null;
            }
        }

        if (focusedDoor == null && focusedKey == null && focusedMover == null)
            FindNearbyInteractable();
    }

    private Ray GetInteractionRay()
    {
        if (interactCamera != null)
            return interactCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 origin = transform.position + Vector3.up * 1.4f;
        return new Ray(origin, transform.forward);
    }

    private void Interact()
    {
        if (focusedDoor != null)
        {
            if (!focusedDoor.TryInteract(gameObject, out string message) && !string.IsNullOrEmpty(message))
                ShowMessage(message);

            return;
        }

        if (focusedKey != null)
        {
            if (focusedKey.TryPickup(gameObject, out string pickupMessage))
                ShowMessage(pickupMessage);

            return;
        }

        if (focusedMover != null)
            focusedMover.TryInteract();
    }

    private void ShowMessage(string message)
    {
        transientMessage = message;
        messageTimer = messageDuration;
    }

    private void FindNearbyInteractable()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 forward = GetInteractionRay().direction;
        int hitCount = Physics.OverlapSphereNonAlloc(origin, nearbyDoorRadius, nearbyHits, interactMask, QueryTriggerInteraction.Collide);

        float bestDoorScore = float.PositiveInfinity;
        float bestKeyScore = float.PositiveInfinity;
        float bestMoverScore = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = nearbyHits[i];
            if (hit == null)
                continue;

            Vector3 closest = hit.ClosestPoint(origin);
            Vector3 toHit = closest - origin;
            float distance = toHit.magnitude;
            if (distance <= 0.001f)
                continue;

            if (Vector3.Dot(forward, toHit / distance) < nearbyDoorForwardDot)
                continue;

            DoorInteraction door = hit.GetComponentInParent<DoorInteraction>();
            if (door != null && door.IsInteractable && distance < bestDoorScore)
            {
                if (!HasLineOfSightTo(hit, origin, closest))
                    continue;

                bestDoorScore = distance;
                focusedDoor = door;
                focusedKey = null;
                focusedMover = null;
                continue;
            }

            if (focusedDoor != null)
                continue;

            DoorKeyItem key = hit.GetComponentInParent<DoorKeyItem>();
            if (key != null && distance < bestKeyScore)
            {
                if (!HasLineOfSightTo(hit, origin, closest))
                    continue;

                bestKeyScore = distance;
                focusedKey = key;
                focusedMover = null;
                continue;
            }

            if (focusedKey != null)
                continue;

            MovingObjectInteraction mover = hit.GetComponentInParent<MovingObjectInteraction>();
            if (mover != null && mover.IsInteractable && distance < bestMoverScore)
            {
                if (!HasLineOfSightTo(hit, origin, closest))
                    continue;

                bestMoverScore = distance;
                focusedMover = mover;
            }
        }
    }

    private bool HasLineOfSightTo(Collider targetCollider, Vector3 origin, Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
            return true;

        Ray ray = new Ray(origin, toTarget / distance);
        if (!Physics.Raycast(ray, out RaycastHit hit, distance + 0.05f, interactMask, QueryTriggerInteraction.Collide))
            return true;

        if (hit.collider == targetCollider)
            return true;

        DoorInteraction targetDoor = targetCollider.GetComponentInParent<DoorInteraction>();
        if (targetDoor != null)
            return hit.collider.GetComponentInParent<DoorInteraction>() == targetDoor;

        DoorKeyItem targetKey = targetCollider.GetComponentInParent<DoorKeyItem>();
        if (targetKey != null)
            return hit.collider.GetComponentInParent<DoorKeyItem>() == targetKey;

        MovingObjectInteraction targetMover = targetCollider.GetComponentInParent<MovingObjectInteraction>();
        return targetMover != null && hit.collider.GetComponentInParent<MovingObjectInteraction>() == targetMover;
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(legacyInteractKey);
#else
        return false;
#endif
    }

    private void OnGUI()
    {
        EnsureStyle();

        string text = string.Empty;
        Color color = promptColor;

        if (messageTimer > 0f && !string.IsNullOrEmpty(transientMessage))
        {
            text = transientMessage;
            color = blockedColor;
        }
        else if (focusedDoor != null)
        {
            text = focusedDoor.PromptText;
        }
        else if (focusedKey != null)
        {
            text = "\u6309 E \u62fe\u53d6";
        }
        else if (focusedMover != null)
        {
            text = focusedMover.PromptText;
        }

        if (string.IsNullOrEmpty(text))
            return;

        promptStyle.normal.textColor = color;
        GUI.Label(new Rect(0f, Screen.height * 0.62f, Screen.width, 40f), text, promptStyle);
    }

    private void EnsureStyle()
    {
        if (promptStyle != null)
            return;

        promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
    }
}
