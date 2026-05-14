using UnityEngine;

[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Camera))]
public class WaterReflection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to use the scene camera tagged MainCamera. With Cinemachine, this should be the real camera that has CinemachineBrain.")]
    public Camera mainCamera;
    [Tooltip("The plane where the camera will be reflected, the water plane or any object with the same position and rotation")]
    public Transform reflectionPlane;
    [Tooltip("The texture used by the Water shader to display the reflection")]
    public RenderTexture outputTexture;

    [Header("Parameters")]
    public bool copyCameraParamerers;
    public float verticalOffset;

    Camera reflectionCamera;
    private bool isReady;

    private Transform mainCamTransform;
    private Transform reflectionCamTransform;

    private void Awake()
    {
        CacheReferences();
        CopyCameraParametersIfNeeded();
    }

    private void OnEnable()
    {
        CacheReferences();
        CopyCameraParametersIfNeeded();
    }

    private void LateUpdate()
    {
        CacheReferences();
        ConfigureReflectionCameraOutput();

        if (isReady)
        {
            SyncCameraSettings();
            RenderReflection();
        }
    }

    private void CacheReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (reflectionCamera == null)
            reflectionCamera = GetComponent<Camera>();

        mainCamTransform = mainCamera != null ? mainCamera.transform : null;
        reflectionCamTransform = reflectionCamera != null ? reflectionCamera.transform : null;
        isReady = mainCamTransform != null && reflectionCamTransform != null && reflectionPlane != null;
    }

    private void ConfigureReflectionCameraOutput()
    {
        if (reflectionCamera == null)
            return;

        if (outputTexture == null)
            outputTexture = reflectionCamera.targetTexture;

        reflectionCamera.targetTexture = outputTexture;
        reflectionCamera.enabled = outputTexture != null;
    }

    private void SyncCameraSettings()
    {
        if (mainCamera == null || reflectionCamera == null)
            return;

        reflectionCamera.clearFlags = mainCamera.clearFlags;
        reflectionCamera.backgroundColor = mainCamera.backgroundColor;
        reflectionCamera.fieldOfView = mainCamera.fieldOfView;
        reflectionCamera.orthographic = mainCamera.orthographic;
        reflectionCamera.orthographicSize = mainCamera.orthographicSize;
        reflectionCamera.nearClipPlane = mainCamera.nearClipPlane;
        reflectionCamera.farClipPlane = mainCamera.farClipPlane;
        reflectionCamera.aspect = mainCamera.aspect;
        reflectionCamera.cullingMask = mainCamera.cullingMask;
    }

    private void RenderReflection()
    {
        // take main camera directions and position world space
        Vector3 cameraDirectionWorldSpace = mainCamTransform.forward;
        Vector3 cameraUpWorldSpace = mainCamTransform.up;
        Vector3 cameraPositionWorldSpace = mainCamTransform.position;

        cameraPositionWorldSpace.y += verticalOffset;

        // transform direction and position by reflection plane
        Vector3 cameraDirectionPlaneSpace = reflectionPlane.InverseTransformDirection(cameraDirectionWorldSpace);
        Vector3 cameraUpPlaneSpace = reflectionPlane.InverseTransformDirection(cameraUpWorldSpace);
        Vector3 cameraPositionPlaneSpace = reflectionPlane.InverseTransformPoint(cameraPositionWorldSpace);

        // invert direction and position by reflection plane
        cameraDirectionPlaneSpace.y *= -1;
        cameraUpPlaneSpace.y *= -1;
        cameraPositionPlaneSpace.y *= -1;

        // transform direction and position from reflection plane local space to world space
        cameraDirectionWorldSpace = reflectionPlane.TransformDirection(cameraDirectionPlaneSpace);
        cameraUpWorldSpace = reflectionPlane.TransformDirection(cameraUpPlaneSpace);
        cameraPositionWorldSpace = reflectionPlane.TransformPoint(cameraPositionPlaneSpace);

        // apply direction and position to reflection camera
        reflectionCamTransform.position = cameraPositionWorldSpace;
        reflectionCamTransform.LookAt(cameraPositionWorldSpace + cameraDirectionWorldSpace, cameraUpWorldSpace);
    }

    private void CopyCameraParametersIfNeeded()
    {
        if (mainCamera != null && reflectionCamera != null && copyCameraParamerers)
        {
            copyCameraParamerers = !copyCameraParamerers;
            reflectionCamera.CopyFrom(mainCamera);

            ConfigureReflectionCameraOutput();
        }
    }
}
