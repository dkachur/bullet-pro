using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviourPunCallbacks
{
    public static SpawnManager Instance { get; private set; }
    
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

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPoints;
    [SerializeField] private float _respawnTime;
    [SerializeField] private GameObject _deathEffectPrefab;
    [SerializeField] private GameObject _playerHitImpactPrefab;

    private GameObject _playerGO;
    private bool _wasSpawned = false;


    private void Start()
    {
        foreach (Transform spawnPoint in _spawnPoints)
        {
            spawnPoint.gameObject.SetActive(false);
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        if (MatchManager.Instance.GetGameState() != MatchManager.GameState.Ending && _playerGO == null)
        {
            Transform spawnPoint = GetRandomSpawnPoint();
            _playerGO = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
            _playerGO.name = PhotonNetwork.NickName;
            _wasSpawned = true;

            UIController.Instance.HideDeathScreen(); 
        }
    }

    public void DestroyPlayer()
    {
        if (_playerGO == null)
        {
            return;
        }

        Debug.Log("Destroying Player....");

        PhotonNetwork.Destroy(_playerGO);
        _playerGO = null;

        MatchManager.Instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, MatchManager.UpdateStatsOptions.UpdateDeaths);

        StartCoroutine(RespawnPlayerRoutine());
    }

    public void SpawnDeathEffect(Vector3 position)
    {
        Instantiate(_deathEffectPrefab, position, Quaternion.identity);
    }

    public void SpawnHitEffect(Vector3 position)
    {
        Instantiate(_playerHitImpactPrefab, position, Quaternion.identity);
    }

    private IEnumerator RespawnPlayerRoutine()
    {
        yield return new WaitForSeconds(_respawnTime);
        SpawnPlayer();
    }

    private Transform GetRandomSpawnPoint()
    {
        return _spawnPoints[Random.Range(0, _spawnPoints.Count)];
    }

    public override void OnJoinedRoom()
    {
        if (!_wasSpawned)
        {
            SpawnPlayer();
        }
    }
}
