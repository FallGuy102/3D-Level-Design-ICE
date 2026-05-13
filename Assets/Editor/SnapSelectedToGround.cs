using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class SnapSelectedToGround
{
    private const float RaycastDistance = 5000f;
    private const float RayStartOffset = 0.05f;

    [Shortcut("Tools/_Project/Snap Selected To Ground", KeyCode.End)]
    [MenuItem("Tools/_Project/Snap Selected To Ground %#g")]
    private static void SnapSelection()
    {
        Transform[] targets = GetTopLevelSelection();
        if (targets.Length == 0)
        {
            Debug.Log("Snap To Ground: Select at least one scene object.");
            return;
        }

        Physics.SyncTransforms();

        int snappedCount = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            if (SnapTransform(targets[i]))
            {
                snappedCount++;
            }
        }

        Debug.Log($"Snap To Ground: Snapped {snappedCount}/{targets.Length} selected object(s).");
    }

    private static Transform[] GetTopLevelSelection()
    {
        Transform[] selected = Selection.transforms;
        List<Transform> result = new List<Transform>();

        for (int i = 0; i < selected.Length; i++)
        {
            Transform candidate = selected[i];
            if (candidate == null || EditorUtility.IsPersistent(candidate.gameObject))
            {
                continue;
            }

            bool childOfSelected = false;
            for (int j = 0; j < selected.Length; j++)
            {
                Transform other = selected[j];
                if (other != null && other != candidate && candidate.IsChildOf(other))
                {
                    childOfSelected = true;
                    break;
                }
            }

            if (!childOfSelected)
            {
                result.Add(candidate);
            }
        }

        return result.ToArray();
    }

    private static bool SnapTransform(Transform target)
    {
        Bounds bounds = GetWorldBounds(target);
        Vector3 rayOrigin = new Vector3(bounds.center.x, bounds.min.y + RayStartOffset, bounds.center.z);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, RaycastDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(target))
            {
                continue;
            }

            float deltaY = hits[i].point.y - bounds.min.y;
            Undo.RecordObject(target, "Snap Selected To Ground");
            target.position += Vector3.up * deltaY;
            EditorUtility.SetDirty(target);
            return true;
        }

        return false;
    }

    private static Bounds GetWorldBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        bool hasBounds = false;
        Bounds bounds = new Bounds(target.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            EncapsulateBounds(renderers[i].bounds, ref bounds, ref hasBounds);
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger)
            {
                continue;
            }

            EncapsulateBounds(colliders[i].bounds, ref bounds, ref hasBounds);
        }

        return bounds;
    }

    private static void EncapsulateBounds(Bounds source, ref Bounds target, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            target = source;
            hasBounds = true;
            return;
        }

        target.Encapsulate(source);
    }
}
