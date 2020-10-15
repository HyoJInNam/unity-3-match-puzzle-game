using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneEvent : MonoBehaviour
{
    public List<GameObject> canvas;

    private void Awake()
    {
        Screen.SetResolution(1280, 720, false);
    }

    public void LoadeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadeExitButton()
    {
        //UnityEditor.EditorApplication.isPlaying=false; 
        Application.Quit();
    }
    
    public void LoadeButton(GameObject go)
    {
        foreach (GameObject w in canvas)
        {
            bool isActive = ((w.Equals(go)) ? true : false);
            if (isActive) w.SetActive((w.activeSelf) ? false : true);
            else w.SetActive(false);
        }
    }
}
