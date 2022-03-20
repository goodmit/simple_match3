using System.Collections.Generic;
using UnityEngine;
using View;
using Object = UnityEngine.Object;

public class TilePool
{
    private readonly Queue<Tile> _queue = new Queue<Tile>();
    private readonly Transform _poolContainer;
    
    public TilePool(Tile tile, int capacity, Transform container)
    {
        _poolContainer = container;
        for (var i = 0; i < capacity; i++)
        {
            var t = Object.Instantiate(tile, container);
            ReturnToPool(t);
        }
    }

    ~TilePool()
    {
        _queue.Clear();
    }
    
    public Tile Take()
    {
        return _queue.Dequeue();
    }

    public void ReturnToPool(Tile tile)
    {
        tile.transform.SetParent(_poolContainer);
        Reset(tile);
        _queue.Enqueue(tile);
    }

    private void Reset(Tile tile)
    {
        tile.Reset();
        tile.transform.localPosition = Vector3.zero;
    }
}