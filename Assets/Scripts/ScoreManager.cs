using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    #region Singleton
    private static ScoreManager _instance = null;

    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ScoreManager>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error : ScoreManager not found");
                }
            }

            return _instance;
        }
    }
    #endregion    
    public int TileRatio;
    public int ComboRatio;
    public int HighScore { get { return _highScore; } }
    public int CurrentScore { get { return _currentScore; } }

    private static int _highScore;
    private static int _currentScore;

    private void Start()
    {
        ResetCurrentScore();
    }

    public void ResetCurrentScore()
    {
        _currentScore = 0;
    }

    public void IncrementCurrentScore(int tileCount, int comboCount)
    {        
        _currentScore += (tileCount * TileRatio) * (comboCount * ComboRatio);

        SoundManager.Instance.PlayerScore(comboCount > 1);
    }

    public void SetHighScore()
    {
        if (_currentScore > _highScore)
        {
            _highScore = _currentScore;
        }        
    }
}
