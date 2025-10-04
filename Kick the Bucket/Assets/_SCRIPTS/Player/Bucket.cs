using UnityEngine;

public class Bucket : MonoBehaviour
{
    [SerializeField] private float impactThreshold;
    [SerializeField] private GameObject impactEffect;
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.magnitude > impactThreshold)
        {
            var obj = Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(obj, 1f);
        }
    }
}