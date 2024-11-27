using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _playerText;
    private int _actorNumber;

    public void SetPlayerInfo(Player player)
    {
        _actorNumber = player.ActorNumber;
        _playerText.text = player.NickName;
    }

    public int GetActorNumber()
    {
        return _actorNumber;
    }
}
