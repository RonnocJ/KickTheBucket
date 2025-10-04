using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerKicker : MonoBehaviour
{
    [SerializeField] private float kickMultiplier;
    [SerializeField] private float trajectoryTimestep;
    [SerializeField] private float slowdownThreshold;
    [SerializeField] private float slowdownMaxDistance;
    [SerializeField] private Vector2 maxKick;
    [SerializeField] private LayerMask slowdownMask;
    [SerializeField] private LayerMask fanMask;
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
        PInputManager.root.actions[PlayerActionType.Kick].bAction += () =>
        {
            anim.enabled = true;
            anim.SetTrigger("kick");
        };
    }
    private void FixedUpdate()
    {
        if (_dirInput.x != 0f) _inputXAccel *= 1f + 0.025f * Mathf.Abs(_dirInput.x);
        else _inputXAccel = 1f;

        if (_dirInput.y != 0f) _inputYAccel *= 1f + 0.025f * Mathf.Abs(_dirInput.y);
        else _inputYAccel = 1f;

        _kickDir += new Vector2(_dirInput.x * _inputXAccel, _dirInput.y * _inputYAccel) * Time.fixedDeltaTime * 0.25f;
        _kickDir = new Vector2(Mathf.Clamp(_kickDir.x, 0.1f, maxKick.x), Mathf.Clamp(_kickDir.y, 0.1f, maxKick.y));

        UpdateTrajectoryPath();

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
    private void UpdateTrajectoryPath()
{
    Vector2 pos = ballRb.position;
    Vector2 vel = _kickDir * kickMultiplier / ballRb.mass;

    var points = new List<Vector3>(75) { pos };

    bool stopped = false;

    float ballRadius = ballCol.radius * ballRb.transform.lossyScale.x;

    for (int i = 1; i < 75; i++)
    {
        if (stopped)
        {
            points.Add(points[points.Count - 1]);
            continue;
        }

        // 1) apply gravity
        vel += Physics2D.gravity * trajectoryTimestep;

        // 2) sample fan forces along the segment we'll travel this step:
        // compute tentative newPos for the segment (before fans) to get dir
        Vector2 tentativePos = pos + vel * trajectoryTimestep;
        Vector2 dir = tentativePos - pos;
        float dist = dir.magnitude;

        if (dist > Mathf.Epsilon)
        {
            RaycastHit2D[] fanHits = Physics2D.CircleCastAll(pos, ballRadius, dir.normalized, dist, fanMask);
            if (fanHits != null && fanHits.Length > 0)
            {
                foreach (var fh in fanHits)
                {
                    if (fh.collider.TryGetComponent(out Fan fanComponent))
                    {
                        Vector2 fanForce = fanComponent.GetForceVector();
                        vel += fanForce / ballRb.mass * trajectoryTimestep;
                    }
                }

                tentativePos = pos + vel * trajectoryTimestep;
                dir = tentativePos - pos;
                dist = dir.magnitude;
            }
        }
        else
        {
            Collider2D[] overlappedFans = Physics2D.OverlapCircleAll(pos, ballRadius, fanMask);
            if (overlappedFans != null && overlappedFans.Length > 0)
            {
                foreach (var fc in overlappedFans)
                {
                    if (fc.TryGetComponent(out Fan fanComponent))
                    {
                        Vector2 fanForce = fanComponent.GetForceVector();
                        vel += fanForce / ballRb.mass * trajectoryTimestep;
                    }
                }

                tentativePos = pos + vel * trajectoryTimestep;
                dir = tentativePos - pos;
                dist = dir.magnitude;
            }
        }

        vel /= 1f + ballRb.linearDamping * trajectoryTimestep;

        Vector2 newPos = pos + vel * trajectoryTimestep;
        dir = newPos - pos;
        dist = dir.magnitude;

        if (dist > Mathf.Epsilon)
        {
            RaycastHit2D predHit = Physics2D.CircleCast(pos, ballRadius, dir.normalized, dist, slowdownMask);
            if (predHit.collider != null)
            {
                points.Add(predHit.point);

                Vector2 normal = predHit.normal;

                float e = predHit.collider.sharedMaterial != null ? predHit.collider.sharedMaterial.bounciness : 0f;
                float friction = predHit.collider.sharedMaterial != null ? predHit.collider.sharedMaterial.friction : 0f;

                float vDotN = Vector2.Dot(vel, normal);
                Vector2 vNormal = vDotN * normal;
                Vector2 vTangent = vel - vNormal;

                Vector2 newNormal = -vNormal * e;

                float frictionFactor = Mathf.Clamp01(1f - friction);
                Vector2 newTangent = vTangent * frictionFactor;

                vel = newTangent + newNormal;
                vel /= 1f + ballRb.linearDamping * trajectoryTimestep;

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

    // set line renderer
    pathLr.positionCount = points.Count;
    for (int i = 0; i < points.Count; i++)
        pathLr.SetPosition(i, points[i]);
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
