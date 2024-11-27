using Photon.Pun;
using UnityEngine;

public class WeaponSync : MonoBehaviour, IPunObservable
{
    private PlayerController _playerController;

    private void Start()
    {
        _playerController = GetComponent<PlayerController>();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (_playerController == null)
        {
            Debug.Log("PlayerController is not assigned in WeaponSync.");
            return;
        }

        if (stream.IsWriting)
        {
            stream.SendNext(_playerController.SelectedGunIndex);
        }
        else
        {
            int gunIndex = (int)stream.ReceiveNext();
            if (_playerController.SelectedGunIndex != gunIndex)
            {
                _playerController.SelectedGunIndex = gunIndex;
                _playerController.ActivateSelectedGun();
            }

        }
    }
}
