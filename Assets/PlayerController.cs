using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
class GroundedInfo
{
    public bool isGrounded;
    public float angle;
    public float distanceToGround;
    public Vector3 surfaceNormal;
    public Vector3 groundHitPosition;
    public GameObject floorObject;

    public GroundedInfo(
        bool isGrounded,
        float angle,
        float distanceToGround,
        Vector3 surfaceNormal,
        Vector3 groundHitPosition,
        GameObject floorObject
    )
    {
        this.isGrounded = isGrounded;
        this.angle = angle;
        this.distanceToGround = distanceToGround;
        this.surfaceNormal = surfaceNormal;
        this.groundHitPosition = groundHitPosition;
        this.floorObject = floorObject;
    }
}

[Serializable]
class InputSequence
{
    public string name;
    public List<KeyCode> sequence;
    public Action doMove;
    public Func<bool> canDoMove = () => true;
}

public class PlayerController : MonoBehaviour
{
    public Transform visuals;

    #region Ground Check Variables
    [Header("Ground Check")]
    public float groundCheckCooldownDuration = 0.1f;
    public LayerMask groundLayer;
    public Vector3 groundCheckOrigin;
    public float groundCheckRadius;
    public float groundCheckDistance;

    private Rigidbody2D rb;
    private GroundedInfo groundedInfo = new GroundedInfo(
        isGrounded: false,
        angle: 0,
        distanceToGround: 0,
        surfaceNormal: Vector3.up,
        groundHitPosition: Vector3.zero,
        floorObject: null
    );

    private float groundCheckCooldown;
    #endregion

    [Header("Movement")]
    public float pushForce = 4;
    public float ollieForce = 3;

    #region Input Management Variables
    [Header("Input Management")]
    public float sequenceTimeoutDuration = 0.3f;        // how long a key must be pressed to maintain a sequence

    public float sequenceTimeout;
    private InputSequence[] possibleMoves;
    public List<KeyCode> currentSequence = new List<KeyCode>();
    private List<KeyCode> keysToCheck = new List<KeyCode> {
        KeyCode.W,
        KeyCode.A,
        KeyCode.S,
        KeyCode.D
    };
    #endregion

    [Header("Visuals")]
    public float rotationDamping = 20f;

    void Awake()
    {
        // initialize possible moves -- right now its just pushing left and right
        possibleMoves = new InputSequence[] {
            new InputSequence {
                name = "Push left",
                sequence = new List<KeyCode> { KeyCode.S, KeyCode.A },
                doMove = PushLeft,
                canDoMove = IsGrounded
            },
            new InputSequence {
                name = "Push right",
                sequence = new List<KeyCode> { KeyCode.S, KeyCode.D },
                doMove = PushRight,
                canDoMove = IsGrounded
            },
            new InputSequence {
                name = "Ollie",
                sequence = new List<KeyCode> { KeyCode.W, KeyCode.W },
                doMove = Ollie,
                canDoMove = IsGrounded
            },
        };
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    void Update()
    {
        CheckGround();
        CheckKeys();

        UpdateVisuals();

        UpdateTimeouts();
    }

    void UpdateVisuals()
    {
        Quaternion targetRotation = visuals.rotation;
        if (IsGrounded())
        {
            targetRotation = Quaternion.Euler(0, 0, Mathf.Atan2(groundedInfo.surfaceNormal.y, groundedInfo.surfaceNormal.x) * Mathf.Rad2Deg - 90);
        }
        else
        {
            if (rb.velocity.magnitude > 0.1f && Mathf.Abs(rb.velocity.x) > 0.1f)
            {
                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                if (rb.velocity.x < 0) { angle += 180; }
                targetRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        visuals.rotation = Quaternion.RotateTowards(visuals.rotation, targetRotation, rotationDamping * 360 * Time.deltaTime);
    }
    void UpdateTimeouts()
    {
        var prevSequenceTimeout = sequenceTimeout;
        sequenceTimeout = Mathf.Max(0, sequenceTimeout - Time.deltaTime);
        if (sequenceTimeout == 0 && prevSequenceTimeout != 0)
        {
            OnSequenceTimedOut();
        }

        groundCheckCooldown = Mathf.Max(0, groundCheckCooldown - Time.deltaTime);
    }

    #region Ground Checking

    // circle cast to get relevant info
    void CheckGround()
    {
        if (groundCheckCooldown > 0)
        {
            groundedInfo.isGrounded = false;
            groundedInfo.angle = 0;
            groundedInfo.distanceToGround = 0;
            groundedInfo.surfaceNormal = Vector3.up;
            groundedInfo.groundHitPosition = transform.position;
            groundedInfo.floorObject = null;
            return;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position + transform.TransformDirection(groundCheckOrigin),
            groundCheckRadius,
            -transform.up,
            groundCheckDistance,
            groundLayer
        );

        groundedInfo.isGrounded = hit.collider != null;
        groundedInfo.angle = Vector3.Angle(hit.normal, transform.up);
        groundedInfo.distanceToGround = hit.distance;
        groundedInfo.surfaceNormal = hit.normal.normalized;
        groundedInfo.groundHitPosition = hit.distance > 0 ? hit.point : transform.position;
        groundedInfo.floorObject = hit.collider != null ? hit.collider.gameObject : null;
    }
    bool IsGrounded() => groundedInfo.isGrounded;

    void OnCollisionExit2D(Collision2D collision)
    {
        if (groundLayer == (groundLayer | (1 << collision.gameObject.layer)))
        {
            CheckGround();
            if (!IsGrounded())
            {
                DoGroundCheckCooldown();
            }
        }
    }

    void DoGroundCheckCooldown()
    {
        groundCheckCooldown = groundCheckCooldownDuration;
    }

    #endregion

    #region Input Actions

    void PushLeft() { Push(Vector2.left * pushForce); }
    void PushRight() { Push(Vector2.right * pushForce); }

    void Push(Vector2 pushVector)
    {
        if (!IsGrounded()) return;

        Vector2 normal = groundedInfo.surfaceNormal;
        Vector2 projection = Vector2.Dot(pushVector, normal) * normal;
        Vector2 projectedPushVector = pushVector - projection;

        rb.AddForce(projectedPushVector, ForceMode2D.Impulse);
    }

    void Ollie()
    {
        DoGroundCheckCooldown();
        rb.AddForce(Vector2.up * ollieForce, ForceMode2D.Impulse);
    }
    #endregion

    #region Input Management
    void CheckKeys()
    {
        foreach (KeyCode key in keysToCheck)
        {
            if (Input.GetKeyDown(key))
            {
                currentSequence.Add(key);
                sequenceTimeout = sequenceTimeoutDuration;
                break;
            }
        }

        bool didMove = false;
        foreach (InputSequence move in possibleMoves)
        {
            if (move.sequence.Count != currentSequence.Count) continue;

            bool isMatch = move.sequence.SequenceEqual(currentSequence);

            if (isMatch && move.canDoMove())
            {
                move.doMove();
                didMove = true;
                break;
            }
        }

        if (didMove)
        {
            currentSequence.Clear();
        }
    }

    void OnSequenceTimedOut()
    {
        currentSequence.Clear();
    }
    #endregion
}