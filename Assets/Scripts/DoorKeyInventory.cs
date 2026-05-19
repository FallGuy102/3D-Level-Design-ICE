using System.Collections.Generic;
using UnityEngine;

public class DoorKeyInventory : MonoBehaviour
{
    private readonly HashSet<string> keys = new HashSet<string>();

    public bool HasKey(string keyId)
    {
        return string.IsNullOrWhiteSpace(keyId) || keys.Contains(keyId);
    }

    public void AddKey(string keyId)
    {
        if (!string.IsNullOrWhiteSpace(keyId))
            keys.Add(keyId);
    }
}
