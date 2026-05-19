using System.Collections;
using UnityEngine;

public class MovingObjectInteraction : MonoBehaviour
{
    public enum HeightSpace
    {
        WorldY,
        LocalY
    }

    [SerializeField] private Transform moveTarget;
    [SerializeField] private HeightSpace heightSpace = HeightSpace.WorldY;
    [SerializeField] private float targetHeight = 0f;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private bool toggleBackOnSecondUse;
    [SerializeField] private bool canInteractWhileMoving;
    [SerializeField] private string promptText = "\u6309 E \u64cd\u4f5c";

    private Coroutine moveRoutine;
    private bool movedToTarget;
    private float initialHeight;
    private bool initialized;

    public string PromptText => promptText;
    public bool IsInteractable => canInteractWhileMoving || moveRoutine == null;

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        if (moveTarget == null)
            moveTarget = transform;

        moveDuration = Mathf.Max(0.01f, moveDuration);
    }

    public bool TryInteract()
    {
        Initialize();

        if (!IsInteractable)
            return false;

        float destinationHeight = toggleBackOnSecondUse && movedToTarget ? initialHeight : targetHeight;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToHeight(destinationHeight));
        movedToTarget = Mathf.Approximately(destinationHeight, targetHeight);
        return true;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        if (moveTarget == null)
            moveTarget = transform;

        initialHeight = GetCurrentHeight();
        initialized = true;
    }

    private IEnumerator MoveToHeight(float destinationHeight)
    {
        float startHeight = GetCurrentHeight();
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = t * t * (3f - 2f * t);
            SetCurrentHeight(Mathf.Lerp(startHeight, destinationHeight, t));
            yield return null;
        }

        SetCurrentHeight(destinationHeight);
        moveRoutine = null;
    }

    private float GetCurrentHeight()
    {
        return heightSpace == HeightSpace.WorldY ? moveTarget.position.y : moveTarget.localPosition.y;
    }

    private void SetCurrentHeight(float height)
    {
        if (heightSpace == HeightSpace.WorldY)
        {
            Vector3 position = moveTarget.position;
            position.y = height;
            moveTarget.position = position;
            return;
        }

        Vector3 localPosition = moveTarget.localPosition;
        localPosition.y = height;
        moveTarget.localPosition = localPosition;
    }
}
