using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    #region Singleton
    private static TimeManager _instance = null;

    public static TimeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimeManager>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error : ScoreManager not found");
                }
            }

            return _instance;
        }
    }
    #endregion    

    public int duration;
    private float _time;

    private void Start()
    {
        _time = 0;
    }

    private void Update()
    {
        if (GameFlowManager.Instance.IsGameOver)
        {
            return;
        }

        if (_time > duration)
        {
            GameFlowManager.Instance.GameOver();
            return;
        }

        _time += Time.deltaTime;
    }

    public float GetRemainingTime()
    {
        return duration - _time;
    }
}
