using System.Collections.Generic;
using UnityEngine;

namespace Assets
{
    public class TileItemDataList : ScriptableObject
    {
        [SerializeField] private List<TileItemData> tilesData;

        public List<TileItemData> Tiles => tilesData;
    }
}