using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets;
using Controllers;
using UnityEngine;
using Utils;
using Zenject;
using Random = UnityEngine.Random;

namespace View
{
    public class GridManager : MonoBehaviour
    {
        public static event Action OnShuffleItems;
        
        [Range(4, 8)]
        [SerializeField] private int tileTypeCount = 4;
        [Range(0, 8)]
        [SerializeField] private int blocksCount = 3;
        [SerializeField] private Vector2Int mapSize = new Vector2Int(6, 6);
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private SpriteMask mask;
        
        [Inject] private ScoreController _scoreController;
        [Inject] private AudioController _audioController;
        
        private Tile[,] _tiles;
        private TileItemDataList _tilesData;
        private List<TileItemData> _foodTilesData;
        private TilePool _pool;
        private Transform _poolContainer;

        private struct PossibleMatchesBlock
        {
            public readonly Vector2Int SecondItemPoint;
            public readonly Vector2Int ShouldBeItemPoint;
            public readonly Vector2Int[] PossibleMatchPoints;
        
            public PossibleMatchesBlock(Vector2Int secondItemPoint, Vector2Int shouldBeItemPoint, Vector2Int[] possibleMatchPoints)
            {
                this.SecondItemPoint = secondItemPoint;
                ShouldBeItemPoint = shouldBeItemPoint;
                PossibleMatchPoints = possibleMatchPoints;
            }
        }
    
        private void Awake()
        {
            mask.enabled = false;
            Tile.InputLocked = true;
            mask.transform.localScale = new Vector3(mapSize.x, mapSize.y);
            _tilesData = Resources.Load<TileItemDataList>("ItemsData/TileItemDataList");
            _foodTilesData = _tilesData.Tiles.Where(t => t.Type == TileType.Item).Take(tileTypeCount).ToList();
            Tile.OnReadyToSwap += OnTileSwapHandler;
            CreatePool();
        }

        private void OnDestroy()
        {
            Tile.OnReadyToSwap -= OnTileSwapHandler;
        }

        private void Start()
        {
#pragma warning disable CS4014
            InitGrid();
#pragma warning restore CS4014
        }

        private void OnTileSwapHandler(Tile tile1, Tile tile2)
        {
#pragma warning disable CS4014
            SwapTiles(tile1, tile2);
#pragma warning restore CS4014
        }
    
        private void CreatePool()
        {
            _poolContainer = new GameObject("pool").transform;
            _poolContainer.SetParent(transform);
            _poolContainer.position = new Vector3(0, 0, 500);
            var poolSize = (int)(mapSize.x * mapSize.y * 1.5f);
            _pool = new TilePool(tilePrefab, poolSize, _poolContainer);
        }
    
        private async Task InitGrid()
        {
            var blockTileData = _tilesData.Tiles.Find(t => t.Type == TileType.Block);
            var spawnedTiles = 0;
            _tiles = new Tile[mapSize.x, mapSize.y];
            for (var x = 0; x < mapSize.x; x++)
            {
                for (var y = 0; y < mapSize.y; y++)
                {
                    var tile = GetNewTile(x, y);
                
                    //if (blocksCount > 0 && y == 1 && x > 0)
                    if (NeedSpawnBlockItem(spawnedTiles++))
                    {
                        blocksCount--;
                        tile.InitTile(blockTileData);

                        _tiles[x, y] = tile;
                        continue;
                    }
                
                    var deniedTiles = GetDeniedTiles(x, y);

                    if (deniedTiles.Count > 0)
                    {
                        var tilesData = deniedTiles.Aggregate(_foodTilesData, (current, deniedName) 
                            => current.Where(t => t.Name != deniedName).ToList());
                    
                        InitTile(tile, tilesData);
                    }
                    else
                    {
                        InitTile(tile, _foodTilesData);
                    }
                    _tiles[x, y] = tile;
                }
            }

            // after filling the map we should align the map transform to the center of the screen
            transform.position = new Vector3((mapSize.x - 1) * -0.5f, (mapSize.y - 1) * -0.5f);

            while (!HasPossibleMatches())
            {
                Debug.LogWarning("No possibles matches! Items have to be shuffled!");
                await Shuffle();
            }
        
            mask.enabled = true;
            Tile.InputLocked = false;
        }
    
        private void InitTile(Tile tile, IReadOnlyList<TileItemData> tilesData)
        {
            var index = Random.Range(0, tilesData.Count);
            tile.InitTile(tilesData[index]);
        }
        
        private async Task SwapTiles(Tile tile1, Tile tile2, bool needCheck = true)
        {
            _audioController.PlayOne(AudioController.SelectKey);
            Tile.InputLocked = true;
            var x1 = (int) tile1.transform.localPosition.x;
            var y1 = (int) tile1.transform.localPosition.y;
            var x2 = (int) tile2.transform.localPosition.x;
            var y2 = (int) tile2.transform.localPosition.y;
        
            tile1.Move(new Vector3(tile2.transform.position.x, tile2.transform.position.y, 1));
            tile2.Move(new Vector3(tile1.transform.position.x, tile1.transform.position.y, 0));
        
            _tiles[x1, y1] = tile2;
            _tiles[x2, y2] = tile1;

            await Awaiters.WaitUntil(() => !tile1.isMoving);
            await Awaiters.WaitUntil(() => !tile2.isMoving);

            if (needCheck)
            {
                var matches = GetMatches();
                if (matches.Count == 0)
                {
                    await SwapTiles(tile2, tile1, false);
                }
                else
                {
                    while (matches.Count > 0)
                    {
                        _audioController.PlayOne(AudioController.MatchKey);
                        foreach (var tile in matches)
                        {
                            tile.PlayMatchedAnimation();
                        }
                        
                        foreach (var tile in matches)
                        {
                            await Awaiters.WaitUntil(() => !tile.isMoving);
                            _tiles[(int)tile.transform.localPosition.x, (int)tile.transform.localPosition.y] = null;
                            _pool.ReturnToPool(tile);
                        }

                        matches.Clear();
                        //await Awaiters.WaitForSeconds(0.5f);
                    
                        await RefillGrid();
                    
                        matches = GetMatches();
                    }
                }
            }

            while (!HasPossibleMatches())
            {
                await Shuffle();
            }
        
            Tile.InputLocked = false;
        }

        private async Task RefillGrid()
        {
            var moved = new List<Tile>();
        
            for (var y = mapSize.y - 1; y >= 0; y--)
            {
                for (var x = 0; x < mapSize.x; x += 1)
                {
                    if (_tiles[x, y] == null || _tiles[x, y].Type != TileType.Item) continue;
                
                    var tile = _tiles[x, y];
                
                    var path = FindPath(x, y, tile);

                    if (path.Count > 0)
                    {
                        tile.Move(path);
                        moved.Add(tile);
                    }
                }
            }

            for (var x = 0; x < mapSize.x; x++)
            {
                var index = 0;
                while (IsLegalCell(x, 0) && _tiles[x, 0] == null)
                {
                    index++;
                    var path = new List<Direction>();
                
                    var tile = GetNewTile(x, -index);
                    InitTile(tile, _foodTilesData);
                    _tiles[x, 0] = tile;
                    for (var i = 0; i < index; i++)
                    {
                        path.Add(Direction.Up);
                    }
                    path.AddRange(FindPath(x, 0, tile));
                
                    if (path.Count > 0)
                    {
                        tile.Move(path);
                        moved.Add(tile);
                    }
                }
            }
        
            foreach (var t in moved)
            {
                await Awaiters.WaitUntil(() => !t.isMoving);
            }
        }

        private List<Direction> FindPath(int x, int y, Tile tile)
        {
            var pathFinding = true;
            var path = new List<Direction>();
            var offset = 0;
            while (pathFinding)
            {
                offset++;

                if (!IsLegalCell(x, y + offset) || _tiles[x, y + offset] != null)
                {
                    pathFinding = false;
                    if (offset > 1)
                    {
                        _tiles[x, y] = null;
                        _tiles[x, y + offset - 1] = tile;
                    }
                }
                else
                {
                    path.Add(Direction.Up);
                }
            }
        
            if (IsLegalCell(x - 1, y + offset - 1) &&
                IsLegalCell(x - 1, y + offset) &&
                _tiles[x - 1, y + offset - 1] != null &&
                _tiles[x - 1, y + offset] == null)
            {
                _tiles[x, y + offset - 1] = null;
                _tiles[x - 1, y + offset] = tile;
                path.Add(Direction.UpLeft);
                var alterPath = FindPath(x - 1, y + offset, tile);
                path.AddRange(alterPath);
            }
            else if(IsLegalCell(x + 1, y + offset - 1) &&
                    IsLegalCell(x + 1, y + offset) &&
                    (!IsLegalCell(x + 2, y + offset - 1) || _tiles[x + 2, y + offset - 1] != null && _tiles[x + 2, y + offset - 1].Type == TileType.Block) &&
                    _tiles[x + 1, y + offset - 1] != null &&
                    _tiles[x + 1, y + offset - 1].Type == TileType.Block &&
                    _tiles[x + 1, y + offset] == null)
            {
                _tiles[x, y + offset - 1] = null;
                _tiles[x + 1, y + offset] = tile;
                path.Add(Direction.UpRight);
                var alterPath = FindPath(x + 1, y + offset, tile);
                path.AddRange(alterPath);
            }

            return path;
        }

        private bool HasPossibleMatches()
        {
            for (var x = 0; x < mapSize.x; x++)
            {
                for (var y = 0; y < mapSize.y; y++)
                {
                    var rightBlock = new PossibleMatchesBlock(
                        new Vector2Int(x + 1, y),
                        new Vector2Int(x + 2, y),
                        new [] {
                            new Vector2Int(x + 2, y + 1),
                            new Vector2Int(x + 2, y - 1),
                            new Vector2Int(x + 3, y) }
                    );

                    var leftBlock = new PossibleMatchesBlock(
                        new Vector2Int(x + 1, y),
                        new Vector2Int(x - 1, y),
                        new [] {
                            new Vector2Int(x - 1, y + 1),
                            new Vector2Int(x - 1, y - 1),
                            new Vector2Int(x - 2, y) }
                    );
                
                    var horizontalSpacedBlock = new PossibleMatchesBlock(
                        new Vector2Int(x + 2, y),
                        new Vector2Int(x + 1, y),
                        new [] {
                            new Vector2Int(x + 1, y + 1),
                            new Vector2Int(x + 1, y - 1)}
                    );
                
                    var upBlock = new PossibleMatchesBlock(
                        new Vector2Int(x, y + 1),
                        new Vector2Int(x, y + 2),
                        new [] {
                            new Vector2Int(x + 1, y + 2),
                            new Vector2Int(x - 1, y + 2),
                            new Vector2Int(x, y + 3) }
                    );

                    var downBlock = new PossibleMatchesBlock(
                        new Vector2Int(x, y - 1),
                        new Vector2Int(x, y - 2),
                        new [] {
                            new Vector2Int(x + 1, y - 2),
                            new Vector2Int(x - 1, y - 2),
                            new Vector2Int(x, y - 3) }
                    );
                
                    var verticalSpacedBlock = new PossibleMatchesBlock(
                        new Vector2Int(x, y + 2),
                        new Vector2Int(x, y + 1),
                        new [] {
                            new Vector2Int(x + 1, y + 1),
                            new Vector2Int(x - 1, y + 1)}
                    );

                    if (CheckHorizontalPossibles(x, y, new [] { rightBlock, leftBlock, upBlock, downBlock, horizontalSpacedBlock, verticalSpacedBlock }))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    
        private bool CheckHorizontalPossibles(int x, int y, IEnumerable<PossibleMatchesBlock> possiblesBlocks)
        {
            var tile = _tiles[x, y];
            if (tile == null || tile.Type != TileType.Item) return false;

            foreach (var block in possiblesBlocks)
            {
                if (!IsLegalCell(block.SecondItemPoint.x, block.SecondItemPoint.y) ||
                    _tiles[block.SecondItemPoint.x, block.SecondItemPoint.y] == null ||
                    _tiles[block.SecondItemPoint.x, block.SecondItemPoint.y].Name != tile.Name) continue;
            
                if (!IsLegalCell(block.ShouldBeItemPoint.x, block.ShouldBeItemPoint.y) ||
                    _tiles[block.ShouldBeItemPoint.x, block.ShouldBeItemPoint.y] == null ||
                    _tiles[block.ShouldBeItemPoint.x, block.ShouldBeItemPoint.y].Type != TileType.Item) continue;

                foreach (var possibleMatch in block.PossibleMatchPoints)
                {
                    if (IsLegalCell(possibleMatch.x, possibleMatch.y) &&
                        _tiles[possibleMatch.x, possibleMatch.y] != null &&
                        _tiles[possibleMatch.x, possibleMatch.y].Name == tile.Name)
                    {
                        return true;
                    }
                }
            }
        
            return false;
        }
    
        private HashSet<Tile> GetMatches()
        {
            var size = Math.Max(mapSize.x, mapSize.y);
            var markedTiles = new HashSet<Tile>();
            for (var i = 0; i < size; i++)
            {
                markedTiles.UnionWith(MatchTilesInLine(i, size));
                markedTiles.UnionWith(MatchTilesInLine(i, size, false));
            }
        
            return markedTiles;
        }

        private HashSet<Tile> MatchTilesInLine(int lineIndex, int size, bool isRow = true)
        {
            var result = new HashSet<Tile>();
            var temp = new List<Tile>();
            for (var i = 0; i < size; i++)
            {
                var x = isRow ? i : lineIndex;
                var y = isRow ? lineIndex : i;
            
                if (!IsLegalCell(x, y)) continue;
            
                var tile = _tiles[x, y];
                if (tile == null || tile.Type != TileType.Item)
                {
                    if (temp.Count > 2)
                    {
                        _scoreController.AddScores(temp.Count);
                        result.UnionWith(temp);
                    }
                    temp.Clear();
                    continue;
                }
            
                if (temp.All(t => t.Name == tile.Name))
                {
                    if (tile.Type == TileType.Item)
                    {
                        temp.Add(tile);
                    }
                }
                else
                {
                    if (temp.Count < 3)
                    {
                        temp.Clear();
                        if (tile.Type == TileType.Item)
                        {
                            temp.Add(tile);
                        }
                        continue;
                    }
                
                    _scoreController.AddScores(temp.Count);
                    result.UnionWith(temp);
                
                    temp.Clear();
                
                    if (tile.Type == TileType.Item)
                    {
                        temp.Add(tile);
                    }
                }
            }

            if (temp.Count >= 3)
            {
                _scoreController.AddScores(temp.Count);
                result.UnionWith(temp);
            }
            temp.Clear();
        
            return result;
        }
    
        private Tile GetNewTile(int x, int y)
        {
            var tile = _pool.Take();
            var tileTransform = tile.transform;
            tileTransform.SetParent(transform);
            tileTransform.position = new Vector3(x, y) + transform.position;
            return tile;
        }

        private bool NeedSpawnBlockItem(int spawnedTiles)
        {
            if (blocksCount == 0) return false;
        
            var baseChance = Mathf.Ceil(100f / ((mapSize.x * mapSize.y - spawnedTiles) / (float)blocksCount));
            var randomTry= Random.Range(0, 100f);
            return baseChance > randomTry;
        }
        
        private List<string> GetDeniedTiles(int x, int y)
        {
            var result = new List<string>();
            var previousTile = IsLegalCell(x - 1, y) ? _tiles[x - 1, y] : null;
            Tile secondPreviousTile;
            if (previousTile != null)
            {
                secondPreviousTile = IsLegalCell(x - 2, y) ? _tiles[x - 2, y] : null;
                if (secondPreviousTile != null && previousTile.Type != TileType.Block && previousTile.Name == secondPreviousTile.Name)
                {
                    result.Add(previousTile.Name);
                }
            }
        
            previousTile = IsLegalCell(x, y - 1) ? _tiles[x, y - 1] : null;
        
            if (previousTile == null || previousTile.Type == TileType.Block) return result;
        
            secondPreviousTile = IsLegalCell(x, y - 2) ? _tiles[x, y - 2] : null;

            if (secondPreviousTile == null || previousTile.Name != secondPreviousTile.Name) return result;

            if (result.Count == 0 || previousTile.Name != result[0])
            {
                result.Add(previousTile.Name);
            }

            return result;
        }

        private async Task Shuffle()
        {
            OnShuffleItems?.Invoke();
            await Awaiters.WaitForSeconds(0.3f);
            
            var newGrid = new Tile[mapSize.x, mapSize.y];
            var list = CreateShuffledList();

            while (!WasShuffled(list, newGrid))
            {
                Array.Clear(newGrid, 0, newGrid.Length);
                list = CreateShuffledList();
            }

            _tiles = newGrid;

            await Awaiters.WaitUntil(() =>
                _tiles.Cast<Tile>().Where(t => t != null && t.Type == TileType.Item).All(t => t.isMoving));
        }

        private bool WasShuffled(IList<Tile> list, Tile[,] newGrid)
        {
            for (var x = 0; x < mapSize.x; x++)
            {
                for (var y = 0; y < mapSize.y; y++)
                {
                    if (_tiles[x, y] == null || _tiles[x, y].Type == TileType.Block)
                    {
                        newGrid[x, y] = _tiles[x, y];
                        continue;
                    }

                    var deniedItemX = GetDeniedItemsX(x, y, newGrid);
                    var deniedItemY = GetDeniedItemsY(x, y, newGrid);

                    var index = list.Count - 1;
                    while (index >= 0)
                    {
                        if(list[index].Name != deniedItemX && list[index].Name != deniedItemY) break;
                        index--;
                    }

                    if (index < 0)
                    {
                        return false;
                    }

                    newGrid[x, y] = list[index];
                    if (newGrid[x, y] != null)
                    {
                        newGrid[x, y].Move(new Vector3(x, y) + transform.position);
                    }
                    list.RemoveAt(index);
                }
            }
            
            _audioController.PlayOne(AudioController.ShuffleKey);
            
            return true;
        }

        private string GetDeniedItemsX(int x, int y, Tile[,] grid)
        {
            if (IsLegalCell(x - 1, y) &&
                IsLegalCell(x - 2, y) &&
                grid[x - 1, y] != null &&
                grid[x - 2, y] != null &&
                grid[x - 1, y].Name == grid[x - 2, y].Name)
            {
                return grid[x - 1, y].Name;
            }
        
            return string.Empty;
        }
    
        private string GetDeniedItemsY(int x, int y, Tile[,] grid)
        {
            if (IsLegalCell(x, y - 1) &&
                IsLegalCell(x, y - 2) &&
                grid[x, y - 1] != null &&
                grid[x, y - 2] != null &&
                grid[x, y - 1].Name == grid[x, y - 2].Name)
            {
                return grid[x, y - 1].Name;
            }
        
            return string.Empty;
        }

        private List<Tile> CreateShuffledList()
        {
            var result = _tiles.Cast<Tile>().Where(t => t != null && t.Type == TileType.Item).ToList();

            for (var index = result.Count - 1; index > 0; index--)
            {
                var randomIndex = Random.Range(0, index + 1);
                (result[randomIndex], result[index]) = (result[index], result[randomIndex]);
            }
            return result;
        }
    
        private bool IsLegalCell(int x, int y)
        {
            return x >= 0 && x < mapSize.x && y >= 0 && y < mapSize.y;
        }
    }
}
