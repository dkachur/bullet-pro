using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardPlayerItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private TMP_Text _killsText;
    [SerializeField] private TMP_Text _deathsText;
    private int _kills;
    private int _playerActorNumber;

    public void SetPlayerInfo(string name, int actorNumber)
    {
        _playerNameText.text = name;
        _playerActorNumber = actorNumber;
    }

    public void UpdateKills(int kills) 
    { 
        _killsText.text = kills.ToString();
        _kills = kills;
    }

    public void UpdateDeaths(int deaths)
    {
        _deathsText.text = deaths.ToString();   
    }

    public int GetActorNumber()
    {
        return _playerActorNumber;
    }

    public int GetKills()
    {
        return _kills;
    }
}
