using UnityEngine;

public class DoorKeyItem : MonoBehaviour
{
    [SerializeField] private string keyId = "Key01";
    [SerializeField] private string pickupMessage = "获得钥匙";
    [SerializeField] private bool destroyOnPickup = true;

    public string KeyId => keyId;
    public string PickupMessage => pickupMessage;

    public bool TryPickup(GameObject player, out string message)
    {
        message = pickupMessage;

        DoorKeyInventory inventory = player.GetComponent<DoorKeyInventory>();
        if (inventory == null)
            inventory = player.AddComponent<DoorKeyInventory>();

        inventory.AddKey(keyId);

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        TryPickup(other.gameObject, out _);
    }
}
