using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct ScoreData
{
    public int level;
    public int width;
    public int height;
    public int objLevel;
    public float score;

    public ScoreData(int l, int w, int h, int obl, float s)
    {
        level = l;
        width = w;
        height = h;
        objLevel = obl;
        score =s;
    }
}

public class ScoreManager: MonoBehaviour
{
    JsonManager<ScoreData> JM = new JsonManager<ScoreData>("GameScoreData.json");
    ScoreData data;

    public Text levelText;
    public Slider logo;
    public List<GameObject> objects;

    private void Start()
    {
        data = new ScoreData();
        JM.Load(ref data);
        
        if (levelText) this.levelText.text = data.level.ToString();
        if (logo) StartCoroutine(LogoValue());
    }
    IEnumerator LogoValue()
    {
        float logoValueGap = 0;
        float logoValue = (data.score / 10000);

        while (logoValueGap <= logoValue)
        {
            if (logo.value == 1) break;
            logoValueGap += (logoValue / 100);
            logo.value += (logoValue / 100);
            yield return new WaitForSeconds(0.05f);
        }
        if (logo.value == 1.0f)
        {
            int objMaxLevel = 4;
            objects[(data.objLevel > objMaxLevel) ? objMaxLevel : data.objLevel].SetActive(true);
            logo.value = 0;
            data.objLevel += 1;
        }

        logoValueGap = 0;
        data.score = 0;
        JM.Save(data);
    }
}
