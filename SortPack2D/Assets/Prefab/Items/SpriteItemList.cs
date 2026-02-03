using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpriteItemList", menuName = "SortPack/Sprite Item List")]
public class SpriteItemList : ScriptableObject
{
    [System.Serializable]
    public class SpriteItem
    {
        [HideInInspector] public int itemID;  // Tự động gán, ẩn trong Inspector
        public string itemName;
        public Sprite sprite;
    }

    [Header("Item Sprites")]
    public List<SpriteItem> items = new List<SpriteItem>();

    [Header("Base Prefab")]
    public GameObject baseItemPrefab;

    // Tự động gán ID khi có thay đổi trong Editor
    private void OnValidate()
    {
        AutoAssignIDs();
    }

    // Tự động gán ID theo thứ tự trong list (0, 1, 2, 3...)
    public void AutoAssignIDs()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].itemID = i;

            // Tự động đặt tên theo sprite nếu chưa có tên
            if (string.IsNullOrEmpty(items[i].itemName) && items[i].sprite != null)
            {
                items[i].itemName = items[i].sprite.name;
            }
        }
    }

    public Sprite GetSprite(int itemID)
    {
        if (itemID >= 0 && itemID < items.Count)
            return items[itemID].sprite;
        return null;
    }

    public string GetItemName(int itemID)
    {
        if (itemID >= 0 && itemID < items.Count)
            return items[itemID].itemName;
        return "Item_" + itemID;
    }

    public SpriteItem GetItem(int itemID)
    {
        if (itemID >= 0 && itemID < items.Count)
            return items[itemID];
        return null;
    }

    public GameObject SpawnItem(int itemID, Vector3 position, Transform parent = null)
    {
        if (baseItemPrefab == null)
        {
            Debug.LogError("[SpriteItemList] baseItemPrefab is NULL!");
            return null;
        }

        Sprite sprite = GetSprite(itemID);
        if (sprite == null)
        {
            Debug.LogWarning("[SpriteItemList] Sprite not found for ID " + itemID);
            return null;
        }

        GameObject itemObj = Instantiate(baseItemPrefab, position, Quaternion.identity, parent);
        itemObj.name = GetItemName(itemID);

        // GÁN SPRITE
        SpriteRenderer sr = itemObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
        }

        // 🔹 AUTO-FIT BOX COLLIDER (3D) NẾU CÓ
        BoxCollider col3D = itemObj.GetComponent<BoxCollider>();
        if (col3D != null && sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;  // local bounds theo sprite
            col3D.center = b.center;
            col3D.size = b.size;
        }

        // 🔹 AUTO-FIT BOX COLLIDER 2D NẾU CÓ
        BoxCollider2D col2D = itemObj.GetComponent<BoxCollider2D>();
        if (col2D != null && sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            col2D.offset = b.center;
            col2D.size = b.size;
        }

        // Item data
        Item item = itemObj.GetComponent<Item>();
        if (item != null)
        {
            item.itemID = itemID;
            item.itemType = GetItemName(itemID).ToLower();
        }

        return itemObj;
    }



    public int GetItemCount()
    {
        return items.Count;
    }

    public List<int> GetAllIDs()
    {
        List<int> ids = new List<int>();
        for (int i = 0; i < items.Count; i++)
        {
            ids.Add(i);
        }
        return ids;
    }
}
