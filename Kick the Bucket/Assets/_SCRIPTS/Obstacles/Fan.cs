using UnityEngine;

public class Fan : MonoBehaviour
{
    [SerializeField] private float blowForce;

    public Vector2 GetForceVector() => -transform.up * blowForce;

    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Bucket") && col.TryGetComponent(out Rigidbody2D rb))
        {
            rb.AddForce(GetForceVector(), ForceMode2D.Force);
        }
    }
}