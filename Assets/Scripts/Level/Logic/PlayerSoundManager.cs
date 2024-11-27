using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip _footstep;
    [SerializeField] private AudioClip _footstepFast;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _footstep;
    }

    public void PlayFootstepSound(bool isFast)
    {
        if (!IsUpdatePlayingStateNeeded(isFast))
        {
            Debug.Log("Playing Correct!");
            return;
        }

        _audioSource.Stop();
        _audioSource.loop = true;
        _audioSource.clip = isFast ? _footstepFast : _footstep;
        _audioSource.Play();
    }

    public void StopFootstepSound()
    {
        if (_audioSource != null)
        {
            _audioSource.loop = false;
        }
    }

    public bool IsUpdatePlayingStateNeeded(bool isFast)
    {
        return !_audioSource.loop || isFast != (_audioSource.clip.name == _footstepFast.name);
    }

    public bool IsPlaying()
    {
        return _audioSource.loop;
    }
}


