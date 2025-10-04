using UnityEngine;

public class Desktop : MonoBehaviour
{
    [SerializeField] private Transform outPortal;
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Bucket") && col.TryGetComponent(out Rigidbody2D rb))
        {
            rb.transform.position = outPortal.position;
            rb.linearVelocity *= -1;
        }
    }
}
