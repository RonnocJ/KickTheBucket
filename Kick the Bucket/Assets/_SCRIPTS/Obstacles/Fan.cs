using UnityEngine;

public class Fan : MonoBehaviour
{
    [SerializeField] private bool oscillating;
    [SerializeField] private float blowForce;
    [SerializeField] private float oscSpeed;
    [SerializeField] private float minAngle;
    [SerializeField] private float maxAngle;
    [SerializeField] private AnimationCurve lerpCurve;
    private bool direction;
    private float timer;
    public Vector2 GetForceVector() => -transform.up * blowForce;

    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Bucket") && col.TryGetComponent(out Rigidbody2D rb))
        {
            rb.AddForce(GetForceVector(), ForceMode2D.Force);
        }
    }

    void Update()
    {
        if (!oscillating) return;

        timer += Time.deltaTime * oscSpeed;

        if (direction)
        {
            transform.localEulerAngles = Vector3.forward * Mathf.Lerp(minAngle, maxAngle, lerpCurve.Evaluate(timer));

            if (transform.localEulerAngles.z >= maxAngle)
            {
                direction = !direction;
                timer = 0;
            }
        }
        else
        {
            transform.localEulerAngles = Vector3.forward * Mathf.Lerp(maxAngle, minAngle, lerpCurve.Evaluate(timer));

            if (transform.localEulerAngles.z <= minAngle)
            {
                direction = !direction;
                timer = 0;
            }
        }
    }
}