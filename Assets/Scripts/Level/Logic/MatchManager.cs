using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    #region Singleton Implementation
    public static MatchManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    } 
    #endregion

    #region Enums
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStats,
        ChangeGameState,
        RestartGame,
        LeaveRoom,
        SyncTime,
        SetMatchInfo,
        SetNewPlayerSkin,
        SyncSkinInfo,
    }

    public enum UpdateStatsOptions
    {
        UpdateKills,
        UpdateDeaths,
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending,
    }
    #endregion

    #region Serialized Fields
    [SerializeField] private int _killsToWin = 5;
    [SerializeField] private float _matchTime = 180f;
    [SerializeField] private MatchTimer _timer;
    [SerializeField] private Transform _endRoundCamPoint;
    [SerializeField] private GameState _gameState = GameState.Waiting;
    [SerializeField] private bool _isRepetitive;
    [SerializeField] private bool _setObserveCameraAfterEnd;
    [SerializeField] private bool _switchMapAfterEnd;
    [SerializeField] private List<PlayerInfo> _players = new List<PlayerInfo>();
    #endregion

    #region Private Fields
    private bool _hasNewPlayerEventBeenSent = false;
    private float _waitAfterEnding = 7f;
    #endregion

    #region Constants
    private const int PlayerPackageSize = 4;
    private const int UpdateStatsPackageSize = 3;
    private const int MainMenuSceneIndex = 0;
    private const int TimeSyncInterval = 5; 
    #endregion


    #region Unity Event Methods
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[Match Manager]: Sending Back To Menu!");
            SceneManager.LoadScene(MainMenuSceneIndex);
        }

        if (PhotonNetwork.IsConnectedAndReady && !_hasNewPlayerEventBeenSent)
        {
            NewPlayerSend(PhotonNetwork.NickName);
            _gameState = GameState.Playing;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            _timer.SetTimer(_matchTime);
            _timer.OnTimerEnd += SetEndingGameState;
            StartCoroutine(SyncTimeRoutine());
            SetMatchInfoSendAll();
        }
    }

    private void Update()
    {
        if (_gameState == GameState.Playing)
        {
            _timer.UpdateTimer();
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    } 
    #endregion

    #region Photon Callbacks
    public override void OnDisconnected(DisconnectCause cause)
    {
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(MainMenuSceneIndex);
    }

    public override void OnJoinedRoom()
    {
        if (!_hasNewPlayerEventBeenSent)
        {
            NewPlayerSend(PhotonNetwork.NickName);
            _gameState = GameState.Playing;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SetMatchInfoSendToPlayer(newPlayer.ActorNumber);
            SyncTimeSendToPlayer(newPlayer.ActorNumber);
            //SkinManager.Instance.AssignSkinToPlayer(newPlayer.ActorNumber);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(MainMenuSceneIndex);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            _players.RemoveAll(p => p.actor == otherPlayer.ActorNumber);
            UIController.Instance.SetLeaderboard(_players);
            ListPlayersSend();

            Debug.Log($"Skin released for Player {otherPlayer.ActorNumber}");
        }

        SkinManager.Instance.ReleaseSkin(otherPlayer.ActorNumber);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            _timer.OnTimerEnd += SetEndingGameState;
            StartCoroutine(SyncTimeRoutine());
        }
    }

    #endregion

    #region Custom Photon Events
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes eventCode = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            switch (eventCode)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;
                case EventCodes.ListPlayers:
                    ListPlayersReceive(data);
                    break;
                case EventCodes.UpdateStats:
                    UpdateStatsReceive(data);
                    break;
                case EventCodes.ChangeGameState:
                    ChangeGameStateReceive(data);
                    break;
                case EventCodes.RestartGame:
                    RestartGameReceive();
                    break;
                case EventCodes.LeaveRoom:
                    LeaveRoomReceive();
                    break;
                case EventCodes.SyncTime:
                    SyncTimeReceive(data);
                    break;
                case EventCodes.SetMatchInfo:
                    SetMatchInfoReceive(data);
                    break;
                case EventCodes.SetNewPlayerSkin:
                    SetNewPlayerSkinReceive(data);
                    break;
                case EventCodes.SyncSkinInfo:
                    SyncSkinInfoReceive(data);
                    break;
                default:
                    break;
            }
        }
    }

    public void NewPlayerSend(string nickname)
    {
        object[] package = new object[PlayerPackageSize]
        {
            nickname,
            PhotonNetwork.LocalPlayer.ActorNumber,
            0,
            0,
        };

        _hasNewPlayerEventBeenSent = PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true });
    }

    public void NewPlayerReceive(object[] data)
    {
        PlayerInfo newPlayer = new PlayerInfo(
            name: (string)data[0],
            actor: (int)data[1],
            kills: (int)data[2],
            deaths: (int)data[3]);

        _players.Add(newPlayer);

        UIController.Instance.SetLeaderboard(_players);
        SkinManager.Instance.AssignSkinToPlayer(newPlayer.actor);
        SetNewPlayerSkinSend(newPlayer.actor, SkinManager.Instance.GetSkinIndexForPlayer(newPlayer.actor));

        if (PhotonNetwork.MasterClient.ActorNumber != newPlayer.actor)
        {
            SyncSkinInfoSendToPlayer(newPlayer.actor);
        }
        
        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        object[] package = new object[_players.Count];

        for (int i = 0; i < package.Length; i++)
        {
            package[i] = new object[PlayerPackageSize]
            {
                _players[i].name,
                _players[i].actor,
                _players[i].kills,
                _players[i].deaths,
            };
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            new SendOptions { Reliability = true });
    }

    public void ListPlayersReceive(object[] data)
    {
        _players.Clear();

        for (int i = 0; i < data.Length; i++)
        {
            object[] playerData = (object[])data[i];

            _players.Add(new PlayerInfo(
                name: (string)playerData[0],
                actor: (int)playerData[1],
                kills: (int)playerData[2],
                deaths: (int)playerData[3]));
        }

        UIController.Instance.SetLeaderboard(_players);
    }

    public void SetNewPlayerSkinSend(int playerId, int skinIndex)
    {
        object[] package = new object[]
        {
            playerId,
            skinIndex,
        };

        SendEventToTarget((byte)EventCodes.SetNewPlayerSkin, package);
    }

    public void SetNewPlayerSkinReceive(object[] data)
    {
        int playerId = (int)data[0];
        int skinIndex = (int)data[1];

        SkinManager.Instance.AssignSkinToPlayer(playerId);
    }

    public void SyncSkinInfoSendToPlayer(int targetPlayerActorNumber)
    {
        object[] package = new object[_players.Count + 1];
        package[0] = SkinManager.Instance.GetSkinsQueueAsArray();

        for (int i = 0; i < package.Length - 1; i++)
        {
            package[i + 1] = new object[]
            {
                _players[i].actor,
                SkinManager.Instance.GetSkinIndexForPlayer(_players[i].actor)
            };
        }

        SendEventToTarget((byte)EventCodes.SyncSkinInfo, package, new[] { targetPlayerActorNumber } );
    }

    public void SyncSkinInfoReceive(object[] data)
    {
        SkinManager.Instance.SetSkinsQueue((int[])data[0]);
        for (int i = 1; i < data.Length; i++)
        {
            object[] skinInfo = (object[])data[i];

            SkinManager.Instance.SetSkin((int)skinInfo[0], (int)skinInfo[1]);    
        }
    }



    public void UpdateStatsSend(int actorNumber, UpdateStatsOptions stateToUpdate, int amountToAdd = 1)
    {
        object[] package = new object[UpdateStatsPackageSize]
        {
            actorNumber,
            stateToUpdate,
            amountToAdd,
        };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStats,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void UpdateStatsReceive(object[] data)
    {
        int actorNumber = (int)data[0];
        UpdateStatsOptions stateToUpdate = (UpdateStatsOptions)data[1];
        int amountToAdd = (int)data[2];

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].actor == actorNumber)
            {
                switch (stateToUpdate)
                {
                    case UpdateStatsOptions.UpdateKills:
                        _players[i].kills += amountToAdd;
                        UIController.Instance.UpdateKillsInLeaderboard(actorNumber, _players[i].kills);

                        if (PhotonNetwork.IsMasterClient)
                        {
                            EndRoundConditionCheck(_players[i].kills);
                        }

                        break;
                    case UpdateStatsOptions.UpdateDeaths:
                        _players[i].deaths += amountToAdd;
                        UIController.Instance.UpdateDeathsInLeaderboard(actorNumber, _players[i].deaths);
                        break;
                    default:
                        break;
                }

                break;
            }
        }
    }

    public void ChangeGameStateSend(GameState gameState)
    {
        object[] package = new object[] { gameState };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeGameState,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void ChangeGameStateReceive(object[] data)
    {
        _gameState = (GameState)data[0];

        if (_gameState == GameState.Ending)
        {
            EndGame();
        }
    }

    public void RestartGameSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RestartGame,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void RestartGameReceive()
    {
        _gameState = GameState.Playing;

        if (PhotonNetwork.IsMasterClient)
        {
            _players.ForEach(p => { p.kills = 0; p.deaths = 0; });
            ListPlayersSend();
            _timer.SetTimer(_matchTime);
            StartCoroutine(SyncTimeRoutine());
        }

        SpawnManager.Instance.SpawnPlayer();
        UIController.Instance.HideAll();
    }

    public void LeaveRoomSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.LeaveRoom,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void LeaveRoomReceive()
    {
        PhotonNetwork.LeaveRoom();
    }

    private void SendEventToTarget(byte eventCode, object[] package, int[] targetActors = null, ReceiverGroup receivers = ReceiverGroup.Others)
    {
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = receivers,
            TargetActors = targetActors
        };
        PhotonNetwork.RaiseEvent(eventCode, package, options, new SendOptions { Reliability = true });
    }


    public void SyncTimeSendAll()
    {
        object[] package = new object[] { _timer.GetCurrentTime() };
        SendEventToTarget((byte)EventCodes.SyncTime, package);
    }

    public void SyncTimeSendToPlayer(int targetPlayerActorNumber)
    {
        object[] package = new object[] { _timer.GetCurrentTime() };
        SendEventToTarget((byte)EventCodes.SyncTime, package, new[] { targetPlayerActorNumber });
    }

    public void SyncTimeReceive(object[] data)
    {
        _timer.SetTimer((float)data[0]);
    }


    public void SetMatchInfoSendAll()
    {
        object[] package = new object[] { _killsToWin, _matchTime, _isRepetitive, _setObserveCameraAfterEnd, _switchMapAfterEnd };
        SendEventToTarget((byte)EventCodes.SetMatchInfo, package);
    }

    public void SetMatchInfoSendToPlayer(int targetPlayerActorNumber)
    {
        object[] package = new object[] { _killsToWin, _matchTime, _isRepetitive, _setObserveCameraAfterEnd, _switchMapAfterEnd };
        SendEventToTarget((byte)EventCodes.SetMatchInfo, package, new[] { targetPlayerActorNumber });
    }

    public void SetMatchInfoReceive(object[] data)
    {
        _killsToWin = (int)data[0];
        _matchTime = (float)data[1];
        _isRepetitive = (bool)data[2];
        _setObserveCameraAfterEnd = (bool)data[3];
        _switchMapAfterEnd = (bool)data[4];
    }

    #endregion

    #region Helper Methods

    #region Game State Methods
    private void EndRoundConditionCheck(int playerKills)
    {
        if (playerKills >= _killsToWin && _killsToWin > 0)
        {
            SetEndingGameState();
        }
    }

    private void SetEndingGameState()
    {
        if (_gameState != GameState.Ending)
        {
            _gameState = GameState.Ending;
            ChangeGameStateSend(GameState.Ending);
        }
    }

    private void EndGame()
    {
        _gameState = GameState.Ending;

        UIController.Instance.ChangeOverheatedTextState(false);
        UIController.Instance.ShowEndRoundScreen();
        UIController.Instance.ShowLeaderboard();

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        Cursor.lockState = CursorLockMode.None;

        if (_setObserveCameraAfterEnd)
        {
            Camera.main.transform.position = _endRoundCamPoint.position;
            Camera.main.transform.rotation = _endRoundCamPoint.rotation;
        }

        StopAllCoroutines();
        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(_waitAfterEnding);

        if (PhotonNetwork.IsMasterClient)
        {
            if (_isRepetitive)
            {
                if (_switchMapAfterEnd && ConnectionManager.Instance.maps.Length > 1)
                {
                    LoadOtherRandomScene();
                }
                else
                {
                    RestartGameSend();
                }
            }
            else
            {
                LeaveRoomSend();
            }
        }
    } 

    public GameState GetGameState()
    {
        return _gameState;
    }
    #endregion

    private IEnumerator SyncTimeRoutine()
    {
        while (PhotonNetwork.IsMasterClient)
        {
            SyncTimeSendAll();
            yield return new WaitForSeconds(TimeSyncInterval);
        }
    }

    private void LoadOtherRandomScene()
    {
        string currentMapName = SceneManager.GetActiveScene().name;
        string randomMapName;

        do
        {
            randomMapName = GetRandomMapName();
        }
        while (randomMapName == currentMapName);

        PhotonNetwork.LoadLevel(randomMapName);
    }

    private string GetRandomMapName()
    {
        return ConnectionManager.Instance.maps[UnityEngine.Random.Range(0, ConnectionManager.Instance.maps.Length)];
    }
    #endregion
}


[Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string name, int actor, int kills, int deaths)
    {
        this.name = name;
        this.actor = actor;
        this.kills = kills;
        this.deaths = deaths;
    }
}
