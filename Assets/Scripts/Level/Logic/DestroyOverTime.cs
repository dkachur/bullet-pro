using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    [SerializeField] private float _lifetime = 1.5f;

    void Start()
    {
        Destroy(gameObject, _lifetime);
    }
}
