using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Renderer))]
public class WaterFreezeTest : MonoBehaviour
{
    [SerializeField] private Material iceMaterial;
    [SerializeField] private Material levelTwoIceMaterial;
    [SerializeField] private Material levelTwoReflectionMaterial;
    [SerializeField] private bool startFrozen;
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.F;
    [SerializeField] private float frozenHeightOffset = 0.08f;
    [SerializeField] private float frozenThickness = 0.18f;
    [SerializeField] private float thicknessSurfaceGap = 0.025f;
    [SerializeField] private bool createThicknessVisual = true;
    [SerializeField] private float freezeSpreadSpeed = 18f;
    [SerializeField] private float maxFreezeRadius = 80f;
    [SerializeField] private float levelTwoHeightOffset = 0.14f;
    [SerializeField] private float levelTwoReflectionHeightOffset = 0.02f;
    [SerializeField] private float raycastDistance = 120f;

    private Renderer waterRenderer;
    private MeshFilter waterMeshFilter;
    private GameObject iceSurfaceVisual;
    private GameObject thicknessVisual;
    private GameObject levelTwoSurfaceVisual;
    private GameObject levelTwoThicknessVisual;
    private GameObject levelTwoReflectionVisual;
    private Material runtimeIceMaterial;
    private Material runtimeLevelTwoIceMaterial;
    private Coroutine freezeRoutine;
    private Coroutine levelTwoFreezeRoutine;
    private bool isFrozen;
    private bool isLevelTwoFrozen;

    public bool IsFrozen => isFrozen;

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
                FreezeLevelTwoFromAim();
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

    public void FreezeLevelTwoFromAim()
    {
        SetFrozenLevelTwo(true, GetAimFreezeCenter());
    }

    public void Thaw()
    {
        SetFrozen(false);
        SetFrozenLevelTwo(false, GetDefaultFreezeCenter());
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

    public void SetFrozenLevelTwo(bool frozen, Vector3 freezeCenter)
    {
        if (waterRenderer == null)
            waterRenderer = GetComponent<Renderer>();

        isFrozen = frozen;
        isLevelTwoFrozen = frozen;

        if (isLevelTwoFrozen)
        {
            Material baseMaterial = levelTwoIceMaterial != null ? levelTwoIceMaterial : iceMaterial;
            if (baseMaterial == null)
            {
                Debug.LogWarning($"{name} cannot freeze level two because no ice material is assigned.", this);
                return;
            }

            if (runtimeLevelTwoIceMaterial == null)
                runtimeLevelTwoIceMaterial = new Material(baseMaterial);

            runtimeLevelTwoIceMaterial.SetVector("_FreezeCenter", freezeCenter);
            runtimeLevelTwoIceMaterial.SetFloat("_FreezeRadius", 0f);

            SetLevelTwoSurfaceVisualActive(true);
            SetLevelTwoThicknessVisualActive(true);
            SetLevelTwoReflectionVisualActive(levelTwoReflectionMaterial != null);

            StartLevelTwoFreezeSpread(freezeCenter);
            return;
        }

        if (levelTwoFreezeRoutine != null)
        {
            StopCoroutine(levelTwoFreezeRoutine);
            levelTwoFreezeRoutine = null;
        }

        SetLevelTwoSurfaceVisualActive(false);
        SetLevelTwoThicknessVisualActive(false);
        SetLevelTwoReflectionVisualActive(false);
    }

    private void StartFreezeSpread(Vector3 freezeCenter)
    {
        if (freezeRoutine != null)
            StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(AnimateFreezeSpread(freezeCenter));
    }

    private void StartLevelTwoFreezeSpread(Vector3 freezeCenter)
    {
        if (levelTwoFreezeRoutine != null)
            StopCoroutine(levelTwoFreezeRoutine);

        levelTwoFreezeRoutine = StartCoroutine(AnimateLevelTwoFreezeSpread(freezeCenter));
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

    private System.Collections.IEnumerator AnimateLevelTwoFreezeSpread(Vector3 freezeCenter)
    {
        float radius = 0f;
        while (radius < maxFreezeRadius)
        {
            radius += freezeSpreadSpeed * Time.deltaTime;
            ApplyLevelTwoFreezeRadius(freezeCenter, radius);
            yield return null;
        }

        ApplyLevelTwoFreezeRadius(freezeCenter, maxFreezeRadius);
        levelTwoFreezeRoutine = null;
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

    private void ApplyLevelTwoFreezeRadius(Vector3 freezeCenter, float radius)
    {
        if (runtimeLevelTwoIceMaterial != null)
        {
            runtimeLevelTwoIceMaterial.SetVector("_FreezeCenter", freezeCenter);
            runtimeLevelTwoIceMaterial.SetFloat("_FreezeRadius", radius);
        }

        if (levelTwoSurfaceVisual != null)
        {
            Renderer iceRenderer = levelTwoSurfaceVisual.GetComponent<Renderer>();
            if (iceRenderer != null && runtimeLevelTwoIceMaterial != null)
                iceRenderer.sharedMaterial = runtimeLevelTwoIceMaterial;
        }

        if (levelTwoThicknessVisual != null)
        {
            Renderer thicknessRenderer = levelTwoThicknessVisual.GetComponent<Renderer>();
            if (thicknessRenderer != null && runtimeLevelTwoIceMaterial != null)
                thicknessRenderer.sharedMaterial = runtimeLevelTwoIceMaterial;
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

    private void CreateLevelTwoSurfaceVisualIfNeeded()
    {
        if (levelTwoSurfaceVisual != null)
            return;

        levelTwoSurfaceVisual = new GameObject("LevelTwoIceSurfaceVisual");
        levelTwoSurfaceVisual.transform.SetParent(transform, false);
        levelTwoSurfaceVisual.transform.localPosition = Vector3.up * levelTwoHeightOffset;
        levelTwoSurfaceVisual.transform.localRotation = Quaternion.identity;
        levelTwoSurfaceVisual.transform.localScale = Vector3.one;

        MeshFilter sourceMeshFilter = waterMeshFilter != null ? waterMeshFilter : GetComponent<MeshFilter>();
        MeshFilter iceMeshFilter = levelTwoSurfaceVisual.AddComponent<MeshFilter>();
        iceMeshFilter.sharedMesh = sourceMeshFilter != null ? sourceMeshFilter.sharedMesh : null;

        MeshRenderer iceRenderer = levelTwoSurfaceVisual.AddComponent<MeshRenderer>();
        Material baseMaterial = levelTwoIceMaterial != null ? levelTwoIceMaterial : iceMaterial;
        if (baseMaterial != null)
            iceRenderer.sharedMaterial = baseMaterial;

        levelTwoSurfaceVisual.SetActive(false);
    }

    private void SetLevelTwoSurfaceVisualActive(bool active)
    {
        if (!active && levelTwoSurfaceVisual == null)
            return;

        CreateLevelTwoSurfaceVisualIfNeeded();

        if (levelTwoSurfaceVisual == null)
            return;

        levelTwoSurfaceVisual.transform.localPosition = Vector3.up * levelTwoHeightOffset;
        levelTwoSurfaceVisual.SetActive(active);
    }

    private void CreateLevelTwoThicknessVisualIfNeeded()
    {
        if (!createThicknessVisual || levelTwoThicknessVisual != null)
            return;

        MeshFilter meshFilter = waterMeshFilter != null ? waterMeshFilter : GetComponent<MeshFilter>();
        Vector3 size = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.bounds.size
            : new Vector3(1f, 0f, 1f);

        levelTwoThicknessVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelTwoThicknessVisual.name = "LevelTwoIceThicknessVisual";
        levelTwoThicknessVisual.transform.SetParent(transform, false);
        levelTwoThicknessVisual.transform.localPosition = new Vector3(0f, levelTwoHeightOffset - frozenThickness * 0.5f - thicknessSurfaceGap, 0f);
        levelTwoThicknessVisual.transform.localRotation = Quaternion.identity;
        levelTwoThicknessVisual.transform.localScale = new Vector3(size.x, frozenThickness, size.z);

        Renderer thicknessRenderer = levelTwoThicknessVisual.GetComponent<Renderer>();
        Material baseMaterial = levelTwoIceMaterial != null ? levelTwoIceMaterial : iceMaterial;
        if (thicknessRenderer != null && baseMaterial != null)
            thicknessRenderer.sharedMaterial = baseMaterial;

        Collider thicknessCollider = levelTwoThicknessVisual.GetComponent<Collider>();
        if (thicknessCollider != null)
            thicknessCollider.enabled = false;

        levelTwoThicknessVisual.SetActive(false);
    }

    private void SetLevelTwoThicknessVisualActive(bool active)
    {
        if (!active && levelTwoThicknessVisual == null)
            return;

        CreateLevelTwoThicknessVisualIfNeeded();

        if (levelTwoThicknessVisual == null)
            return;

        levelTwoThicknessVisual.transform.localPosition = new Vector3(0f, levelTwoHeightOffset - frozenThickness * 0.5f - thicknessSurfaceGap, 0f);
        levelTwoThicknessVisual.transform.localScale = new Vector3(levelTwoThicknessVisual.transform.localScale.x, frozenThickness, levelTwoThicknessVisual.transform.localScale.z);
        levelTwoThicknessVisual.SetActive(active);
    }

    private void CreateLevelTwoReflectionVisualIfNeeded()
    {
        if (levelTwoReflectionVisual != null)
            return;

        levelTwoReflectionVisual = new GameObject("LevelTwoIceReflectionVisual");
        levelTwoReflectionVisual.transform.SetParent(transform, false);
        levelTwoReflectionVisual.transform.localPosition = Vector3.up * (levelTwoHeightOffset + levelTwoReflectionHeightOffset);
        levelTwoReflectionVisual.transform.localRotation = Quaternion.identity;
        levelTwoReflectionVisual.transform.localScale = Vector3.one;

        MeshFilter sourceMeshFilter = waterMeshFilter != null ? waterMeshFilter : GetComponent<MeshFilter>();
        MeshFilter reflectionMeshFilter = levelTwoReflectionVisual.AddComponent<MeshFilter>();
        reflectionMeshFilter.sharedMesh = sourceMeshFilter != null ? sourceMeshFilter.sharedMesh : null;

        MeshRenderer reflectionRenderer = levelTwoReflectionVisual.AddComponent<MeshRenderer>();
        if (levelTwoReflectionMaterial != null)
            reflectionRenderer.sharedMaterial = levelTwoReflectionMaterial;

        levelTwoReflectionVisual.SetActive(false);
    }

    private void SetLevelTwoReflectionVisualActive(bool active)
    {
        if (!active && levelTwoReflectionVisual == null)
            return;

        CreateLevelTwoReflectionVisualIfNeeded();

        if (levelTwoReflectionVisual == null)
            return;

        levelTwoReflectionVisual.transform.localPosition = Vector3.up * (levelTwoHeightOffset + levelTwoReflectionHeightOffset);
        levelTwoReflectionVisual.SetActive(active);
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
