using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    #region Singleton
    private static BoardManager _instance = null;

    public static BoardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BoardManager>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error : BoardManager not found");
                }                
            }

            return _instance;
        }
    }
    #endregion

    public bool IsAnimating
    {
        get
        {
            return IsProcessing || IsSwapping;
        }
    }

    public bool IsProcessing { get; set; }
    public bool IsSwapping { get; set; }

    [Header("Board")]
    public Vector2Int Size;
    public Vector2 OffsetTile;
    public Vector2 OffsetBoard;

    [Header("Tile")]
    public List<Sprite> TileTypes = new List<Sprite>();
    public GameObject TilePrefab;

    private Vector2 _startPosition;
    private Vector2 _endPosition;
    private TileController[,] _tiles;

    private int _combo;
    private void Start()
    {
        Vector2 tileSize = TilePrefab.GetComponent<SpriteRenderer>().size;
        CreateBoard(tileSize);

        IsProcessing = false;
        IsSwapping = false;
    }
    #region Generate
    public void CreateBoard(Vector2 tileSize)
    {
        _tiles = new TileController[Size.x, Size.y];

        Vector2 totalSize = (tileSize + OffsetTile) * (Size - Vector2.one);

        _startPosition = (Vector2)transform.position - ((totalSize - (tileSize + OffsetTile)) / 2) + OffsetBoard;
        _endPosition = _startPosition + totalSize;

        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                TileController newTile = Instantiate(
                    TilePrefab,
                    new Vector2(
                        _startPosition.x + ((tileSize.x + OffsetTile.x) * x),
                        _startPosition.y + ((tileSize.y + OffsetTile.y) * y)),
                    TilePrefab.transform.rotation)
                    .GetComponent<TileController>();

                _tiles[x, y] = newTile;

                List<int> possibleId = GetStartingPossibleIdList(x, y);
                int newId = possibleId[Random.Range(0, possibleId.Count)];
                
                newTile.ChangeId(newId, x, y);
            }
        }
    }

    private List<int> GetStartingPossibleIdList(int x, int y)
    {
        List<int> possibledId = new List<int>();

        for (int i= 0; i < TileTypes.Count; i++)
        {
            possibledId.Add(i);
        }

        if (x > 1 && _tiles[x - 1, y].id == _tiles[x-2, y].id)
        {
            possibledId.Remove(_tiles[x - 1, y].id);
        }

        if (y > 1 && _tiles[x, y - 1].id == _tiles[x, y - 2].id)
        {
            possibledId.Remove(_tiles[x , y - 1].id);
        }
        return possibledId;
    }

    #endregion

    #region Swapping
    public IEnumerator SwapTilePosition(TileController a, TileController b, System.Action onCompleted)
    {
        IsSwapping = true;

        Vector2Int indexA = GetTileIndex(a);
        Vector2Int indexB = GetTileIndex(b);

        _tiles[indexA.x, indexA.y] = b;
        _tiles[indexB.x, indexB.y] = a;

        a.ChangeId(a.id, indexB.x, indexB.y);

        bool isRoutineACompleted = false;
        bool isRoutineBCompleted = false;

        StartCoroutine(a.MoveTilePosition(GetTileIndexPosition(indexB), () => { isRoutineACompleted = true; }));
        StartCoroutine(b.MoveTilePosition(GetTileIndexPosition(indexA), () => { isRoutineBCompleted = true; }));

        yield return new WaitUntil(() => { return isRoutineACompleted && isRoutineBCompleted; });

        onCompleted?.Invoke();

        IsSwapping = false;
    }
    #endregion

    #region Process
    
    public void Process()
    {
        IsProcessing = true;
        _combo = 0;
        ProcessMatches();
    }
    #region Match
    private void ProcessMatches()
    {
        List<TileController> matchingTile = GetAllMathes();

        if (matchingTile == null || matchingTile.Count == 0)
        {
            IsProcessing = false;
            return;
        }
        _combo++;
        ScoreManager.Instance.IncrementCurrentScore(matchingTile.Count, _combo);

        StartCoroutine(ClearMatches(matchingTile, ProcessDropping));
    }
    public List<TileController> GetAllMathes()
    {
        List<TileController> mathingTiles = new List<TileController>();

        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                List<TileController> tileMatched = _tiles[x, y].GetAllMatches();

                if (tileMatched == null || tileMatched.Count == 0)
                {
                    continue;
                }

                foreach (TileController item in tileMatched)
                {
                    if (!mathingTiles.Contains(item))
                    {
                        mathingTiles.Add(item);
                    }                    
                }
            }
        }

        return mathingTiles;
    }
    #endregion

    private IEnumerator ClearMatches(List<TileController> mathcingTiles, System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        for (int i = 0; i < mathcingTiles.Count; i++)
        {
            isCompleted.Add(false);
        }

        for(int i = 0; i < mathcingTiles.Count; i++)
        {
            int index = i;
            StartCoroutine(mathcingTiles[i].SetDestroyed(() => { isCompleted[index] = true; }));
        }

        yield return new WaitUntil(() => { return IsAllTrue(isCompleted); });
        
        onCompleted?.Invoke();
    }
    #region Drop
    private void ProcessDropping()
    {
        Dictionary<TileController, int> dropingTiles = GetAllDrop();       

        StartCoroutine(DropTiles(dropingTiles, ProcessDestroyAndFill));
    }

    private Dictionary<TileController, int> GetAllDrop()
    {
        Dictionary<TileController, int> droppingTiles = new Dictionary<TileController, int>();

        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                if (_tiles[x, y].IsDestroyed)
                {
                    for (int i = y+ 1; i < Size.y; i++)
                    {
                        if (_tiles[x, i].IsDestroyed)
                        {
                            continue;
                        }

                        if (droppingTiles.ContainsKey(_tiles[x, i]))
                        {
                            droppingTiles[_tiles[x, i]]++;
                        }
                        else
                        {
                            droppingTiles.Add(_tiles[x, i], 1);
                        }
                    }
                }                
            }
        }
        return droppingTiles;
    }

    private IEnumerator DropTiles(Dictionary<TileController, int> droppingTiles, System.Action onCompleted)
    {
        foreach(KeyValuePair<TileController, int> pair in droppingTiles)
        {
            Vector2Int tileIndex = GetTileIndex(pair.Key);

            TileController temp = pair.Key;
            _tiles[tileIndex.x, tileIndex.y] = _tiles[tileIndex.x, tileIndex.y - pair.Value];
            _tiles[tileIndex.x, tileIndex.y - pair.Value] = temp;

            temp.ChangeId(temp.id, tileIndex.x, tileIndex.y - pair.Value);
        }

        yield return null;

        onCompleted?.Invoke();
    }
    #endregion

    #region Drop & Fill
    private void ProcessDestroyAndFill()
    {
        List<TileController> destroyedTiles = GetAllDestroyed();

        StartCoroutine(DestroyAndFillTiles(destroyedTiles, ProcessReposition));
    }

    private List<TileController> GetAllDestroyed()
    {
        List<TileController> destroyedTiles = new List<TileController>();

        for (int x= 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                if(_tiles[x, y].IsDestroyed)
                {
                    destroyedTiles.Add(_tiles[x, y]);
                }
            }
        }

        return destroyedTiles;
    }

    private IEnumerator DestroyAndFillTiles(List<TileController> destroyedTiles, System.Action onCompleted)
    {
        List<int> highestIndex = new List<int>();

        for (int i = 0; i < Size.x; i++)
        {
            highestIndex.Add(Size.y - 1);
        }

        float spawnHeight = _endPosition.y + TilePrefab.GetComponent<SpriteRenderer>().size.y + OffsetTile.y;

        foreach(TileController tile in destroyedTiles)
        {
            Vector2Int tileIndex = GetTileIndex(tile);
            Vector2Int targetIndex = new Vector2Int(tileIndex.x, highestIndex[tileIndex.x]);
            highestIndex[tileIndex.x]--;

            tile.transform.position = new Vector2(tile.transform.position.x, spawnHeight);
            tile.GenerateRandomTile(targetIndex.x, targetIndex.y);
        }

        yield return null;

        onCompleted?.Invoke();
    }
    #endregion

    #region Reposition
    private void ProcessReposition()
    {
        StartCoroutine(RepositionTiles(ProcessMatches));
    }

    private IEnumerator RepositionTiles(System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        int i = 0;
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y <Size.y; y++)
            {
                Vector2 targetPosition = GetTileIndexPosition(new Vector2Int(x, y));

                if ((Vector2) _tiles[x, y].transform.position == targetPosition)
                {
                    continue;
                }

                isCompleted.Add(false);

                int index = i;
                StartCoroutine(_tiles[x, y].MoveTilePosition(targetPosition, () => { isCompleted[index] = true; }));

                i++;
            }
        }

        yield return new WaitUntil(() => { return IsAllTrue(isCompleted); });

        onCompleted?.Invoke();
    }    
    #endregion

    #endregion

    #region Helper
    public Vector2Int GetTileIndex(TileController tile)
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                if (tile == _tiles[x, y]) return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    public Vector2 GetTileIndexPosition(Vector2Int index)
    {
        Vector2 tileSize = TilePrefab.GetComponent<SpriteRenderer>().size;
        return new Vector2(
                _startPosition.x + ((tileSize.x + OffsetTile.x) * index.x),
                _startPosition.y + ((tileSize.y + OffsetTile.y) * index.y)
                );
    }

    public bool IsAllTrue(List<bool> list)
    {
        foreach(bool status in list)
        {
            if (!status) return false;
        }

        return true;
    }
    #endregion
}
