using System;
using UnityEngine;
using Utils;

namespace Assets
{
    [CreateAssetMenu(fileName = "TileItemData_", menuName = "Tile Item Data", order = 51)]
    public class TileItemData : ScriptableObject
    {
        [SerializeField] private string itemName;
        [SerializeField] private TileType tileType = TileType.Item;
        [SerializeField] private Sprite itemIcon;
        [SerializeField] private bool isSwappable = true;

        public string ID { get; } = Guid.NewGuid().ToString("N");
        public string Name => itemName;
        public TileType Type => tileType;
        public Sprite ItemIcon => itemIcon;
        public bool IsSwappable => isSwappable;
    }
}