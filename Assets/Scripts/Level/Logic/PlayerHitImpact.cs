using UnityEngine;

public class PlayerHitImpact : MonoBehaviour
{
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.pitch = Random.Range(0.7f, 1f);
        _audioSource.Play();
    }
}
