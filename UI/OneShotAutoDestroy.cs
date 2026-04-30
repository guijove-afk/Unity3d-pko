using UnityEngine;

public class OneShotAutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }
}
