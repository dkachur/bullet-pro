using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TMP_Text _roomNameText;
    private RoomInfo _roomInfo;

    public void SetRoomInfo(RoomInfo info)
    {
        _roomInfo = info;
        _roomNameText.text = _roomInfo.Name;
    }

    public void OpenRoom()
    {
        ConnectionManager.Instance.JoinRoom(_roomInfo.Name);
    }
}
