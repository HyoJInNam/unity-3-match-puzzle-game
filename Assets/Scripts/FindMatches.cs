using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FindMatches : MonoBehaviour
{
    private Board board;
    public int[] dotsCnt;
    public List<GameObject> currentMatches = new List<GameObject> ();

    // Start is called before the first frame update
    void Start()
    {
        board = FindObjectOfType<Board>();
        dotsCnt = new int[board.dots.Length];
    }

    public void Clear()
    {
        currentMatches.Clear();
        for (int i = 0; i < dotsCnt.Length; i++)
        {
            dotsCnt[i] = 0;
        }
    }

    public void MatchPiecesOfColor(string color)
    {
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                if (board.allDots[i, j] == null) continue;
                if (board.allDots[i, j].tag == color)
                {
                    board.allDots[i, j].GetComponent<Dot>().isMatched = true;
                }
            }
        }
    }

    public void FindAllMatches()
    {
        StartCoroutine(FindAllMatchesCo());
        CheckMatchedCount();
    }
    public void CheckMatchedCount()
    {
        for (int i = 0; i < dotsCnt.Length; i++)
        {
            dotsCnt[i] = 0;
        }

        for (int i = 0; i < currentMatches.Count; i++)
        {
            for (int j = 0; j < board.dots.Length; j++)
            {
                if (currentMatches[i].tag == board.dots[j].tag) dotsCnt[j]++;
            }
        }
    }
    private IEnumerator FindAllMatchesCo()
    {
        yield return new WaitForSeconds(.2f);
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                GameObject currentDot = board.allDots[i, j];
                if (currentDot == null) continue;

                if (i > 0 && i < board.width - 1)
                {
                    FindMatchedAt(board.allDots[i - 1, j], currentDot, board.allDots[i + 1, j]);
                }
                if (j > 0 && j < board.height - 1)
                {
                    FindMatchedAt(board.allDots[i, j + 1], currentDot, board.allDots[i, j - 1]);
                }
            }
        }
    }
    private void FindMatchedAt(GameObject dot1, GameObject currentDot, GameObject dot2)
    {
        if (dot1 == null || dot2 == null) return;
        if (dot1.tag == currentDot.tag && dot2.tag == currentDot.tag)
        {
            currentMatches.Union(IsBomb(dot1, currentDot, dot2));
            GetNearbyPieces(dot1, currentDot, dot2);
        }
    }

    private List<GameObject> IsBomb(GameObject dot1, GameObject dot2, GameObject dot3)
    {
        List<GameObject> currentDots = new List<GameObject>();
        GetBombAndMatch(dot1);
        GetBombAndMatch(dot2);
        GetBombAndMatch(dot3);
        return currentDots;
    }
    private void GetBombAndMatch(GameObject gbDot)
    {
        Dot dot = gbDot.GetComponent<Dot>();
        switch(dot.dotState)
        {
            case DotState.RowBomb:
                currentMatches.Union(GetRowPieces(dot.row));
                break;
            case DotState.ColumnBomb:
                currentMatches.Union(GetColumnPieces(dot.column));
                break;
            case DotState.AdjacentBomb:
                currentMatches.Union(GetAdjacentPieces(dot.column, dot.row));
                break;
            default:
                break;
        }
    }

    List<GameObject> GetAdjacentPieces(int column, int row)
    {
        List<GameObject> dots = new List<GameObject>();
        for(int i = column - 1; i <= column + 1; i++)
        {
            for(int j = row - 1; j <= row + 1; j++)
            {
                if(i >= 0 && i < board.width && j >= 0 && j < board.height)
                {
                    if (board.allDots[i, j] == null) continue;
                    dots.Add(board.allDots[i, j]);
                    board.allDots[i, j].GetComponent<Dot>().isMatched = true;
                }
            }
        }
        return dots;
    }
    List<GameObject> GetColumnPieces(int column)
    {
        List<GameObject> dots = new List<GameObject>();
        for (int i = 0; i < board.height; i++)
        {
            if (board.allDots[column, i] == null) continue;
            dots.Add(board.allDots[column, i]);
            board.allDots[column, i].GetComponent<Dot>().isMatched = true;
        }
        return dots;
    }
    List<GameObject> GetRowPieces(int row)
    {
        List<GameObject> dots = new List<GameObject>();
        for (int i = 0; i < board.width; i++)
        {
            if (board.allDots[i, row] == null) continue;
            dots.Add(board.allDots[i, row]);
            board.allDots[i, row].GetComponent<Dot>().isMatched = true;
        }

        return dots;
    }

    private void GetNearbyPieces(GameObject dot1, GameObject dot2, GameObject dot3)
    {
        AddToListAndMatch(dot1);
        AddToListAndMatch(dot2);
        AddToListAndMatch(dot3);
    }
    private void AddToListAndMatch(GameObject dot)
    {
        if (!currentMatches.Contains(dot))
        {
            currentMatches.Add(dot);
        }
        dot.GetComponent<Dot>().isMatched = true;
    }

    public void CheckBombs(Dot dot)
    {
        for (int i = 0; i < dotsCnt.Length; i++)
        {
            if (dotsCnt[i] == 4)
            {
                dot.dotState = IsMakeLineBomb(dot);
            }
            if (dotsCnt[i] == 5)
            {
                dot.dotState = IsMakeColorBomb();
            }
        }
    }
    private DotState IsMakeLineBomb(Dot dot)
    {
        if ((dot.swipeDir == SwipeDirection.left) || (dot.swipeDir == SwipeDirection.right))
        {
            return DotState.RowBomb;
        }
        else if ((dot.swipeDir == SwipeDirection.up) || (dot.swipeDir == SwipeDirection.down))
        {
            return DotState.ColumnBomb;
        }

        return DotState.None;
    }
    private DotState IsMakeColorBomb()
    {
        Dot firstPiece = currentMatches[0].GetComponent<Dot>();
        if (firstPiece == null) return DotState.None;

        int numberHorizontal = 0;
        int numberVertical = 0;
        
        foreach (GameObject currentPiece in currentMatches)
        {
            Dot dot = currentPiece.GetComponent<Dot>();
            if (dot.row == firstPiece.row) numberHorizontal++;
            if (dot.column == firstPiece.column) numberVertical++;
        }

        return (numberVertical == 5 || numberHorizontal == 5) ? DotState.ColorBomb : DotState.AdjacentBomb;
    }
}
