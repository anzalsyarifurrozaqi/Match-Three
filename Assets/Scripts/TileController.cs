using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public int id;
    public bool IsDestroyed { get; private set; }

    private static readonly Color _selectColor = new Color(.5f, .5f, .5f);
    private static readonly Color _normalColor = Color.white;

    private static readonly float _moveDuration = .5f;
    private static readonly float _destroyBigDuration = .1f;
    private static readonly float _destriySmalleDuration = .4f;

    private static readonly Vector2 _sizeBig = Vector2.one * 1.2f;
    private static readonly Vector2 _sizeSmall = Vector2.zero;
    private static readonly Vector2 _sizeNormal = Vector2.one;

    private static readonly Vector2[] adjacentDirection = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private static TileController _previcousTile = null;

    private BoardManager _board;
    private GameFlowManager _game;
    private SpriteRenderer _render;
    private bool _isSelected = false;

    private void Awake()
    {
        _board = BoardManager.Instance;
        _game = GameFlowManager.Instance;
        _render = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        IsDestroyed = false;
    }

    private void OnMouseDown()
    {
        if (_render.sprite == null || _board.IsAnimating ||_game.IsGameOver) return;

        SoundManager.Instance.PlayTap();

        if (_isSelected)
        {
            Deselect();
        }

        else
        {
            if (_previcousTile == null)
            {
                Select();
            }
            else
            {
                if (GetAllAdjacentTiles().Contains(_previcousTile))
                {
                    TileController otherTile = _previcousTile;
                    _previcousTile.Deselect();
                    
                    SwapTile(otherTile, () => {
                        if (_board.GetAllMathes().Count > 0)
                        {
                            _board.Process();
                        }
                        else
                        {
                            SoundManager.Instance.PlayerWrongMove();
                            SwapTile(otherTile);
                        }
                        
                    });
                }
                else
                {
                    _previcousTile.Deselect();
                    Select();
                }               
                //Select();
            }
        }
    }
    public void ChangeId(int id, int x, int y)
    {
        _render.sprite = _board.TileTypes[id];
        this.id = id;

        name = $"TILE {id} ({x}, {y})";
    }



    #region Select & Deselect
    private void Select()
    {
        _isSelected = true;
        _render.color = _selectColor;
        _previcousTile = this;
    }

    private void Deselect()
    {
        _isSelected = false;
        _render.color = _normalColor;
        _previcousTile = null;
    }
    #endregion
   
    #region Swapping & Moving
    public void SwapTile(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(_board.SwapTilePosition(this, otherTile, onCompleted));
    }

    public IEnumerator MoveTilePosition(Vector2 targetPosition, System.Action onComplete)
    {
        Vector2 startPosition = transform.position;        
        float time = 0.0f;

        yield return new WaitForEndOfFrame();

        while (time < _moveDuration)
        {
            transform.position = Vector2.Lerp(startPosition, targetPosition, time / _moveDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }        

        transform.position = targetPosition;

        onComplete.Invoke();
    }
    #endregion

    #region Adjacent
    public TileController GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, _render.size.x);

        if (hit)
        {
            return hit.collider.GetComponent<TileController>();
        }

        return null;
    }

    public List<TileController> GetAllAdjacentTiles()
    {
        List<TileController> adjacentTiles = new List<TileController>();

        for (int i = 0; i < adjacentDirection.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirection[i]));
        }

        return adjacentTiles;
    }
    #endregion

    #region Check Match
    private List<TileController> GetMatch(Vector2 castDir)
    {
        List<TileController> matchingTiles = new List<TileController>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, _render.size.x);

        while (hit)
        {
            TileController otherTile = hit.collider.GetComponent<TileController>();
            if (otherTile.id != id || otherTile.IsDestroyed)
            {
                break;
            }

            matchingTiles.Add(otherTile);
            hit = Physics2D.Raycast(otherTile.transform.position, castDir, _render.size.x);
        }

        return matchingTiles;
    }

    private List<TileController> GetOneLineMatch(Vector2[] paths)
    {
        List<TileController> matchingTiles = new List<TileController>();

        for (int i = 0; i <paths.Length; i++)
        {
            matchingTiles.AddRange(GetMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            return matchingTiles;
        }

        return null;
    }

    public List<TileController> GetAllMatches()
    {
        if (IsDestroyed)
        {
            return null;
        }

        List<TileController> matchinTiles = new List<TileController>();

        List<TileController> horizontalMathingTiles = GetOneLineMatch(new Vector2[2] { Vector2.up, Vector2.down });
        List<TileController> vertocalMathingTiles = GetOneLineMatch(new Vector2[2] { Vector2.left, Vector2.right });

        if (horizontalMathingTiles != null)
        {
            matchinTiles.AddRange(horizontalMathingTiles);
        }

        if (vertocalMathingTiles != null)
        {
            matchinTiles.AddRange(vertocalMathingTiles);
        }

        if (matchinTiles != null && matchinTiles.Count >= 2)
        {
            matchinTiles.Add(this);
        }

        return matchinTiles;
    }
    #endregion

    #region Destroy & Generate
    public IEnumerator SetDestroyed(System.Action onCompleted)
    {
        IsDestroyed = true;
        id = -1;
        name = "TILE_NULL";

        Vector2 startSize = transform.localScale;
        float time = 0.0f;

        while(time < _destroyBigDuration)
        {
            transform.localScale = Vector2.Lerp(startSize, _sizeBig, time / _destroyBigDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.localScale = _sizeBig;

        startSize = transform.localScale;
        time = 0.0f;

        while(time < _destriySmalleDuration)
        {
            transform.localScale = Vector2.Lerp(startSize, _sizeSmall, time / +_destriySmalleDuration);
            time += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = _sizeSmall;
        _render.sprite = null;

        onCompleted?.Invoke();
    }

    public void GenerateRandomTile(int x, int y)
    {
        transform.localScale = _sizeNormal;
        IsDestroyed = false;

        ChangeId(Random.Range(0, _board.TileTypes.Count), x, y);
    }
    #endregion
}
