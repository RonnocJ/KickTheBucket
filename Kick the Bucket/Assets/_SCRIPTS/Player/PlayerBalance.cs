using UnityEngine;

public class PlayerBalance : MonoBehaviour
{
    public bool alive;
    [SerializeField] private float balanceForce;
    [SerializeField] private Rigidbody2D rb;

    private void Update()
    {
       if(alive) rb.MoveRotation(Mathf.LerpAngle(rb.rotation, 0, balanceForce * Time.fixedDeltaTime));
    }
}