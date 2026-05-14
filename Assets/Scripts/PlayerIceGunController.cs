using UnityEngine;
using Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Animator))]
public class PlayerIceGunController : MonoBehaviour
{
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");

    [Header("Input")]
    [SerializeField] private bool useRightMouseToAim = true;

    [Header("Animation")]
    [SerializeField] private string upperBodyAimLayerName = "UpperBodyAim";
    [SerializeField] private float aimBlendSpeed = 10f;

    [Header("Aim")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float aimRange = 80f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Fire")]
    [SerializeField] private bool requireAimToFire = true;
    [SerializeField] private float fireCooldown = 0.18f;
    [SerializeField] private Color impactColor = new Color(0.45f, 0.9f, 1f, 1f);
    [SerializeField] private float impactVfxLifetime = 1.2f;

    [Header("Weapon Visual")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private string weaponSocketName = "Right_Hand";
    [SerializeField] private Vector3 weaponLocalPosition = new Vector3(0.03f, 0.02f, 0.08f);
    [SerializeField] private Vector3 weaponLocalEulerAngles = new Vector3(0f, 90f, 90f);

    [Header("Aim Camera")]
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private Vector3 aimingShoulderOffset = new Vector3(0.9f, 0.12f, 0.1f);
    [SerializeField] private float aimingCameraSide = 0.68f;
    [SerializeField] private float aimingCameraDistance = 2.6f;
    [SerializeField] private float aimingFieldOfView = 34f;
    [SerializeField] private float cameraBlendSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool drawAimRay = true;

    private Animator animator;
    private int upperBodyAimLayerIndex = -1;
    private float aimWeight;
    private bool isAiming;
    private Ray currentAimRay;
    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private Vector3 defaultShoulderOffset;
    private float defaultCameraSide;
    private float defaultCameraDistance;
    private float defaultFieldOfView;
    private GameObject weaponInstance;
    private float nextFireTime;
    private Material impactVfxMaterial;

    public bool IsAiming => isAiming;
    public Ray CurrentAimRay => currentAimRay;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        upperBodyAimLayerIndex = animator.GetLayerIndex(upperBodyAimLayerName);

        if (aimCamera == null)
            aimCamera = Camera.main;

        CacheAimCamera();
        CacheWeaponSocket();
        CreateWeaponInstance();
    }

    private void Update()
    {
        ReadInput();
        UpdateAimRay();
        UpdateAnimator();
        UpdateAimCamera();
        UpdateWeaponVisibility();
        TryFire();
    }

    private void ReadInput()
    {
        isAiming = useRightMouseToAim && IsRightMouseHeld();
    }

    private void UpdateAimRay()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        currentAimRay = aimCamera != null
            ? aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        if (drawAimRay && isAiming)
            Debug.DrawRay(currentAimRay.origin, currentAimRay.direction * aimRange, Color.cyan);
    }

    private void UpdateAnimator()
    {
        animator.SetBool(IsAimingHash, isAiming);

        if (upperBodyAimLayerIndex < 0)
            return;

        float targetWeight = isAiming ? 1f : 0f;
        aimWeight = Mathf.MoveTowards(aimWeight, targetWeight, aimBlendSpeed * Time.deltaTime);
        animator.SetLayerWeight(upperBodyAimLayerIndex, aimWeight);
    }

    public bool TryGetAimHit(out RaycastHit hit)
    {
        return Physics.Raycast(currentAimRay, out hit, aimRange, aimMask, QueryTriggerInteraction.Collide);
    }

    private void TryFire()
    {
        if (requireAimToFire && !isAiming)
            return;

        if (!WasFirePressed() || Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireCooldown;

        if (TryGetAimHit(out RaycastHit hit))
            SpawnImpactVfx(hit.point, hit.normal);
        else
            Debug.DrawRay(currentAimRay.origin, currentAimRay.direction * aimRange, impactColor, 0.2f);
    }

    private void SpawnImpactVfx(Vector3 position, Vector3 normal)
    {
        GameObject vfxObject = new GameObject("IceGunImpactVFX");
        vfxObject.transform.position = position + normal * 0.02f;
        vfxObject.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem particles = vfxObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.18f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
        main.startColor = impactColor;
        main.gravityModifier = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 22)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.025f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(impactColor, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetImpactVfxMaterial();

        particles.Play();
        Destroy(vfxObject, impactVfxLifetime);
    }

    private Material GetImpactVfxMaterial()
    {
        if (impactVfxMaterial != null)
            return impactVfxMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        impactVfxMaterial = new Material(shader);
        impactVfxMaterial.name = "Runtime Ice Gun Impact VFX";
        impactVfxMaterial.color = impactColor;
        return impactVfxMaterial;
    }

    private void CacheAimCamera()
    {
        if (aimVirtualCamera == null)
            aimVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (aimVirtualCamera == null)
            return;

        thirdPersonFollow = aimVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        defaultFieldOfView = aimVirtualCamera.m_Lens.FieldOfView;

        if (thirdPersonFollow == null)
            return;

        defaultShoulderOffset = thirdPersonFollow.ShoulderOffset;
        defaultCameraSide = thirdPersonFollow.CameraSide;
        defaultCameraDistance = thirdPersonFollow.CameraDistance;
    }

    private void CacheWeaponSocket()
    {
        if (weaponSocket != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == weaponSocketName)
            {
                weaponSocket = children[i];
                return;
            }
        }
    }

    private void CreateWeaponInstance()
    {
        if (weaponInstance != null)
            return;

        if (weaponPrefab == null)
        {
            Debug.LogWarning($"{name} cannot show ice gun because Weapon Prefab is not assigned.", this);
            return;
        }

        if (weaponSocket == null)
        {
            Debug.LogWarning($"{name} cannot show ice gun because weapon socket '{weaponSocketName}' was not found.", this);
            return;
        }

        weaponInstance = GameObject.Instantiate(weaponPrefab);
        weaponInstance.name = weaponPrefab.name;
        weaponInstance.transform.SetParent(weaponSocket, false);
        weaponInstance.transform.localPosition = weaponLocalPosition;
        weaponInstance.transform.localRotation = Quaternion.Euler(weaponLocalEulerAngles);
        weaponInstance.SetActive(false);
    }

    private void UpdateWeaponVisibility()
    {
        if (weaponInstance == null)
            return;

        if (weaponInstance.activeSelf != isAiming)
            weaponInstance.SetActive(isAiming);
    }

    private void UpdateAimCamera()
    {
        if (aimVirtualCamera == null || thirdPersonFollow == null)
            return;

        float t = cameraBlendSpeed * Time.deltaTime;
        Vector3 targetShoulderOffset = isAiming ? aimingShoulderOffset : defaultShoulderOffset;
        float targetCameraSide = isAiming ? aimingCameraSide : defaultCameraSide;
        float targetCameraDistance = isAiming ? aimingCameraDistance : defaultCameraDistance;
        float targetFieldOfView = isAiming ? aimingFieldOfView : defaultFieldOfView;

        thirdPersonFollow.ShoulderOffset = Vector3.Lerp(thirdPersonFollow.ShoulderOffset, targetShoulderOffset, t);
        thirdPersonFollow.CameraSide = Mathf.Lerp(thirdPersonFollow.CameraSide, targetCameraSide, t);
        thirdPersonFollow.CameraDistance = Mathf.Lerp(thirdPersonFollow.CameraDistance, targetCameraDistance, t);
        aimVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(aimVirtualCamera.m_Lens.FieldOfView, targetFieldOfView, t);
    }

    private bool IsRightMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(1))
            return true;
#endif

        return false;
    }

    private bool WasFirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
            return true;
#endif

        return false;
    }
}
