using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[System.Serializable]
public struct GOAL
{
    [SerializeField]
    public string name;
    public int count;
    public GOAL(string name, int count)
    {
        this.name = name; this.count = count;
    }
}

[System.Serializable]
public struct Level
{
    [SerializeField]
    public int level;
    public int width;
    public int height;
    public int moveCount;
    public List<GOAL> goals;
    public List<TileType> tileType;

    public Level(int l, int w, int h, int mc, List<GOAL> g, List<TileType> tt)
    {
        level = l;
        width = w;
        height = h;
        moveCount = mc;
        goals = g;
        tileType = tt;
    }
}

public class GameManager : MonoBehaviour
{
    JsonManager<ScoreData> JM = new JsonManager<ScoreData>("GameScoreData.json");
    ScoreData Scoredata;

    JsonManager<Level> JMLvl = new JsonManager<Level>("GameLevelData.json");
    public Level levelData;

    [Header("Game Clear")]
    public GameObject clearButton;
    public GameObject gameClear;
    public GameObject ALLGameClear;
    public GameObject gameScore;
    private bool isClear = false;

    [Header("Game Object")]
    public Board board;
    public SoundManager sm;
    public GameObject gameOver;
    public Text gameLevel;
    public Text moveCount;
    public List<GameObject> goalList;

    [Header("Game")]
    public int titlelevel;
    public int moveCnt;
    public List<GOAL> goals;
    List<TileType> TTList = new List<TileType>();

    private bool isLoad;
    private int width;
    private int height;
    private bool isBreakable = false;
    public int score = 0;

    private void Awake()
    {
        Scoredata = new ScoreData();
        JM.Load(ref Scoredata);
        titlelevel = Scoredata.level;

        levelData = new Level();
        isLoad = SetBoardSize(JMLvl.Load(ref levelData));
    }
    private bool SetBoardSize(bool isLoad)
    {
        if (isLoad)
        {
            if (levelData.level == titlelevel)
            {
                board.width = levelData.width;
                board.height = levelData.height;
                board.boardLayout = new TileType[levelData.tileType.Count];
                board.boardLayout = levelData.tileType.ToArray();

                moveCnt = levelData.moveCount;
                goals = levelData.goals;
                return true;
            }
        }

        board.width = (levelData.level < 10)? Random.Range(6, 12) : Random.Range(9, 14);
        board.height = (levelData.level < 10) ? Random.Range(6, 12) : Random.Range(9, 14);

        SetGameInfo(board.width, board.height);

        board.boardLayout = new TileType[TTList.Count];
        board.boardLayout = TTList.ToArray();
        return false;
    }
    private void SetGameInfo(int w, int h)
    {
        levelData.level = titlelevel;
        width = w;
        height = h;

        SetMap(levelData.level % (int)TileKind.count);
        SetGoals();

        JM.Save(new ScoreData(levelData.level, width, height, Scoredata.objLevel, 0));
        JMLvl.Save(new Level(levelData.level, width, height, moveCnt, goals, TTList));
    }
    private void SetGoals()
    {
        string[] name = { "coral", "yellow", "green", "blue", "pupple" };
        bool[] isName = new bool[5] { false, false, false, false, false };

        moveCnt = ((width + height) / 4) * 5;

        int goalCnt = (moveCnt - 10) / 5;
        int dotCnt = ((6 - goalCnt) + (moveCnt / 10)) * 3;

        goals.Clear();
        while (goalCnt > 0)
        {
            int r = Random.Range(0, 5);
            if (isName[r] == true) continue;
            else
            {
                isName[r] = true;
                goals.Add(new GOAL(name[r], dotCnt));
                goalCnt -= 1;
            }
        }

        if (isBreakable == true)
        {
            goals.Add(new GOAL("Breakable", TTList.Count));
        }
    }
    void Start()
    {
        gameLevel.text = levelData.level.ToString();
        moveCount.text = moveCnt.ToString();

        foreach (GOAL goal in goals)
        {
            foreach (GameObject go in goalList)
            {
                if (goal.name.Equals(go.name))
                {
                    go.SetActive(true);
                    go.transform.Find("count").GetComponent<Text>().text = goal.count.ToString();
                }
            }
        }
    }

    public bool DecreaseMoveCount()
    {
        moveCnt--;
        moveCount.text = moveCnt.ToString();
        if (moveCnt == 0)
        {
            if (isClear && (gameClear.activeSelf || gameScore.activeSelf))
            {
                isClear = true;
                clearButton.SetActive(true);
                sm.PlayPlayerSound(PLAYERSOUNDTYPE.GAMECLEAR);
                StartCoroutine(Clear());
                return false;
            }

            gameOver.SetActive(true);
            sm.PlayPlayerSound(PLAYERSOUNDTYPE.GAMEOVER);
            return true;
        }
        return true;
    }
    public void DecreaseGoal(string gameObject, bool isBreakable)
    {
        sm.PlayPlayerSound(PLAYERSOUNDTYPE.MATCH);

        if (isBreakable)
        {
            DecreaseGoal("Breakable(Clone)", false);
        }

        int cnt = 0;
        foreach (GOAL goal in goals.ToArray())
        {
            string str = goal.name + "(Clone)";
            if (str.Equals(gameObject))
            {
                score += 10;
                GOAL g = new GOAL(goal.name, goal.count - 1);
                foreach (GameObject go in goalList)
                {
                    if (g.name.Equals(go.name))
                    {
                        g.count = (g.count <= 0) ? 0 : g.count;
                        score += (g.count <= 0) ? 40 : 20;
                        go.transform.Find("count").GetComponent<Text>().text = g.count.ToString();

                        goals.Add(g);
                        goals.RemoveAt(cnt);
                    }
                }
            }
            cnt++;
        }


        foreach (GOAL goal in goals.ToArray())
        {
            if (goal.count != 0) return;
        }

        StartCoroutine(Clear());
    }
    private IEnumerator Clear()
    {
        gameScore.SetActive(false);
        gameClear.SetActive(true);
        yield return new WaitForSeconds(1);
        gameClear.SetActive(false);
        gameScore.SetActive(true);

        gameScore.transform.Find("count").GetComponent<Text>().text = score.ToString();

        if (moveCnt == 0)
        {
            gameScore.SetActive(false);
            ALLGameClear.SetActive(true);
            ALLGameClear.transform.Find("count").GetComponent<Text>().text = score.ToString();
            JM.Save(new ScoreData(levelData.level + 1, board.width, board.height, Scoredata.objLevel, score));
        }
    }

    private void SetMap(int tileType)
    {
        if ((width == height) && (tileType == (int)TileKind.blank))
        {
            MapColumnAndRow((width / 2 + 1), (height / 2 + 1), tileType);
        }
        else if (width % 2 == 1)
        {
            SetMapType(tileType);
        }
        else if (height % 2 == 1)
        {
            SetMapType(tileType);
        }
        else
        {
            if (tileType == (int)TileKind.breakable) MapAll(tileType);
        }
    }
    private void SetMapType(int tileType)
    {
        if (tileType == (int)TileKind.blank) SetMapBlankAt(width, tileType);
        if (tileType == (int)TileKind.breakable) SetMapBreakableAt(width, tileType);
    }
    private void SetMapBreakableAt(int dir, int tileType)
    {
        isBreakable = true;
        bool idDir = (dir == width) ? true : false;
        int random = (Random.Range(0, dir) % 2);
        if (dir % 2 == 0)
        {
            if (random == 0)
            {
                //left
                for (int i = 0; i < (dir / 2 + 1); i++)
                    SetMapDirAt(idDir, i, tileType);
            }
            else
            {
                //right
                for (int i = (dir / 2 + 1); i < dir; i++)
                    SetMapDirAt(idDir, i, tileType);
            }
        }
        else
        {
            if (random == 0)
            {
                //left
                for (int i = 0; i < (dir / 3); i++)
                    SetMapDirAt(idDir, i, tileType);
                //right
                for (int i = (dir - (dir / 3) - 1); i < dir + 1; i++)
                    SetMapDirAt(idDir, i, tileType);
            }
            else
            {
                //middle
                for (int i = (dir / 3); i < (dir - (dir / 3)); i++)
                    SetMapDirAt(idDir, i, tileType);
            }
        }
    }
    private void SetMapBlankAt(int dir, int tileType)
    {
        bool idDir = (dir == width) ? true : false;
        if ((Random.Range(0, dir) % 2) == 0)
        {
            int l = dir / 3;
            SetMapDirAt(idDir, l, tileType);

            int r = dir - (dir / 3) - 1;
            SetMapDirAt(idDir, r, tileType);
        }
        else if(dir % 2 == 1)
        {
            int m = dir / 2;
            SetMapDirAt(idDir, m, tileType);
        }
    }
    private void SetMapDirAt(bool isColumn, int x, int tileType)
    {
        if (isColumn) MapColumn(x, tileType);
        else MapRow(x, tileType);
    }

    private void MapColumn(int m, int tileType)
    {
        for (int i = 0; i < width; i++)
        {
            TTList.Add(new TileType(i, m, tileType));
        }
    }
    private void MapRow(int m, int tileType)
    {
        for (int i = 0; i < height; i++)
        {
            TTList.Add(new TileType(m, i, tileType));
        }
    }
    private void MapColumnAndRow(int w, int h, int tileType)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (i == w || j == h) TTList.Add(new TileType(i, j, tileType));
            }
        }
    }
    private void MapAll(int tileType)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                TTList.Add(new TileType(i, j, tileType));
            }
        }
    }

}