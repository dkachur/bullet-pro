using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviourPunCallbacks
{
    #region Singleton Implementation
    public static ConnectionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    #endregion

    #region Constants
    private const string LoadingText = "Loading...";
    private const string JoiningLobbyText = "Joining The Lobby...";
    private const string JoiningRoomText = "Joining The Room...";
    private const string CreatingRoomText = "Creating Room...";
    private const string LeavingRoomText = "Leaving Room...";
    private const string TestRoomName = "Test";
    private const int MaxPlayersInRoom = 8;
    #endregion

    public string[] maps;

    private List<RoomInfo> allRooms = new List<RoomInfo>();
    private static bool _hasNickName;

    #region Unity Methods
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        MainMenuUIManager.Instance.ShowLoadingScreenWithText(LoadingText);
    }
    #endregion

    #region Public Methods
    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(MainMenuUIManager.Instance.GetRoomNameInputText()))
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = MaxPlayersInRoom;

            PhotonNetwork.CreateRoom(MainMenuUIManager.Instance.GetRoomNameInputText(), roomOptions);
            MainMenuUIManager.Instance.ShowLoadingScreenWithText(CreatingRoomText);
        }
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        MainMenuUIManager.Instance.ShowLoadingScreenWithText(JoiningRoomText);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MainMenuUIManager.Instance.ShowLoadingScreenWithText(LeavingRoomText);
    }

    public void SetNickName(string nickName)
    {
        PhotonNetwork.NickName = nickName;
        _hasNickName = true;
    }

    public void LoadGameScene()
    {
        var customProperties = new ExitGames.Client.Photon.Hashtable { { "GameStarted", true } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);

        PhotonNetwork.LoadLevel(maps[Random.Range(0, maps.Length)]);
    }

    public void QuickLaunch()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = MaxPlayersInRoom;

        PhotonNetwork.CreateRoom(TestRoomName, roomOptions);
        MainMenuUIManager.Instance.ShowLoadingScreenWithText(CreatingRoomText);
    }
    #endregion

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected To Server!");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        MainMenuUIManager.Instance.ShowLoadingScreenWithText(JoiningLobbyText);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby!");

        if (!_hasNickName)
        {
            MainMenuUIManager.Instance.ActivateEnterNickNameUI();
        }
        else
        {
            MainMenuUIManager.Instance.ActivateMenuButtons();
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Room with name {PhotonNetwork.CurrentRoom.Name} created.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room with name {PhotonNetwork.CurrentRoom.Name}.");
        MainMenuUIManager.Instance.ActivateRoomUIWithName(PhotonNetwork.CurrentRoom.Name);
        MainMenuUIManager.Instance.ChangeStartGameButtonState(PhotonNetwork.IsMasterClient);

        allRooms.Clear();


        Debug.Log($"[LocalActorNumber] {PhotonNetwork.LocalPlayer.ActorNumber}.");
        ShowPlayersInRoom();

        //if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameStarted"))
        //{
        //    PhotonNetwork.LoadLevel(GameSceneName);
        //}
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Error while creating room: {message}.\nReturn Code: {returnCode}.");
        MainMenuUIManager.Instance.ActivateErrorUI(message);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Player left the room.");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[List Update] List Count: {roomList.Count}");

        foreach (RoomInfo updatedRoom in roomList)
        {
            Debug.Log($"[List Update] Room Name: {updatedRoom.Name}");
            Debug.Log($"[List Update] Room Removed: {updatedRoom.RemovedFromList}");

            bool wasUpdated = false;

            for (int i = 0; i < allRooms.Count; i++)
            {
                if (allRooms[i].Name == updatedRoom.Name)
                {
                    if (updatedRoom.RemovedFromList)
                    {
                        allRooms.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        allRooms[i] = updatedRoom;
                        wasUpdated = true;
                    }
                }
            }

            if (!wasUpdated)
            {
                allRooms.Add(updatedRoom);
            }
        }

        MainMenuUIManager.Instance.ShowRoomsList(allRooms);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player with name {newPlayer.NickName} entered room!");
        Debug.Log($"[ActorNumber] {newPlayer.ActorNumber} entered!");
        MainMenuUIManager.Instance.AddPlayerToList(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[ActorNumber] {otherPlayer.ActorNumber} left!");
        MainMenuUIManager.Instance.RemovePlayerFromTheList(otherPlayer.ActorNumber);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        MainMenuUIManager.Instance.ChangeStartGameButtonState(PhotonNetwork.IsMasterClient);
    }

    #endregion

    #region Private Methods

    private void ShowPlayersInRoom()
    {
        MainMenuUIManager.Instance.ShowPlayersList(PhotonNetwork.PlayerList);
    }

    #endregion
}
