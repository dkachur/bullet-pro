using Photon.Pun;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private bool _isAutomatic;
    [SerializeField] private float _timeBetweenShots;
    [SerializeField] private int _damage;
    [SerializeField] private float _heatPerShot;
    [SerializeField] private float _maxGunHeat;
    [SerializeField] private float _coolRate;
    [SerializeField] private float _overheatedCoolRate;
    [SerializeField] private GameObject _bulletImpactPrefab;
    [SerializeField] private GameObject _muzzleFlashGO;
    [SerializeField] private float _muzzleFlashTime;
    [SerializeField] private Transform _handPositionPoint;
    [SerializeField] private float _zHandPositionOffset;
    [SerializeField] private float _adsZoom;

    private const float BulletImpactPositionOffset = 0.002f;
    private const float PlayerHitImpactPositionOffset = 0.1f;
    private const float ShotSFXMinPitch = 0.9f;
    private const float ShotSFXMaxPitch = 1.1f;
    private const float FireDelay = 0.5f;
    private const string PlayerTag = "Player";
    private readonly Vector3 ViewportCenterPoint = new Vector3(.5f, .5f, 0f);

    private Camera _camera;
    private PhotonView _playerPhotonView;
    private PlayerController _playerController;
    private AudioSource _audioSource;
    private float _gunHeat;
    private float _lastShotTimeCounter;
    private float _disableTime;
    private bool _isShooting = false;
    private bool _isOverheated = false;
    private bool _canShoot = true;
    private bool _isPlayerPhotonViewMine = false;


    private void Awake()
    {
        _playerPhotonView = GetComponentInParent<PhotonView>();
        _playerController = GetComponentInParent<PlayerController>();
        _audioSource = GetComponent<AudioSource>();

        _isPlayerPhotonViewMine = _playerPhotonView != null && _playerPhotonView.IsMine;
    }


    private void OnEnable()
    {
        if (!_isPlayerPhotonViewMine)
        {
            return;
        }

        GunCoolDown(Time.time - _disableTime);

        if (UIController.Instance != null)
        {
            UIController.Instance.ChangeOverheatedTextState(_isOverheated);
            UIController.Instance.SetWeaponTempValue(_gunHeat / _maxGunHeat);
        }

        _muzzleFlashGO.SetActive(false);
        StartFireDelay();
    }

    private void OnDisable()
    {
        _disableTime = Time.time;
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (_isPlayerPhotonViewMine)
            ProcessShooting();
    }

    public void StartShooting()
    {
        _isShooting = true;
    }

    public void StopShooting()
    {
        _isShooting = false;
    }

    private void ProcessShooting()
    {
        _lastShotTimeCounter -= Time.deltaTime;

        if (_lastShotTimeCounter < 0)
        {
            if (_isShooting && !_isOverheated && _canShoot)
            {
                Shoot();
                GunHeatUp();
                _lastShotTimeCounter = _timeBetweenShots;
            }
        }

        GunCoolDown(Time.deltaTime);

        if (!_isAutomatic)
        {
            _isShooting = false;
        }
    }

    private void Shoot()
    {
        _playerController.SendGunShot();

        Ray ray = _camera.ViewportPointToRay(ViewportCenterPoint);

        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            ProcessHit(hitInfo);
        }

        ShowMuzzleFlash();
        PlayShotSFX();
    }

    private void ProcessHit(RaycastHit hitInfo)
    {
        if (hitInfo.collider.tag == PlayerTag)
        {
            var playerController = hitInfo.collider.gameObject.GetComponent<PlayerController>();
            if (playerController != null && playerController.GetPhotonView() != null)
            {
                playerController.TakeDamage(_damage, PhotonNetwork.NickName, PhotonNetwork.LocalPlayer.ActorNumber, hitInfo.point + (hitInfo.normal * PlayerHitImpactPositionOffset));
            }
        }
        else
        {
            Instantiate(_bulletImpactPrefab, hitInfo.point + (hitInfo.normal * BulletImpactPositionOffset), Quaternion.LookRotation(hitInfo.normal, Vector3.up));
        }
    }

    public void ShowMuzzleFlash()
    {
        StartCoroutine(ShowMuzzleFlashRoutine());
    }

    private IEnumerator ShowMuzzleFlashRoutine()
    {
        _muzzleFlashGO.SetActive(true);

        yield return new WaitForSeconds(_muzzleFlashTime);
        _muzzleFlashGO.SetActive(false);
    }

    public void PlayShotSFX()
    {
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
        _audioSource.pitch = Random.Range(ShotSFXMinPitch, ShotSFXMaxPitch);
        _audioSource.Play();
    }

    private void GunHeatUp()
    {
        _gunHeat += _heatPerShot;

        if (_gunHeat >= _maxGunHeat)
        {
            _gunHeat = _maxGunHeat;
            _isOverheated = true;
            Debug.Log($"[OVERHEATED]: Gun Heat Up: {_isOverheated}");
            Debug.Log("[OT]: Gun Heat Up");
            UIController.Instance.ChangeOverheatedTextState(true);
        }

        UIController.Instance.SetWeaponTempValue(_gunHeat / _maxGunHeat);
    }

    private void GunCoolDown(float timePassed)
    {
        if (_gunHeat == 0)
        {
            return;
        }

        _gunHeat -= (_isOverheated ? _overheatedCoolRate : _coolRate) * timePassed;

        if (_gunHeat <= 0f)
        {
            _gunHeat = 0f;
            _isOverheated = false;

            UIController.Instance.ChangeOverheatedTextState(false);
        }

        UIController.Instance.SetWeaponTempValue(_gunHeat / _maxGunHeat);
    }

    private void StartFireDelay()
    {
        _canShoot = false;
        StartCoroutine(FireDelayRoutine());
    }

    private IEnumerator FireDelayRoutine()
    {
        yield return new WaitForSeconds(FireDelay);
        _canShoot = true;
    }

    public void SyncToHand()
    {
        transform.position = new Vector3(_handPositionPoint.position.x, _handPositionPoint.position.y, _handPositionPoint.position.z);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z + _zHandPositionOffset);
    }

    public float GetAdsZoomValue()
    {
        return _adsZoom;
    }
}
