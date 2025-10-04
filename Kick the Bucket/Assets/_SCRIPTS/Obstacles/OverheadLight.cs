using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OverheadLight : MonoBehaviour
{
    [SerializeField] private Light2D lightSource;
    [SerializeField] private Light2D wallLight;
    [SerializeField] private ParticleSystem shatterParticles;
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Bucket"))
        {
            lightSource.intensity = 0;
            wallLight.intensity -= 0.1f;
            shatterParticles.Play();
        }   
    }
}