using UnityEngine;

public class PlayerHead : MonoBehaviour
{
    [SerializeField] private PlayerBalance[] balances;
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Bucket"))
        {
            foreach (var b in balances)
            {
                b.alive = false;
            }
            Debug.Log("Hit head with bucket");
        }
    }
}