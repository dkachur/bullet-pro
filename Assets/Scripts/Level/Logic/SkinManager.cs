using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance { get; private set; }

    [SerializeField] private Material[] _playerSkins;

    private Queue<int> _availableSkins; // Contains indexes of available skins
    private Dictionary<int, int> _playerSkinMap; // PlayerID -> SkinIndex

    public event Action<int> OnSkinChanged; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        } else
        {
            Instance = this;
        }

        _availableSkins = new Queue<int>();
        _playerSkinMap = new Dictionary<int, int>();

        for (int i = 0; i < _playerSkins.Length; i++)
        {
            _availableSkins.Enqueue(i);
        }
    }

    public void AssignSkinToPlayer(int playerId)
    {
        if (_availableSkins.Count == 0)
        {
            Debug.LogWarning("No available skins!");
            return;
        }

        int skinIndex = _availableSkins.Dequeue();
        SetSkin(playerId, skinIndex);
    }

    public void ReleaseSkin(int playerId)
    {
        if (!_playerSkinMap.ContainsKey(playerId))
        {
            Debug.Log($"Player {playerId} does not have an assigned skin.");
            return;
        }

        int skinIndex = _playerSkinMap[playerId];
        _playerSkinMap.Remove(playerId);
        _availableSkins.Enqueue(skinIndex);
    }

    public void SetSkin(int playerId, int skinIndex)
    {
        _playerSkinMap[playerId] = skinIndex;

        OnSkinChanged?.Invoke(playerId);
    }

    public Material GetSkinForPlayer(int playerId)
    {
        if (_playerSkinMap.TryGetValue(playerId, out int skinIndex))
        {
            return _playerSkins[skinIndex];
        }
        Debug.Log($"No skin found for Player {playerId}.");
        return null;
    }

    public int GetSkinIndexForPlayer(int playerId)
    {
        if (_playerSkinMap.TryGetValue(playerId, out int skinIndex))
        {
            return skinIndex;
        }
        Debug.LogWarning($"No skin found for Player {playerId}.");
        return -1;
    }

    public int[] GetSkinsQueueAsArray()
    {
        return _availableSkins.ToArray();
    }

    public void SetSkinsQueue(int[] newQueueAsArray)
    {
        _availableSkins = new Queue<int>(newQueueAsArray);
    }


    #region Debug Methods
    public void PrintQueue()
    {
        Debug.Log("QUEUE:");
        foreach (var item in _availableSkins)
        {
            Debug.Log("ITEM: " + item);
        }
    }

    public void PrintMap()
    {
        Debug.Log("MAP:");
        foreach (var item in _playerSkinMap)
        {
            Debug.Log($"{item.Key} = {item.Value}");
        }
    }
    #endregion
}
