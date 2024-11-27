using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private Transform _viewPoint;
    [SerializeField, Range(0f, 1f)] private float _mouseSensitivity = .1f;
    [SerializeField] private float _maxVerticalViewAngle = 70f;
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 8f;
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private Transform _canJumpCheckPoint;
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private List<Gun> _guns;
    [SerializeField] private Transform _gunHolder;
    [SerializeField] private Animator _anim;
    [SerializeField] private GameObject _playerModel;
    [SerializeField] private bool _invertedLook;
    [SerializeField] private float _defaultCameraAngle = 60f;
    [SerializeField] private PlayerSoundManager _playerSound;
    #endregion

    #region Private Fields
    private CharacterController _characterController;
    private PhotonView _photonView;
    private Camera _camera;
    private Renderer _playerRenderer;
    private Vector3 _movementVector;
    private Vector3 _currentDirection = Vector3.zero;
    private Vector2 _initialPosition;
    private Vector2 _mouseInput, _movementInput;
    private float _xRotation;
    private float _mouseScrollInput;
    private float _currentSpeed;
    private float _gunHolderXPos;
    private int _currentHealth;
    private int _selectedGunIndex;
    private bool _canJump;
    private bool _isSprinting;
    #endregion

    #region Constants
    private const float GravityModifier = 3f;
    private const float MaxDistanceCanJumpCheck = 0.3f;
    private const float MovementAccelerationChangeTime = 0.25f;
    private const float MovementSlowDownChangeTime = 0.1f;
    private const float CameraZoomChangeTime = 7f;
    private const float DirectionChangeTime = 10f;
    private const float LerpTolerance = 0.01f;
    private const float SpeedToPlayStepSound = 3.5f;
    private const float AimingGunXPosValue = 0f;
    private const string MoveAction = "Move";
    private const string SprintAction = "Sprint";
    private const string JumpAction = "Jump";
    private const string LookAction = "Look";
    private const string FireAction = "Fire";
    private const string AimDownSightsAction = "AimDownSights";
    private const string SwitchWeaponAction = "SwitchWeapon";
    private const string SwitchWeaponByNumberAction = "SwitchWeaponByNumber";
    private const string PlayerActionMap = "Player";
    #endregion

    #region Properties
    public static PlayerInput LocalPlayerInput { get; private set; }

    public int SelectedGunIndex
    {
        get => _selectedGunIndex;
        set
        {
            if (value >= 0 && value < _guns.Count)
            {
                _selectedGunIndex = value;
            }
            else if (value < 0)
            {
                _selectedGunIndex = 0;
            }
            else if (value >= _guns.Count)
            {
                _selectedGunIndex = _guns.Count - 1;
            }
        }
    }
    #endregion

    #region Unity Event Methods

    private void Start()
    {
        InitializeComponents();

        if (_photonView.IsMine)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }


        _xRotation = _viewPoint.localRotation.eulerAngles.x;
        _gunHolderXPos = _gunHolder.localPosition.x;

        _selectedGunIndex = 0;
        ActivateSelectedGun();
        SetSkin();
    }

    private void Update()
    {
        if (_photonView.IsMine)
        {
            CanJumpCheck();

            ProcessMouseInput();
            ProcessKeyboardInput();
            ProcessShootingInput();

            UpdateAnimations();
        }
        else
        {
            SyncWeaponToHand();
        }
    }

    private void LateUpdate()
    {
        if (_photonView.IsMine && MatchManager.Instance.GetGameState() == MatchManager.GameState.Playing)
        {
            SetCameraFollowPosition();
            HandlePlayerSound();
        }
    }

    private void OnEnable()
    {
        if (LocalPlayerInput != null && _photonView != null && _photonView.IsMine)
        {
            LocalPlayerInput.enabled = true;
        }

        SkinManager.Instance.OnSkinChanged += ChangeSkin;
    }

    private void OnDisable()
    {
        GetComponent<PlayerInput>().enabled = false;
        SkinManager.Instance.OnSkinChanged -= ChangeSkin;
    }

    #endregion

    #region Initialization Methods
    private void InitializeComponents()
    {
        _characterController = GetComponent<CharacterController>();
        _photonView = GetComponent<PhotonView>();
        _playerRenderer = _playerModel.GetComponent<Renderer>();
    }

    private void SetupLocalPlayer()
    {
        if (!UIController.Instance.IsInOptionsMenu)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            LocalPlayerInput.SwitchCurrentActionMap(UIController.UIActionMap);
        }

        LocalPlayerInput = GetComponent<PlayerInput>();
        _camera = Camera.main;
        //_playerModel.SetActive(false);

        _currentHealth = _maxHealth;
        UIController.Instance.SetHealthValue((float)_currentHealth / _maxHealth);
    }

    private void SetupRemotePlayer()
    {
        GetComponent<PlayerInput>().enabled = false;
    }
    #endregion

    #region Input Handling
    private void ProcessMouseInput()
    {
        _mouseInput = LocalPlayerInput.actions[LookAction].ReadValue<Vector2>() * _mouseSensitivity;
        _mouseScrollInput = LocalPlayerInput.actions[SwitchWeaponAction].ReadValue<float>();
        bool isAiming = LocalPlayerInput.actions[AimDownSightsAction].IsPressed();

        RotatePlayer();

        if (_mouseScrollInput != 0)
        {
            SwitchWeapon();
        }

        AimDownSights(isAiming);
    }

    private void RotatePlayer()
    {
        Vector3 newRotation = transform.rotation.eulerAngles;
        newRotation.y += _mouseInput.x;

        transform.rotation = Quaternion.Euler(newRotation);

        if (_invertedLook) _mouseInput.y = -_mouseInput.y;

        _xRotation -= _mouseInput.y;
        _xRotation = Mathf.Clamp(_xRotation, -_maxVerticalViewAngle, _maxVerticalViewAngle);

        _viewPoint.localRotation = Quaternion.Euler(_xRotation, _viewPoint.localRotation.eulerAngles.y, _viewPoint.localRotation.eulerAngles.z);
    }

    private void SwitchWeapon()
    {
        if (_mouseScrollInput > 0)
        {
            _selectedGunIndex++;

            if (_selectedGunIndex >= _guns.Count)
            {
                _selectedGunIndex = 0;
            }
        }
        else if (_mouseScrollInput < 0)
        {
            _selectedGunIndex--;

            if (_selectedGunIndex < 0)
            {
                _selectedGunIndex = _guns.Count - 1;
            }
        }

        ActivateSelectedGun();
    }

    private void AimDownSights(bool isAiming)
    {
        float targetZoomValue = isAiming ? _guns[SelectedGunIndex].GetAdsZoomValue() : _defaultCameraAngle;
        float targetXPosValue = isAiming ? AimingGunXPosValue : _gunHolderXPos;

        if (_camera.fieldOfView == targetZoomValue)
        {
            return;
        }

        if (Mathf.Abs(targetZoomValue - _camera.fieldOfView) > LerpTolerance)
        {
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetZoomValue, CameraZoomChangeTime * Time.deltaTime);

            float newXValue = Mathf.Lerp(_gunHolder.localPosition.x, targetXPosValue, CameraZoomChangeTime * Time.deltaTime);
            _gunHolder.localPosition = new Vector3(newXValue, _gunHolder.localPosition.y, _gunHolder.localPosition.z);
        }
        else
        {
            _camera.fieldOfView = targetZoomValue;
            _gunHolder.localPosition = new Vector3(targetXPosValue, _gunHolder.localPosition.y, _gunHolder.localPosition.z);
        }
    }

    private void ProcessKeyboardInput()
    {
        _isSprinting = LocalPlayerInput.actions[SprintAction].IsPressed();
        float maxSpeed = _isSprinting ? _sprintSpeed : _movementSpeed;
        _movementInput = LocalPlayerInput.actions[MoveAction].ReadValue<Vector2>();

        _initialPosition = GetVector2Position();

        if (LocalPlayerInput.actions[MoveAction].IsPressed())
        {
            Vector3 targetDirection = (transform.forward * _movementInput.y) + (transform.right * _movementInput.x);
            targetDirection.Normalize();

            _currentDirection = Vector3.Lerp(_currentDirection, targetDirection, DirectionChangeTime * Time.deltaTime);
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, maxSpeed / MovementAccelerationChangeTime * Time.deltaTime);
        }
        else
        {
            _currentDirection = Vector3.Lerp(_currentDirection, Vector3.zero, DirectionChangeTime * Time.deltaTime);
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0, maxSpeed / MovementSlowDownChangeTime * Time.deltaTime);
        }

        if (LocalPlayerInput.actions[SwitchWeaponByNumberAction].WasPerformedThisFrame())
        {
            _selectedGunIndex = (int)LocalPlayerInput.actions[SwitchWeaponByNumberAction].ReadValue<float>() - 1;
            Debug.Log("Selected Gun: " + _selectedGunIndex);
            ActivateSelectedGun();
        }

        float yMovementValue = _movementVector.y;

        _movementVector = _currentDirection * _currentSpeed;

        _movementVector.y = yMovementValue;

        if (_canJump && LocalPlayerInput.actions[JumpAction].WasPressedThisFrame())
        {
            _movementVector.y = _jumpForce;
        }
        else if (!_characterController.isGrounded)
        {
            _movementVector.y += Physics.gravity.y * GravityModifier * Time.deltaTime;
        }

        _characterController.Move(_movementVector * Time.deltaTime);
    }

    private void ProcessShootingInput()
    {
        if (LocalPlayerInput.actions[FireAction].WasPressedThisFrame())
        {
            _guns[_selectedGunIndex].StartShooting();
        }
        else if (LocalPlayerInput.actions[FireAction].WasReleasedThisFrame() || LocalPlayerInput.currentActionMap.name != PlayerActionMap)
        {
            _guns[_selectedGunIndex].StopShooting();
        }
    }

    #endregion

    #region Weapon Management
    public void ActivateSelectedGun()
    {
        for (int i = 0; i < _guns.Count; i++)
        {
            _guns[i].gameObject.SetActive(i == SelectedGunIndex);
        }
    }

    public void SendGunShot()
    {
        _photonView.RPC(nameof(PlayGunShotFX), RpcTarget.Others);
    }

    [PunRPC]
    private void PlayGunShotFX()
    {
        _guns[SelectedGunIndex].ShowMuzzleFlash();
        _guns[SelectedGunIndex].PlayShotSFX();
    }
    #endregion

    #region Health and Death
    public void TakeDamage(int damage, string damagerName, int damagerActorNumber, Vector3 hitPosition)
    {
        _photonView.RPC(nameof(TakeDamageRPC), RpcTarget.All, damage, damagerName, damagerActorNumber, hitPosition);
    }

    [PunRPC]
    private void TakeDamageRPC(int damage, string damagerName, int damagerActorNumber, Vector3 hitPosition)
    {
        if (_photonView.IsMine)
        {
            Debug.Log(PhotonNetwork.NickName + " got damaged by " + damagerName);
            _currentHealth -= damage;

            Debug.Log("Health: " + _currentHealth);
            UIController.Instance.SetHealthValue((float)_currentHealth / _maxHealth);

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;

                Die();
                UIController.Instance.ShowDeathScreen(damagerName);
                MatchManager.Instance.UpdateStatsSend(damagerActorNumber, MatchManager.UpdateStatsOptions.UpdateKills);
                SpawnManager.Instance.DestroyPlayer();
            }
        }

        SpawnManager.Instance.SpawnHitEffect(hitPosition);
    }

    private void Die()
    {
        _photonView.RPC(nameof(SpawnDeathEffectRPC), RpcTarget.All);
    }

    [PunRPC]
    private void SpawnDeathEffectRPC()
    {
        SpawnManager.Instance.SpawnDeathEffect(transform.position);
    }
    #endregion

    #region Skins
    public void ChangeSkin(int playerId)
    {
        Debug.Log("[CHANGE SKIN EVENT] for id: " + playerId + " at: " + gameObject.name);

        if (_photonView == null)
        {
            Debug.Log("Photon View is NULL at " + gameObject.name);
            return;
        }

        if (_photonView.Owner.ActorNumber == playerId)
        {
            SetSkin();
        }
    }

    private void SetSkin()
    {
        Material assignedSkin = SkinManager.Instance.GetSkinForPlayer(_photonView.Owner.ActorNumber);

        if (assignedSkin != null)
        {
            _playerRenderer.material = assignedSkin;
        }
    }
    #endregion

    #region Helper Methods
    private void UpdateAnimations()
    {
        _anim.SetBool("grounded", _canJump);
        _anim.SetFloat("speed", _movementInput.magnitude);
    }

    private void SetCameraFollowPosition()
    {
        _camera.transform.position = _viewPoint.position;
        _camera.transform.rotation = _viewPoint.rotation;
    }

    private void CanJumpCheck()
    {
        _canJump = Physics.Raycast(_canJumpCheckPoint.position, Vector3.down, MaxDistanceCanJumpCheck, _groundLayers.value);
    }

    private void HandlePlayerSound()
    {
        float movedDistance = (_initialPosition - GetVector2Position()).magnitude;
        float distanceToPlaySound = SpeedToPlayStepSound * Time.deltaTime;

        if (movedDistance > distanceToPlaySound && _canJump)
        {
            if (_playerSound.IsUpdatePlayingStateNeeded(_isSprinting))
            {
                //Debug.Log("[SEND PLAY RPC]");
                _photonView.RPC(nameof(PlayStepSound), RpcTarget.All, _isSprinting);
            }
        }
        else if (_playerSound.IsPlaying())
        {
            //Debug.Log("[SEND STOP RPC]");
            _photonView.RPC(nameof(StopStepSound), RpcTarget.All);
        }
    }

    [PunRPC]
    private void PlayStepSound(bool isFast)
    {
        _playerSound.PlayFootstepSound(isFast);
    }

    [PunRPC]
    private void StopStepSound()
    {
        _playerSound.StopFootstepSound();
    }

    private void SyncWeaponToHand()
    {
        _guns[SelectedGunIndex].SyncToHand();
    }

    private Vector2 GetVector2Position()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    public PhotonView GetPhotonView()
    {
        return _photonView;
    }
    #endregion
}
