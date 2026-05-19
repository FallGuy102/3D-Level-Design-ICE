using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlumbobDiamondMesh : MonoBehaviour
{
    [SerializeField] private float width = 0.55f;
    [SerializeField] private float upperHeight = 0.75f;
    [SerializeField] private float lowerHeight = 0.75f;

    private Mesh generatedMesh;

    private void OnEnable()
    {
        RebuildMesh();
    }

    private void OnValidate()
    {
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        width = Mathf.Max(0.01f, width);
        upperHeight = Mathf.Max(0.01f, upperHeight);
        lowerHeight = Mathf.Max(0.01f, lowerHeight);

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = "Generated Plumbob Diamond",
                hideFlags = HideFlags.DontSave
            };
        }

        float halfWidth = width * 0.5f;
        Vector3 top = Vector3.up * upperHeight;
        Vector3 bottom = Vector3.down * lowerHeight;
        Vector3 right = Vector3.right * halfWidth;
        Vector3 forward = Vector3.forward * halfWidth;
        Vector3 left = Vector3.left * halfWidth;
        Vector3 back = Vector3.back * halfWidth;

        generatedMesh.Clear();
        generatedMesh.vertices = new[]
        {
            top, right, forward,
            top, forward, left,
            top, left, back,
            top, back, right,
            bottom, forward, right,
            bottom, left, forward,
            bottom, back, left,
            bottom, right, back
        };
        generatedMesh.triangles = new[]
        {
            0, 1, 2,
            3, 4, 5,
            6, 7, 8,
            9, 10, 11,
            12, 13, 14,
            15, 16, 17,
            18, 19, 20,
            21, 22, 23
        };
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
            meshFilter.sharedMesh = generatedMesh;
    }
}
