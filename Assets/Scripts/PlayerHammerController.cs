using System.Collections;
using UnityEngine;
using Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerHammerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool useLeftMouseToShatter = true;
    [SerializeField] private bool blockWhileIceGunAiming = true;
    [SerializeField] private KeyCode legacyShatterKey = KeyCode.Mouse0;

    [Header("Aim")]
    [SerializeField] private PlayerIceGunController iceGunController;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float shatterRange = 9f;
    [SerializeField] private LayerMask shatterMask = ~0;

    [Header("VFX")]
    [SerializeField] private Material iceShardMaterial;
    [SerializeField] private Color shatterColor = new Color(0.7f, 0.95f, 1f, 1f);
    [SerializeField] private float shatterVfxLifetime = 1.4f;
    [SerializeField] private int shardCount = 14;
    [SerializeField] private Vector2 shardSizeRange = new Vector2(0.05f, 0.16f);
    [SerializeField] private Vector2 shardSpeedRange = new Vector2(2.2f, 5.8f);
    [SerializeField] private float shardLifetime = 1.1f;

    [Header("Camera Feedback")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float shakeAmplitude = 2.2f;
    [SerializeField] private float shakeFrequency = 18f;
    [SerializeField] private float shakeDuration = 0.16f;

    private CinemachineBasicMultiChannelPerlin cameraNoise;
    private Material shatterVfxMaterial;
    private Coroutine shakeRoutine;
    private bool isLookingAtBreakableIce;

    public bool IsLookingAtBreakableIce => isLookingAtBreakableIce;

    private void Awake()
    {
        if (iceGunController == null)
            iceGunController = GetComponent<PlayerIceGunController>();

        if (aimCamera == null)
            aimCamera = Camera.main;

        CacheCameraNoise();
    }

    private void Update()
    {
        if (blockWhileIceGunAiming && iceGunController != null && iceGunController.IsAiming)
        {
            isLookingAtBreakableIce = false;
            return;
        }

        isLookingAtBreakableIce = TryGetBreakableIce(out _);

        if (WasShatterPressed() && TryGetBreakableIce(out RaycastHit hit))
            Shatter(hit);
    }

    private bool TryGetBreakableIce(out RaycastHit hit)
    {
        Ray ray = GetAimRay();
        RaycastHit[] hits = Physics.RaycastAll(ray, shatterRange, shatterMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].transform.IsChildOf(transform))
                continue;

            WaterFreezeTest waterFreeze = hits[i].collider.GetComponentInParent<WaterFreezeTest>();
            if (waterFreeze != null && waterFreeze.IsFrozen)
            {
                hit = hits[i];
                return true;
            }

            EnemyFreezeController enemyFreeze = hits[i].collider.GetComponentInParent<EnemyFreezeController>();
            if (enemyFreeze != null && enemyFreeze.IsFrozen)
            {
                hit = hits[i];
                return true;
            }

            break;
        }

        hit = default;
        return false;
    }

    private void Shatter(RaycastHit hit)
    {
        SpawnShatterVfx(hit.point, hit.normal);
        SpawnIceShards(hit.point, hit.normal);
        ShakeCamera();

        WaterFreezeTest waterFreeze = hit.collider.GetComponentInParent<WaterFreezeTest>();
        if (waterFreeze != null && waterFreeze.IsFrozen)
        {
            waterFreeze.Thaw();
            return;
        }

        EnemyFreezeController enemyFreeze = hit.collider.GetComponentInParent<EnemyFreezeController>();
        if (enemyFreeze != null && enemyFreeze.IsFrozen)
            enemyFreeze.Shatter();
    }

    private Ray GetAimRay()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera != null)
            return aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (iceGunController != null)
            return iceGunController.CurrentAimRay;

        return new Ray(transform.position + Vector3.up * 1.5f, transform.forward);
    }

    private void SpawnShatterVfx(Vector3 position, Vector3 normal)
    {
        GameObject vfxObject = new GameObject("HammerShatterVFX");
        vfxObject.transform.position = position + normal * 0.04f;
        vfxObject.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem particles = vfxObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.22f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
        main.startColor = shatterColor;
        main.gravityModifier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 42)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 58f;
        shape.radius = 0.18f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(shatterColor, 0.35f),
                new GradientColorKey(new Color(0.25f, 0.75f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetShatterVfxMaterial();

        particles.Play();
        Destroy(vfxObject, shatterVfxLifetime);
    }

    private void SpawnIceShards(Vector3 position, Vector3 normal)
    {
        Material shardMaterial = iceShardMaterial != null ? iceShardMaterial : GetShatterVfxMaterial();

        for (int i = 0; i < shardCount; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "HammerIceShard";
            shard.transform.position = position + normal * 0.08f;
            shard.transform.rotation = Random.rotation;

            float size = Random.Range(shardSizeRange.x, shardSizeRange.y);
            shard.transform.localScale = new Vector3(size * Random.Range(0.55f, 1.2f), size * Random.Range(0.35f, 0.75f), size * Random.Range(0.7f, 1.45f));

            Renderer shardRenderer = shard.GetComponent<Renderer>();
            if (shardRenderer != null)
                shardRenderer.sharedMaterial = shardMaterial;

            Collider shardCollider = shard.GetComponent<Collider>();
            if (shardCollider != null)
                shardCollider.enabled = false;

            Rigidbody body = shard.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.mass = 0.04f;
            body.drag = 0.2f;
            body.angularDrag = 0.05f;

            Vector3 tangent = Vector3.ProjectOnPlane(Random.onUnitSphere, normal).normalized;
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.right;

            Vector3 direction = (normal * Random.Range(0.45f, 1.15f) + tangent * Random.Range(0.2f, 1f)).normalized;
            body.AddForce(direction * Random.Range(shardSpeedRange.x, shardSpeedRange.y), ForceMode.VelocityChange);
            body.AddTorque(Random.onUnitSphere * Random.Range(3f, 9f), ForceMode.VelocityChange);

            Destroy(shard, shardLifetime);
        }
    }

    private Material GetShatterVfxMaterial()
    {
        if (shatterVfxMaterial != null)
            return shatterVfxMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        shatterVfxMaterial = new Material(shader);
        shatterVfxMaterial.name = "Runtime Hammer Shatter VFX";
        shatterVfxMaterial.color = shatterColor;
        return shatterVfxMaterial;
    }

    private void CacheCameraNoise()
    {
        if (virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera == null)
            return;

        cameraNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (cameraNoise == null)
            cameraNoise = virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    private void ShakeCamera()
    {
        if (cameraNoise == null)
            CacheCameraNoise();

        if (cameraNoise == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeCameraRoutine());
    }

    private IEnumerator ShakeCameraRoutine()
    {
        float startAmplitude = cameraNoise.m_AmplitudeGain;
        float startFrequency = cameraNoise.m_FrequencyGain;
        float elapsed = 0f;

        cameraNoise.m_AmplitudeGain = shakeAmplitude;
        cameraNoise.m_FrequencyGain = shakeFrequency;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            cameraNoise.m_AmplitudeGain = Mathf.Lerp(startAmplitude, shakeAmplitude, t);
            cameraNoise.m_FrequencyGain = Mathf.Lerp(startFrequency, shakeFrequency, t);
            yield return null;
        }

        cameraNoise.m_AmplitudeGain = startAmplitude;
        cameraNoise.m_FrequencyGain = startFrequency;
        shakeRoutine = null;
    }

    private bool WasShatterPressed()
    {
        if (blockWhileIceGunAiming && iceGunController != null && iceGunController.IsAiming)
            return false;

#if ENABLE_INPUT_SYSTEM
        if (useLeftMouseToShatter && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (!useLeftMouseToShatter && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (useLeftMouseToShatter && Input.GetMouseButtonDown(0))
            return true;

        if (!useLeftMouseToShatter && Input.GetKeyDown(legacyShatterKey))
            return true;
#endif

        return false;
    }
}
