using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance { get; private set; }

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

    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [SerializeField] private GameObject _menuButtons;
    [SerializeField] private GameObject _testRoomButton;

    [SerializeField] private GameObject _createRoomPanel;
    [SerializeField] private TMP_InputField _roomNameInputField;

    [SerializeField] private GameObject _roomPanel;
    [SerializeField] private TextMeshProUGUI _roomNameText;
    [SerializeField] private GameObject _startGameButton;

    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TextMeshProUGUI _errorText;

    [SerializeField] private GameObject _findRoomPanel;
    [SerializeField] private RoomButton _roomButton;
    [SerializeField] private List<RoomButton> _roomButtons = new List<RoomButton>();

    [SerializeField] private PlayerTextUI _playerTextUI;
    [SerializeField] private List<PlayerTextUI> _playerTextsUI = new List<PlayerTextUI>();

    [SerializeField] private GameObject _enterNickNamePanel;
    [SerializeField] private TMP_InputField _nickNameInput;



    private const string NickNameKey = "nickName";
    private const string PlaceholderReminderText = "You Forgot To Fill Your Name";

    public void ShowLoadingScreenWithText(string text)
    {
        CloseUI();
        _loadingPanel.SetActive(true);
        _loadingText.text = text;
    }

    public void CloseUI()
    {
        _loadingPanel.SetActive(false);
        _menuButtons.SetActive(false);
        _createRoomPanel.SetActive(false);
        _roomPanel.SetActive(false);
        _errorPanel.SetActive(false);
        _findRoomPanel.SetActive(false);
        _enterNickNamePanel.SetActive(false);
    }

    public void ActivateCreateRoomUI()
    {
        CloseUI();
        _createRoomPanel.SetActive(true);
    }

    public void ActivateMenuButtons()
    {
        CloseUI();
        _menuButtons.SetActive(true);

#if UNITY_EDITOR
        _testRoomButton.SetActive(true);
#else
        _testRoomButton.SetActive(false);
#endif

    }

    public void ActivateRoomUIWithName(string roomName)
    {
        CloseUI();
        _roomNameText.text = roomName;
        _roomPanel.SetActive(true);
    }

    public void ActivateErrorUI(string errorMessage)
    {
        CloseUI();
        _errorText.text = errorMessage;
        _errorPanel.SetActive(true);
    }

    public void ActivateFindRoomUI()
    {
        CloseUI();
        _findRoomPanel.SetActive(true);
    }

    public void ActivateEnterNickNameUI()
    {
        CloseUI();
        _enterNickNamePanel.SetActive(true);

        if (PlayerPrefs.HasKey(NickNameKey))
        {
            _nickNameInput.text = PlayerPrefs.GetString(NickNameKey);
        }
    }

    public void ChangeStartGameButtonState(bool state)
    {
        _startGameButton.SetActive(state);
    }

    public void SetNickName()
    {
        if (!string.IsNullOrEmpty(_nickNameInput.text))
        {
            ConnectionManager.Instance.SetNickName(_nickNameInput.text);
            PlayerPrefs.SetString(NickNameKey, _nickNameInput.text);
            ActivateMenuButtons();
        } else
        {
            TMP_Text placeholderText = _nickNameInput.placeholder.GetComponent<TMP_Text>();
            placeholderText.text = PlaceholderReminderText;
            placeholderText.color = Color.red;
        }
    }

    public string GetRoomNameInputText()
    {
        return _roomNameInputField.text;
    }

    public void ShowRoomsList(List<RoomInfo> allRooms)
    {
        foreach (RoomButton roomButton in _roomButtons)
        {
            Destroy(roomButton.gameObject);
        }

        _roomButtons.Clear();

        foreach (RoomInfo room in allRooms)
        {
            if (room.PlayerCount != room.MaxPlayers)
            {
                RoomButton newRoomButton = Instantiate(_roomButton, _roomButton.transform.parent);
                newRoomButton.SetRoomInfo(room);
                newRoomButton.gameObject.SetActive(true);
                _roomButtons.Add(newRoomButton);
            }
        }
    }

    public void ShowPlayersList(Player[] players)
    {
        foreach(PlayerTextUI player in _playerTextsUI)
        {
            Destroy(player.gameObject);
        }

        _playerTextsUI.Clear();

        foreach(Player player in players)
        {
            AddPlayerToList(player);
        }
    }

    public void AddPlayerToList(Player player)
    {
        PlayerTextUI newPlayerText = Instantiate(_playerTextUI, _playerTextUI.transform.parent);

        newPlayerText.SetPlayerInfo(player);
        newPlayerText.gameObject.SetActive(true);
        _playerTextsUI.Add(newPlayerText);
    }

    public void RemovePlayerFromTheList(int playerActorNumber)
    {
        for (int i = 0; i < _playerTextsUI.Count; i++)
        {
            if (_playerTextsUI[i].GetActorNumber() == playerActorNumber)
            {
                Destroy(_playerTextsUI[i].gameObject);
                _playerTextsUI.RemoveAt(i);
                return;
            }
        }
    }



    public void QuitGame()
    {
        Application.Quit();
    }

}
