using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Renderer))]
public class WaterFreezeTest : MonoBehaviour
{
    [SerializeField] private Material iceMaterial;
    [SerializeField] private bool startFrozen;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.F;
    [SerializeField] private float frozenHeightOffset = 0.08f;
    [SerializeField] private float frozenThickness = 0.18f;
    [SerializeField] private float thicknessSurfaceGap = 0.025f;
    [SerializeField] private bool createThicknessVisual = true;
    [SerializeField] private float freezeSpreadSpeed = 18f;
    [SerializeField] private float maxFreezeRadius = 80f;
    [SerializeField] private float raycastDistance = 120f;

    private Renderer waterRenderer;
    private MeshFilter waterMeshFilter;
    private GameObject iceSurfaceVisual;
    private GameObject thicknessVisual;
    private Material runtimeIceMaterial;
    private Coroutine freezeRoutine;
    private bool isFrozen;

    private void Awake()
    {
        waterRenderer = GetComponent<Renderer>();
        waterMeshFilter = GetComponent<MeshFilter>();
        CreateIceSurfaceVisualIfNeeded();
        CreateThicknessVisualIfNeeded();
        SetFrozen(startFrozen);
    }

    private void Update()
    {
        if (WasTogglePressed())
        {
            if (isFrozen)
                Thaw();
            else
                FreezeFromAim();
        }
    }

    public void Freeze()
    {
        SetFrozen(true, GetDefaultFreezeCenter());
    }

    public void FreezeFromAim()
    {
        SetFrozen(true, GetAimFreezeCenter());
    }

    public void Thaw()
    {
        SetFrozen(false);
    }

    public void SetFrozen(bool frozen)
    {
        SetFrozen(frozen, GetDefaultFreezeCenter());
    }

    public void SetFrozen(bool frozen, Vector3 freezeCenter)
    {
        if (waterRenderer == null)
            waterRenderer = GetComponent<Renderer>();

        isFrozen = frozen;

        if (isFrozen)
        {
            if (iceMaterial == null)
            {
                Debug.LogWarning($"{name} cannot freeze because no ice material is assigned.", this);
                return;
            }

            if (runtimeIceMaterial == null)
                runtimeIceMaterial = new Material(iceMaterial);

            runtimeIceMaterial.SetVector("_FreezeCenter", freezeCenter);
            runtimeIceMaterial.SetFloat("_FreezeRadius", 0f);

            SetIceSurfaceVisualActive(true);
            SetThicknessVisualActive(true);

            StartFreezeSpread(freezeCenter);
            return;
        }

        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
            freezeRoutine = null;
        }

        SetIceSurfaceVisualActive(false);
        SetThicknessVisualActive(false);
    }

    private void StartFreezeSpread(Vector3 freezeCenter)
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(AnimateFreezeSpread(freezeCenter));
    }

    private System.Collections.IEnumerator AnimateFreezeSpread(Vector3 freezeCenter)
    {
        float radius = 0f;
        while (radius < maxFreezeRadius)
        {
            radius += freezeSpreadSpeed * Time.deltaTime;
            ApplyFreezeRadius(freezeCenter, radius);
            yield return null;
        }

        ApplyFreezeRadius(freezeCenter, maxFreezeRadius);
        freezeRoutine = null;
    }

    private void ApplyFreezeRadius(Vector3 freezeCenter, float radius)
    {
        if (runtimeIceMaterial != null)
        {
            runtimeIceMaterial.SetVector("_FreezeCenter", freezeCenter);
            runtimeIceMaterial.SetFloat("_FreezeRadius", radius);
        }

        if (iceSurfaceVisual != null)
        {
            Renderer iceRenderer = iceSurfaceVisual.GetComponent<Renderer>();
            if (iceRenderer != null && runtimeIceMaterial != null)
                iceRenderer.sharedMaterial = runtimeIceMaterial;
        }

        if (thicknessVisual != null)
        {
            Renderer thicknessRenderer = thicknessVisual.GetComponent<Renderer>();
            if (thicknessRenderer != null && runtimeIceMaterial != null)
                thicknessRenderer.sharedMaterial = runtimeIceMaterial;
        }
    }

    private Vector3 GetAimFreezeCenter()
    {
        Camera aimCamera = Camera.main;
        if (aimCamera == null)
            return GetDefaultFreezeCenter();

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance) && hit.transform == transform)
            return hit.point;

        Plane waterPlane = new Plane(transform.up, transform.position);
        if (waterPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return GetDefaultFreezeCenter();
    }

    private Vector3 GetDefaultFreezeCenter()
    {
        Bounds bounds = waterRenderer != null ? waterRenderer.bounds : new Bounds(transform.position, Vector3.one);
        return bounds.center;
    }

    private void CreateIceSurfaceVisualIfNeeded()
    {
        if (iceSurfaceVisual != null)
            return;

        iceSurfaceVisual = new GameObject("IceSurfaceVisual");
        iceSurfaceVisual.transform.SetParent(transform, false);
        iceSurfaceVisual.transform.localPosition = Vector3.up * frozenHeightOffset;
        iceSurfaceVisual.transform.localRotation = Quaternion.identity;
        iceSurfaceVisual.transform.localScale = Vector3.one;

        MeshFilter sourceMeshFilter = waterMeshFilter != null ? waterMeshFilter : GetComponent<MeshFilter>();
        MeshFilter iceMeshFilter = iceSurfaceVisual.AddComponent<MeshFilter>();
        iceMeshFilter.sharedMesh = sourceMeshFilter != null ? sourceMeshFilter.sharedMesh : null;

        MeshRenderer iceRenderer = iceSurfaceVisual.AddComponent<MeshRenderer>();
        if (iceMaterial != null)
            iceRenderer.sharedMaterial = iceMaterial;

        iceSurfaceVisual.SetActive(false);
    }

    private void SetIceSurfaceVisualActive(bool active)
    {
        CreateIceSurfaceVisualIfNeeded();

        if (iceSurfaceVisual == null)
            return;

        iceSurfaceVisual.transform.localPosition = Vector3.up * frozenHeightOffset;
        iceSurfaceVisual.SetActive(active);
    }

    private void CreateThicknessVisualIfNeeded()
    {
        if (!createThicknessVisual || thicknessVisual != null)
            return;

        MeshFilter meshFilter = waterMeshFilter != null ? waterMeshFilter : GetComponent<MeshFilter>();
        Vector3 size = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.bounds.size
            : new Vector3(1f, 0f, 1f);

        thicknessVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        thicknessVisual.name = "IceThicknessVisual";
        thicknessVisual.transform.SetParent(transform, false);
        thicknessVisual.transform.localPosition = new Vector3(0f, frozenHeightOffset - frozenThickness * 0.5f - thicknessSurfaceGap, 0f);
        thicknessVisual.transform.localRotation = Quaternion.identity;
        thicknessVisual.transform.localScale = new Vector3(size.x, frozenThickness, size.z);

        Renderer thicknessRenderer = thicknessVisual.GetComponent<Renderer>();
        if (thicknessRenderer != null && iceMaterial != null)
            thicknessRenderer.sharedMaterial = iceMaterial;

        Collider thicknessCollider = thicknessVisual.GetComponent<Collider>();
        if (thicknessCollider != null)
            thicknessCollider.enabled = false;

        thicknessVisual.SetActive(false);
    }

    private void SetThicknessVisualActive(bool active)
    {
        CreateThicknessVisualIfNeeded();

        if (thicknessVisual == null)
            return;

        thicknessVisual.transform.localPosition = new Vector3(0f, frozenHeightOffset - frozenThickness * 0.5f - thicknessSurfaceGap, 0f);
        thicknessVisual.transform.localScale = new Vector3(thicknessVisual.transform.localScale.x, frozenThickness, thicknessVisual.transform.localScale.z);
        thicknessVisual.SetActive(active);
    }

    private bool WasTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(legacyToggleKey))
            return true;
#endif

        return false;
    }
}
