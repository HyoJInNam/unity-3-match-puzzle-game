using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public enum GameState
{
    wait,
    move,
}
public enum TileKind
{
    normal = 0,
    blank,
    breakable,
    count
}

[System.Serializable]
public class TileType
{
    [SerializeField]
    public int x;
    public int y;
    public int tileKind;

    public TileType(int x, int y, int tileKind)
    {
        this.x = x;
        this.y = y;
        this.tileKind = tileKind;
    }
}

public class Board : MonoBehaviour
{
    public GameState currentState = GameState.move;
    public int width;
    public int height;
    public int offSet;
    public GameObject BackGrounp;
    public GameObject tilePrefab;
    public GameObject breakableTilePrefab;
    public GameObject[] dots;
    public GameObject destroyParticle;
    public TileType[] boardLayout;
    private bool[,] blankSpaces;
    private BackgroundTile[,] backgroundTiles;
    private BackgroundTile[,] breakableTiles;
    public GameObject[,] allDots;
    public Dot currentDot;
    private FindMatches findMatches;
    private GameManager gameManager;
    

    // Use this for initialization
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        findMatches = FindObjectOfType<FindMatches>();
        
        breakableTiles = new BackgroundTile[width, height];
        blankSpaces = new bool[width, height];
        allDots = new GameObject[width, height];
        SetUp();
    }
    public void GenerateBlankSpaces()
    {
        try
        {
            for (int i = 0; i < boardLayout.Length; i++)
            {
                //TileKind.Blank
                if (boardLayout[i].tileKind == 1)
                {
                    blankSpaces[boardLayout[i].x, boardLayout[i].y] = true;
                }
            }
        }
        catch (System.Exception exp)
        {
            return;
        }
    }
    public void GenerateBreakableTiles()
    {
        try
        {
            for (int i = 0; i < boardLayout.Length; i++)
            {
                //TileKind.Breakable
                if (boardLayout[i].tileKind == 2)
                {
                    Vector2 tempPosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                    GameObject tile = Instantiate(breakableTilePrefab, tempPosition, Quaternion.identity);
                    breakableTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
                }
            }
        }
        catch(System.Exception exp)
        {
            return;
        }
    }

    private void SetUp()
    {
        GenerateBlankSpaces();
        GenerateBreakableTiles();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (!blankSpaces[i, j])
                {
                    GameObject backgroundTile = Instantiate(tilePrefab, new Vector2(i, j), Quaternion.identity, BackGrounp.transform);
                    backgroundTile.name = "( " + i + ", " + j + " )";

                    int dotToUse = Random.Range(0, dots.Length);
                    int maxIterations = 0;
                    while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100)
                    {
                        dotToUse = Random.Range(0, dots.Length);
                        maxIterations++;
                        Debug.Log(maxIterations);
                    }
                    maxIterations = 0;

                    GameObject dot = Instantiate(dots[dotToUse], new Vector2(i, j + offSet), Quaternion.identity, this.transform);
                    dot.GetComponent<Dot>().row = j;
                    dot.GetComponent<Dot>().column = i;
                    allDots[i, j] = dot;
                }
            }
        }
    }

    private bool MatchesAt(int column, int row, GameObject piece)
    {
        if (column > 1 && row > 1)
        {
            if (allDots[column - 1, row] != null && allDots[column - 2, row] != null)
            {
                if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag)
                {
                    return true;
                }
            }
            if (allDots[column, row - 1] != null && allDots[column, row - 2] != null)
            {
                if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag)
                {
                    return true;
                }
            }

        }
        else if (column <= 1 || row <= 1)
        {
            if (row > 1)
            {
                if (allDots[column, row - 1] != null && allDots[column, row - 2] != null)
                {
                    if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag)
                    {
                        return true;
                    }
                }
            }
            if (column > 1)
            {
                if (allDots[column - 1, row] != null && allDots[column - 2, row] != null)
                {
                    if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void DestroyMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    DestroyMatchesAt(i, j);
                }
            }
        }
        findMatches.Clear();
        StartCoroutine(DecreaseRowCo2());
    }

    private void CheckToMakeBombs()
    {
        if (findMatches.currentMatches.Count < 4) return;

        findMatches.CheckMatchedCount();

        if (currentDot == null) return;

        if (currentDot.MakeItUnmatchedBomb())
        {
            currentDot.otherDot.GetComponent<Dot>().MakeItUnmatchedBomb();
        }
    }
    private void DestroyMatchesAt(int column, int row)
    {
        if (allDots[column, row].GetComponent<Dot>().isMatched)
        {
            CheckToMakeBombs();

            bool isBreakable = false;
            if (breakableTiles[column, row] != null)
            {
                breakableTiles[column, row].TakeDamage(1);
                if (breakableTiles[column, row].hitPoints <= 0)
                {
                    breakableTiles[column, row] = null;
                    isBreakable = true;
                }
            }

            GameObject particle = Instantiate(destroyParticle, allDots[column, row].transform.position, Quaternion.identity);

            Destroy(particle, .5f);
            gameManager.DecreaseGoal(allDots[column, row].gameObject.name, isBreakable);
            Destroy(allDots[column, row]);
            allDots[column, row] = null;
        }
    }

    private IEnumerator DecreaseRowCo2()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (!blankSpaces[i, j] && allDots[i, j] == null)
                {
                    for (int k = j + 1; k < height; k++)
                    {
                        if (allDots[i, k] != null)
                        {
                            allDots[i, k].GetComponent<Dot>().row = j;
                            allDots[i, k] = null;
                            break;
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(.4f);
        StartCoroutine(FillBoardCo());
    }
    private IEnumerator FillBoardCo()
    {
        RefillBoard();
        yield return new WaitForSeconds(.5f);

        while (MatchesOnBoard())
        {
            yield return new WaitForSeconds(.5f);
            DestroyMatches();
        }
        findMatches.Clear();
        currentDot = null;
        yield return new WaitForSeconds(.5f);

        if (IsDeadLocked())
        {
            ShuffleBoard();
        }
        currentState = GameState.move;
    }

    private void RefillBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null && !blankSpaces[i, j])
                {
                    Vector2 tempPosition = new Vector2(i, j + offSet);
                    int dotToUse = Random.Range(0, dots.Length);
                    int maxIterations = 0;

                    while (MatchesAt(i, j, dots[dotToUse]) && maxIterations < 100)
                    {
                        maxIterations++;
                        dotToUse = Random.Range(0, dots.Length);
                    }

                    maxIterations = 0;
                    GameObject piece = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity, this.transform);
                    allDots[i, j] = piece;
                    piece.GetComponent<Dot>().row = j;
                    piece.GetComponent<Dot>().column = i;
                }
            }
        }
    }
    private bool MatchesOnBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null) continue;

                if (allDots[i, j].GetComponent<Dot>().isMatched)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckForMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null) continue;

                if (i < width - 2)
                {
                    if (CheckForMatchedAt(allDots[i + 1, j], allDots[i, j], allDots[i + 2, j])) return true;
                }
                if (j < height - 2)
                {
                    if (CheckForMatchedAt(allDots[i, j + 1], allDots[i, j], allDots[i, j + 2])) return true;
                }
            }
        }
        return false;
    }
    private bool CheckForMatchedAt(GameObject dot1, GameObject currentDot, GameObject dot2)
    {
        if (dot1 == null || dot2 == null) return false;
        return (dot1.tag == currentDot.tag && dot2.tag == currentDot.tag) ? true : false;
    }

    private bool IsDeadLocked()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    if (i < width - 1)
                    {
                        if (SwitchAndCheck(i, j, Vector2.right)) return false;
                    }
                    if (j < height - 1)
                    {
                        if (SwitchAndCheck(i, j, Vector2.up)) return false;
                    }
                }
            }
        }
        return true;
    }
    private bool SwitchAndCheck(int column, int row, Vector2 direction)
    {
        SwitchPieces(column, row, direction);
        if (CheckForMatches())
        {
            SwitchPieces(column, row, direction);
            return true;
        }
        SwitchPieces(column, row, direction);
        return false;
    }
    private void SwitchPieces(int column, int row, Vector2 direction)
    {
        GameObject holder = allDots[column + (int)direction.x, row + (int)direction.y] as GameObject;
        allDots[column + (int)direction.x, row + (int)direction.y] = allDots[column, row];
        allDots[column, row] = holder;
    }

    private void ShuffleBoard()
    {
        List<GameObject> newBoard = new List<GameObject>();
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    newBoard.Add(allDots[i, j]);
                }
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (!blankSpaces[i, j])
                {
                    int pieceToUse = Random.Range(0, newBoard.Count);
                    int maxIterations = 0;

                    while (MatchesAt(i, j, newBoard[pieceToUse]) && maxIterations < 100)
                    {
                        pieceToUse = Random.Range(0, newBoard.Count);
                        maxIterations++;
                        Debug.Log(maxIterations);
                    }

                    Dot piece = newBoard[pieceToUse].GetComponent<Dot>();
                    maxIterations = 0;
                    piece.column = i;
                    piece.row = j;
                    allDots[i, j] = newBoard[pieceToUse];
                    newBoard.Remove(newBoard[pieceToUse]);
                }
            }
            if (IsDeadLocked())
            {
                ShuffleBoard();
            }
        }
    }
}