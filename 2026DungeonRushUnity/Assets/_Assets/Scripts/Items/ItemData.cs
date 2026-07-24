using UnityEngine;

// Config 1 loại item — tạo asset qua menu: Create > DungOnRush > Item Data
// Đặt asset trong Resources/Scriptable Objects/Items để StaticItemData load được.
[CreateAssetMenu(fileName = "ItemData", menuName = "DungOnRush/Item Data")]
public class ItemData : ScriptableObject
{
    public ItemType type;
    public string displayName;
    public Sprite icon;
}
