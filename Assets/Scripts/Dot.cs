using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

public enum SwipeDirection
{
    none,
    left,
    right,
    up,
    down
}

public enum DotState
{
    None,
    RowBomb,
    ColumnBomb,
    AdjacentBomb,
    ColorBomb
}

public class Dot : MonoBehaviour
{
    [Header("board Variables")]
    public int column;
    public int row;
    public int previousColumn;
    public int previousRow;
    public int targetX;
    public int targetY;
    public bool isMatched = false;


    public GameManager gameManager;

    private FindMatches findMatches;
    private Board board;
    public GameObject otherDot;
    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;

    [Header("Swipe Stuff")]
    public SwipeDirection swipeDir;
    public float swipeAngle = 0;
    public float swipeResist = .5f;

    [Header("Powerup Stuff")]
    public DotState dotState;
    public GameObject adjacentMarker;
    public GameObject rowArrow;
    public GameObject columnArrow;
    public GameObject colorBomb;


    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        swipeDir = SwipeDirection.none;
        dotState = DotState.None;

        board = FindObjectOfType<Board>();
        findMatches = FindObjectOfType<FindMatches>();
    }

    void Update()
    {
        targetX = column;
        targetY = row; 
        SetPosition(Mathf.Abs(targetX - transform.position.x) > .1, new Vector2(targetX, transform.position.y));
        SetPosition(Mathf.Abs(targetY - transform.position.y) > .1, new Vector2(transform.position.x, targetY));
    }
    private void SetPosition(bool IsLongAway, Vector2 tempPos)
    {
        if (!IsLongAway) transform.position = tempPos;
        else
        {
            transform.position = Vector2.Lerp(transform.position, tempPos, .6f);
            if (board.allDots[column, row] != this.gameObject)
            {
                board.allDots[column, row] = this.gameObject;
            }
            findMatches.FindAllMatches();
        }
    }

    private void OnMouseDown()
    {
        if (board.currentState == GameState.move)
        {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void OnMouseUp()
    {
        if (board.currentState == GameState.move)
        {
            finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CalculateAngle();
        }
    }

    void CalculateAngle()
    {
        if (Mathf.Abs(finalTouchPosition.y - firstTouchPosition.y) > swipeResist || Mathf.Abs(finalTouchPosition.x - firstTouchPosition.x) > swipeResist)
        {
            board.currentState = GameState.wait;
            swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * Mathf.Rad2Deg;// 180 / Mathf.PI;
            MovePieces();
            board.currentDot = this;
        }
        else
        {
            board.currentState = GameState.move;
            swipeAngle = 0;
        }
    }
    void MovePieces()
    {
        if (swipeAngle > -45 && swipeAngle <= 45 && column < board.width - 1)
        {
            MovePiecesActual(Vector2.right);
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && row < board.height - 1)
        {
            MovePiecesActual(Vector2.up);
        }
        else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        { 
            MovePiecesActual(Vector2.left);
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0)
        {
            MovePiecesActual(Vector2.down);
        }
        else
        {
            board.currentState = GameState.move;
        }
    }
    void MovePiecesActual(Vector2 direction)
    {
        otherDot = board.allDots[column + (int)direction.x, row + (int)direction.y];
        previousRow = row;
        previousColumn = column;

        if (otherDot != null)
        {
            MovePiecesDirection(direction);
            otherDot.GetComponent<Dot>() .MovePiecesDirection(direction * (-1));
            StartCoroutine(CheckMoveCo());
        }
        else
        {
            board.currentState = GameState.move;
        }
    }
    void MovePiecesDirection(Vector2 direction)
    {
        column += (int)direction.x;
        row += (int)direction.y;

        if (direction == Vector2.left) swipeDir = SwipeDirection.left;
        if (direction == Vector2.right) swipeDir = SwipeDirection.right;
        if (direction == Vector2.up) swipeDir = SwipeDirection.up;
        if (direction == Vector2.down) swipeDir = SwipeDirection.down;
    }
    public IEnumerator CheckMoveCo()
    {
        if (dotState == DotState.ColorBomb)
        {
            findMatches.MatchPiecesOfColor(otherDot.tag);
            isMatched = true;
        }
        else if (otherDot.GetComponent<Dot>().dotState == DotState.ColorBomb)
        {
            findMatches.MatchPiecesOfColor(this.gameObject.tag);
            otherDot.GetComponent<Dot>().isMatched = true;
        }

        yield return new WaitForSeconds(.5f);
        if (otherDot != null)
        {
            if (!isMatched && !otherDot.GetComponent<Dot>().isMatched)
            {
                otherDot.GetComponent<Dot>().row = row;
                otherDot.GetComponent<Dot>().column = column;
                row = previousRow;
                column = previousColumn;
                yield return new WaitForSeconds(.5f);
                board.currentDot = null;
                board.currentState = GameState.move;
            }
            else
            {
                if(gameManager.DecreaseMoveCount() == true) board.currentState = GameState.wait;
                board.DestroyMatches();
            }
        }
    }

    public bool MakeItUnmatchedBomb()
    {
        if (!isMatched) return (otherDot != null);


        findMatches.CheckBombs(this);
        isMatched = false;


        switch (dotState)
        {
            case DotState.RowBomb:
                MakeRowBomb();
                break;
            case DotState.ColumnBomb:
                MakeColumnBomb();
                break;
            case DotState.AdjacentBomb:
                MakeAdjacentBomb();
                break;
            case DotState.ColorBomb:
                MakeColorBomb();
                break;
            default:
                break;
        }
        return false;
    }
    private void MakeRowBomb()
    {
        dotState = DotState.RowBomb;
        Instantiate(rowArrow, transform.position, Quaternion.identity, this.transform);
    }
    private void MakeColumnBomb()
    {
        dotState = DotState.ColumnBomb;
        Instantiate(columnArrow, transform.position, Quaternion.identity, this.transform);
    }
    private void MakeColorBomb()
    {
        dotState = DotState.ColorBomb;
        Instantiate(colorBomb, transform.position, Quaternion.identity, this.transform);
    }
    private void MakeAdjacentBomb()
    {
        dotState = DotState.AdjacentBomb;
        Instantiate(adjacentMarker, transform.position, Quaternion.identity, this.transform);
    }

}