using UnityEngine;

public class Chair : MonoBehaviour
{
    [SerializeField] private float magnitudeThreshold;
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Bucket") && col.TryGetComponent(out Rigidbody2D rb) && rb.linearVelocity.magnitude > magnitudeThreshold)
        {
            rb.linearVelocity = ((Vector2)transform.up + new Vector2(rb.linearVelocity.x * 0.01f, 0f)) * rb.linearVelocity.magnitude;
        }
    }
}
