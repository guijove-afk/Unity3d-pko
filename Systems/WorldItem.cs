using UnityEngine;
using Mirror;

public class WorldItem : NetworkBehaviour
{
    [SyncVar] private string itemId;
    [SyncVar] private int quantity;
    [SyncVar] private float despawnTime;

    private ItemData itemData;
    private GameObject visualModel;
    private float spawnTime;
    private bool isPickedUp;

    public string ItemId => itemId;
    public int Quantity => quantity;

    [Server]
    public void Initialize(ItemData item, int qty, float despawnDuration = 120f)
    {
        itemId = item.itemId;
        quantity = qty;
        despawnTime = Time.time + despawnDuration;
        spawnTime = Time.time;

        RpcCreateVisual(item.itemId);
    }

    [ClientRpc]
    private void RpcCreateVisual(string id)
    {
        itemData = ItemDatabase.Instance?.GetItem(id);
        if (itemData == null) return;

        if (itemData.worldModelPrefab != null)
        {
            visualModel = Instantiate(itemData.worldModelPrefab, transform);
            visualModel.transform.localRotation = Quaternion.Euler(itemData.dropRotation);
            visualModel.transform.localScale = Vector3.one * itemData.dropScale;
        }

        if (GetComponent<Collider>() == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        StartCoroutine(FloatAnimation());
    }

    private System.Collections.IEnumerator FloatAnimation()
    {
        float offset = Random.Range(0f, Mathf.PI * 2f);

        while (visualModel != null)
        {
            float y = Mathf.Sin(Time.time * 2f + offset) * 0.2f;
            visualModel.transform.localPosition = new Vector3(0, y, 0);
            visualModel.transform.Rotate(Vector3.up, 50f * Time.deltaTime);
            yield return null;
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (Time.time >= despawnTime && !isPickedUp)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [Server]
    public void Pickup(PlayerInventory inventory)
    {
        if (isPickedUp) return;
        if (inventory == null) return;

        isPickedUp = true;

        if (inventory.AddItem(itemId, quantity))
        {
            RpcPickupSuccess();
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            isPickedUp = false;
            TargetInventoryFull(inventory.connectionToClient);
        }
    }

    [ClientRpc]
    private void RpcPickupSuccess()
    {
        if (itemData != null)
        {
            DamagePopupManager.Instance?.ShowText(transform.position,
                $"+{quantity} {itemData.itemName}", Color.yellow);
        }
    }

    [TargetRpc]
    private void TargetInventoryFull(NetworkConnectionToClient target)
    {
        DamagePopupManager.Instance?.ShowText(transform.position, "Inventário Cheio!", Color.red);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out PlayerInventory inventory))
        {
            // Auto-pickup opcional
            // Pickup(inventory);
        }
    }
}