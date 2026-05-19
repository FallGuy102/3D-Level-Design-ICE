using UnityEngine;

public class EnemyFreezeController : MonoBehaviour
{
    [Header("Freeze")]
    [SerializeField] private Material iceMaterial;
    [SerializeField] private Material levelTwoIceMaterial;
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private Vector3 icePadding = new Vector3(0.25f, 0.2f, 0.25f);

    [Header("Slide")]
    [SerializeField] private float slideSpeed = 4.5f;
    [SerializeField] private float slideDrag = 5f;
    [SerializeField] private float slideDuration = 1.2f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController characterController;
    private Animator animator;
    private Renderer[] renderers;
    private GameObject iceBlock;
    private Collider iceBlockCollider;
    private Vector3 slideVelocity;
    private float freezeTimer;
    private float slideTimer;
    private float verticalVelocity;
    private float animatorSpeed = 1f;
    private bool isFrozen;

    public bool IsFrozen => isFrozen;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        UpdateGravity();

        if (!isFrozen)
            return;

        freezeTimer -= Time.deltaTime;
        UpdateSlide();

        if (freezeTimer <= 0f)
            Thaw();
    }

    public void Freeze(Vector3 hitPoint, Vector3 shotDirection)
    {
        Freeze(hitPoint, shotDirection, false);
    }

    public void Freeze(Vector3 hitPoint, Vector3 shotDirection, bool useLevelTwoIce)
    {
        bool wasFrozen = isFrozen;
        isFrozen = true;
        freezeTimer = freezeDuration;
        slideTimer = slideDuration;

        Vector3 horizontalDirection = Vector3.ProjectOnPlane(shotDirection, Vector3.up).normalized;
        if (horizontalDirection.sqrMagnitude < 0.01f)
            horizontalDirection = transform.forward;

        slideVelocity = horizontalDirection * slideSpeed;
        if (!wasFrozen)
            SetAnimatorFrozen(true);
        CreateIceBlockIfNeeded();
        ApplyIceBlockMaterial(useLevelTwoIce);
        FitIceBlockToEnemy();
        if (iceBlockCollider != null)
            iceBlockCollider.enabled = true;
        iceBlock.SetActive(true);
    }

    public void Thaw()
    {
        isFrozen = false;
        slideVelocity = Vector3.zero;
        slideTimer = 0f;
        SetAnimatorFrozen(false);

        if (iceBlock != null)
        {
            if (iceBlockCollider != null)
                iceBlockCollider.enabled = false;

            iceBlock.SetActive(false);
        }
    }

    public void PushWhileFrozen(Vector3 shotDirection)
    {
        if (!isFrozen)
            return;

        Vector3 horizontalDirection = Vector3.ProjectOnPlane(shotDirection, Vector3.up).normalized;
        if (horizontalDirection.sqrMagnitude < 0.01f)
            horizontalDirection = transform.forward;

        slideVelocity = horizontalDirection * slideSpeed;
        slideTimer = slideDuration;
    }

    public void Shatter()
    {
        if (!isFrozen)
            return;

        Destroy(gameObject);
    }

    private void UpdateSlide()
    {
        if (slideTimer <= 0f)
            return;

        slideTimer -= Time.deltaTime;
        Vector3 movement = slideVelocity * Time.deltaTime;

        if (characterController != null && characterController.enabled)
            characterController.Move(movement);
        else
            transform.position += movement;

        slideVelocity = Vector3.Lerp(slideVelocity, Vector3.zero, slideDrag * Time.deltaTime);
    }

    private void UpdateGravity()
    {
        float deltaTime = Time.deltaTime;
        if (characterController != null && characterController.enabled)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedGravity;
            else
                verticalVelocity += gravity * deltaTime;

            characterController.Move(Vector3.up * verticalVelocity * deltaTime);
            return;
        }

        verticalVelocity += gravity * deltaTime;
        transform.position += Vector3.up * verticalVelocity * deltaTime;
    }

    private void CreateIceBlockIfNeeded()
    {
        if (iceBlock != null)
            return;

        iceBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        iceBlock.name = "FrozenIceBlock";
        iceBlock.transform.SetParent(transform, false);

        iceBlockCollider = iceBlock.GetComponent<Collider>();
        if (iceBlockCollider != null)
        {
            iceBlockCollider.isTrigger = true;
            iceBlockCollider.enabled = false;
        }

        Renderer iceRenderer = iceBlock.GetComponent<Renderer>();
        if (iceRenderer != null)
            iceRenderer.sharedMaterial = GetIceBlockMaterial();

        iceBlock.SetActive(false);
    }

    private void FitIceBlockToEnemy()
    {
        Bounds bounds = GetVisualBounds();
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = new Vector3(
            bounds.size.x / Mathf.Max(transform.lossyScale.x, 0.001f),
            bounds.size.y / Mathf.Max(transform.lossyScale.y, 0.001f),
            bounds.size.z / Mathf.Max(transform.lossyScale.z, 0.001f));
        float squareFootprintSize = Mathf.Max(localSize.x + icePadding.x, localSize.z + icePadding.z);

        iceBlock.transform.localPosition = localCenter;
        iceBlock.transform.localRotation = Quaternion.identity;
        iceBlock.transform.localScale = new Vector3(squareFootprintSize, localSize.y + icePadding.y, squareFootprintSize);
    }

    private Bounds GetVisualBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position + Vector3.up, Vector3.one);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled || renderers[i].gameObject == iceBlock)
                continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private Material GetIceBlockMaterial()
    {
        return iceMaterial;
    }

    private void ApplyIceBlockMaterial(bool useLevelTwoIce)
    {
        if (iceBlock == null)
            return;

        Renderer iceRenderer = iceBlock.GetComponent<Renderer>();
        if (iceRenderer == null)
            return;

        Material targetMaterial = useLevelTwoIce && levelTwoIceMaterial != null
            ? levelTwoIceMaterial
            : iceMaterial;

        if (targetMaterial != null)
            iceRenderer.sharedMaterial = targetMaterial;
    }

    private void SetAnimatorFrozen(bool frozen)
    {
        if (animator == null)
            return;

        if (frozen)
        {
            animatorSpeed = animator.speed;
            animator.speed = 0f;
            return;
        }

        animator.speed = animatorSpeed;
    }
}
