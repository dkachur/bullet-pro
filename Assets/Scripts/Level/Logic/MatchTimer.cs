using System;
using UnityEngine;

public class MatchTimer : MonoBehaviour
{
    public event Action OnTimerEnd;

    private float _currentTime;
    private float _lastUpdateTime;


    public void SetTimer(float matchTime)
    {
        _currentTime = matchTime;
        _lastUpdateTime = Time.time;
        UpdateUI();
    }

    public float GetCurrentTime()
    {
        return _currentTime;
    }

    public void UpdateTimer()
    {
        _currentTime -= Time.time - _lastUpdateTime;
        _lastUpdateTime = Time.time;

        if (IsOver())
        {
            _currentTime = 0;
            OnTimerEnd?.Invoke();
        }

        UpdateUI();
    }

    public bool IsOver()
    {
        return _currentTime <= 0f;
    }

    private void UpdateUI()
    {
        UIController.Instance.UpdateTimer(_currentTime);
    }
}
