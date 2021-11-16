using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    #region Singleton
    private static GameFlowManager _instance = null;

    public static GameFlowManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameFlowManager>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error : ScoreManager not found");
                }
            }

            return _instance;
        }
    }
    #endregion    
    
    [Header("UI")]
    public UIGameOver GameOverUI;
    public bool IsGameOver { get { return _isGameOver; } }
    private bool _isGameOver = false;
    private void Start()
    {
        _isGameOver = false;
    }
    public void GameOver()
    {
        _isGameOver = true;
        ScoreManager.Instance.SetHighScore();
        GameOverUI.Show();
    }
}
