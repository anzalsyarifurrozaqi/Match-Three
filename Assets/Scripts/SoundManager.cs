using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    #region Singleton
    private static SoundManager _instance = null;

    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();

                if (_instance == null)
                {
                    Debug.LogError("Fatal Error : BoardManager not found");
                }
            }

            return _instance;
        }
    }
    #endregion

    public AudioClip ScoreNormal;
    public AudioClip ScoreCombo;

    public AudioClip WrongMove;

    public AudioClip Tap;

    private AudioSource _player;

    private void Start()
    {
        _player = GetComponent<AudioSource>();
    }

    public void PlayerScore(bool isCombo)
    {
        if (isCombo)
        {
            _player.PlayOneShot(ScoreCombo);
        }
        else
        {
            _player.PlayOneShot(ScoreNormal);
        }
    }

    public void PlayerWrongMove()
    {
        _player.PlayOneShot(WrongMove);
    }

    public void PlayTap()
    {
        _player.PlayOneShot(Tap);
    }
}
