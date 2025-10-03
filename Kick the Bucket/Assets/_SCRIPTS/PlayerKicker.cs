using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerKicker : MonoBehaviour
{
    [SerializeField] private float kickMultiplier;
    [SerializeField] private float slowdownThreshold;
    [SerializeField] private float slowdownMaxDistance;
    [SerializeField] private Vector2 maxKick;
    [SerializeField] private LayerMask slowdownMask;
    [SerializeField] private Rigidbody2D ballRb;
    [SerializeField] private CircleCollider2D ballCol;
    [SerializeField] private Animator anim;
    [SerializeField] private LineRenderer pathLr;

    private float _currentTimeScale = 1;
    private float _inputXAccel = 1;
    private float _inputYAccel = 1;
    private Vector2 _kickDir = Vector2.one;
    private Vector2 _dirInput => PInputManager.root.actions[PlayerActionType.Direction].v2Value;

    private void Start()
    {
        PInputManager.root.actions[PlayerActionType.Kick].bAction += () => anim.SetTrigger("kick");
    }
    private void FixedUpdate()
    {
        if (_dirInput.x != 0) _inputXAccel *= 1.025f * Mathf.Abs(_dirInput.x);
        else _inputXAccel = 1;

        if (_dirInput.y != 0) _inputYAccel *= 1.025f * Mathf.Abs(_dirInput.y);
        else _inputYAccel = 1;

        _kickDir += new Vector2(_dirInput.x * _inputXAccel, _dirInput.y * _inputYAccel) * Time.fixedDeltaTime * 0.25f;
        _kickDir = new Vector2(Mathf.Clamp(_kickDir.x, 0.1f, maxKick.x), Mathf.Clamp(_kickDir.y, 0.1f, maxKick.y));

        Vector2 initialVelocity = ballRb.linearVelocity;
        float mass = ballRb.mass;
        Vector2 impulse = _kickDir * kickMultiplier;
        initialVelocity += impulse / mass;

        Vector2 pos = ballRb.position;
        Vector2 vel = initialVelocity;

        var points = new List<Vector3>(75)
        {
            pos
        };

        bool stopped = false;

        float ballRadius = ballCol.radius * ballRb.transform.lossyScale.x;

        for (int i = 1; i < 75; i++)
        {
            if (stopped)
            {
                points.Add(points[points.Count - 1]); // repeat last
                continue;
            }

            vel += Physics2D.gravity * 0.01f;
            vel *= 1f / (1f + ballRb.linearDamping * 0.01f);
            Vector2 newPos = pos + vel * 0.01f;

            Vector2 dir = newPos - pos;
            float dist = dir.magnitude;
            if (dist > Mathf.Epsilon)
            {
                RaycastHit2D predHit = Physics2D.CircleCast(pos, ballRadius, dir.normalized, dist, slowdownMask);
                if (predHit.collider != null)
                {
                    points.Add(predHit.point);

                    Vector2 normal = predHit.normal;

                    float e = 0.5f;
                    if (predHit.collider.sharedMaterial != null) e = predHit.collider.sharedMaterial.bounciness;

                    // Decompose velocity into normal and tangent parts:
                    float vDotN = Vector2.Dot(vel, normal);
                    Vector2 vNormal = vDotN * normal;        // component along normal
                    Vector2 vTangent = vel - vNormal;        // component tangent to surface

                    // New velocity: tangential unchanged (ignoring friction here), normal reversed * restitution
                    vel = vTangent - vNormal * e;

                    // Nudge position out of surface by radius+eps along normal
                    pos = predHit.point + normal * (ballRadius + Mathf.Epsilon);

                    continue;
                }
                else
                {
                    points.Add(newPos);
                }
            }
            else
            {
                points.Add(pos);
            }

            pos = newPos;
        }

        // Set line renderer
        pathLr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            pathLr.SetPosition(i, points[i]);

        RaycastHit2D hit = Physics2D.Raycast((Vector2)ballRb.transform.position, ballRb.linearVelocity, slowdownMaxDistance, slowdownMask);

        if (hit && ballRb.linearVelocity.magnitude > slowdownThreshold)
        {
            _currentTimeScale -= Time.fixedDeltaTime * 25f;
        }
        else if (Time.timeScale < 1f)
        {
            _currentTimeScale += Time.fixedDeltaTime * 50f;
        }

        _currentTimeScale = Mathf.Clamp(_currentTimeScale, 0.125f, 1f);

        MusicManager.root.liveSources.ForEach(c => c.pitch = Mathf.Lerp(c.pitch, _currentTimeScale, Time.deltaTime * 2.5f));
        Time.timeScale = _currentTimeScale;
    }

    public void KickBall()
    {
        pathLr.enabled = false;

        ballRb.AddForce(_kickDir * kickMultiplier, ForceMode2D.Impulse);
    }
    public void DisableAnim()
    {
        anim.enabled = false;
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Bucket"))
        {
            var rbs = GetComponentsInChildren<Rigidbody2D>();
            foreach (var rb in rbs)
            {
                rb.gravityScale = 2f;
            }
        }
    }
}
