using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

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

    [SerializeField] private TextMeshProUGUI _overheatedText;
    [SerializeField] private Slider _weaponTempSlider;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private GameObject _deathScreen;
    [SerializeField] private TextMeshProUGUI _killedByText;
    [SerializeField] private GameObject _endRoundScreen;
    [SerializeField] private GameObject _leaderboard;
    [SerializeField] private LeaderboardPlayerItem _playerItem;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private GameObject _optionsMenu;

    private List<LeaderboardPlayerItem> _playersLB = new List<LeaderboardPlayerItem>();

    private const string KilledByPrefix = "By ";
    private const string ShowOptionsMenuAction = "ShowOptionsMenu";
    private const string CloseOptionsMenuAction = "CloseOptionsMenu";
    private const string ShowLeaderboardAction = "ShowLeaderboard";
    private const string PlayerActionMap = "Player";
    public const string UIActionMap = "UI";

    public bool IsInOptionsMenu { get; private set; } = false;


    private void Start()
    {
        HideAll();
    }

    private void Update()
    {
        if (PlayerController.LocalPlayerInput != null)
        {
            CheckForOptionsMenuInput();
            CheckForLeaderboardInput();
        }
    }

    #region UI Input Methods
    private void CheckForOptionsMenuInput()
    {
        if (PlayerController.LocalPlayerInput.actions[ShowOptionsMenuAction].WasPressedThisFrame())
        {
            ActivateOptionsMenu();
        }
        else if (PlayerController.LocalPlayerInput.actions[CloseOptionsMenuAction].WasPressedThisFrame())
        {
            DeactivateOptionsMenu();
        }
    }

    private void CheckForLeaderboardInput()
    {
        if (PlayerController.LocalPlayerInput.actions[ShowLeaderboardAction].WasPressedThisFrame()
            && MatchManager.Instance.GetGameState() != MatchManager.GameState.Ending)
        {
            ShowLeaderboard();
        }
        else if (_leaderboard.activeInHierarchy
            && !PlayerController.LocalPlayerInput.actions[ShowLeaderboardAction].IsPressed()
            && MatchManager.Instance.GetGameState() != MatchManager.GameState.Ending)
        {
            HideLeaderboard();
        }
    }
    #endregion

    #region Change State Methods

    private void ActivateOptionsMenu()
    {
        ShowOptionsMenu();

        Cursor.lockState = CursorLockMode.None;
        PlayerController.LocalPlayerInput.SwitchCurrentActionMap(UIActionMap);
        IsInOptionsMenu = true;
    }

    private void DeactivateOptionsMenu()
    {
        HideOptionsMenu();

        Cursor.lockState = CursorLockMode.Locked;
        PlayerController.LocalPlayerInput.SwitchCurrentActionMap(PlayerActionMap);
        IsInOptionsMenu = false;
    }

    public void ChangeOverheatedTextState(bool state)
    {
        _overheatedText.gameObject.SetActive(state);
    }

    public void SetWeaponTempValue(float value)
    {
        _weaponTempSlider.value = value;
    }

    public void SetHealthValue(float value)
    {
        _healthSlider.value = value;
    }

    public void ShowDeathScreen(string killedByName)
    {
        _deathScreen.SetActive(true);
        _killedByText.text = KilledByPrefix + killedByName;
    }

    public void HideDeathScreen()
    {
        _deathScreen.SetActive(false);
    }

    public void ShowLeaderboard()
    {
        _leaderboard.SetActive(true);
    }

    public void HideLeaderboard()
    {
        _leaderboard.SetActive(false);
    }

    public void ShowEndRoundScreen()
    {
        _endRoundScreen.SetActive(true);
    }

    public void HideEndRoundScreen()
    {
        _endRoundScreen.SetActive(false);
    }

    public void ShowOptionsMenu()
    {
        _optionsMenu.SetActive(true);
    }

    public void HideOptionsMenu()
    {
        _optionsMenu.SetActive(false);
    }

    public void HideAll()
    {
        HideDeathScreen();
        HideEndRoundScreen();
        HideLeaderboard();
        HideOptionsMenu();
        ChangeOverheatedTextState(false);
    }
    #endregion

    #region UI Buttons Callbacks

    public void ResumeGame()
    {
        DeactivateOptionsMenu();
    }

    public void LoadMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    #endregion

    #region Update UI Methods
    public void UpdateKillsInLeaderboard(int actorNumber, int newKillsAmount)
    {
        int playerIndex = _playersLB.FindIndex(p => p.GetActorNumber() == actorNumber);

        if (playerIndex >= 0)
        {
            _playersLB[playerIndex].UpdateKills(newKillsAmount);

            var playerItem = _playersLB[playerIndex];
            _playersLB.RemoveAt(playerIndex);

            int newIndex = _playersLB.FindIndex(p => p.GetKills() < newKillsAmount); // search for new index
            if (newIndex == -1) // if there is no index for player with less kills, add to the end
            {
                newIndex = _playersLB.Count;
            }
            _playersLB.Insert(newIndex, playerItem);

            // Set index in hierarchy for leaderboard
            playerItem.transform.SetSiblingIndex(newIndex + 1); // Add 1 because first element in vertical layour is Header
        }
    }

    public void UpdateDeathsInLeaderboard(int actorNumber, int newDeathAmount)
    {
        LeaderboardPlayerItem player = _playersLB.Find(p => p.GetActorNumber() == actorNumber);
        if (player != null)
        {
            player.UpdateDeaths(newDeathAmount);
        }
    }

    public void SetLeaderboard(List<PlayerInfo> players)
    {
        foreach (LeaderboardPlayerItem playerItem in _playersLB)
        {
            Destroy(playerItem.gameObject);
        }

        _playersLB.Clear();

        var sortedPlayers = SortPlayers(players);

        foreach (PlayerInfo player in sortedPlayers)
        {
            var newPlayerLB = Instantiate(_playerItem.gameObject, _playerItem.transform.parent).GetComponent<LeaderboardPlayerItem>();
            newPlayerLB.SetPlayerInfo(player.name, player.actor);
            newPlayerLB.UpdateKills(player.kills);
            newPlayerLB.UpdateDeaths(player.deaths);

            newPlayerLB.gameObject.SetActive(true);

            _playersLB.Add(newPlayerLB);
        }
    }

    public void UpdateTimer(float time)
    {
        var timer = TimeSpan.FromSeconds(time);

        _timerText.text = $"{timer.Minutes:00}:{timer.Seconds:00}";
    }

    #endregion

    #region Helper Methods
    private List<PlayerInfo> SortPlayers(List<PlayerInfo> playersList)
    {
        return playersList.OrderByDescending(player => player.kills).ToList();
    }
    #endregion
}
