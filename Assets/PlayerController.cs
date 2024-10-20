using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;


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
public class InputSequence
{
    public string name;
    public List<KeyCode> sequence;
    public Action doMove;
    public Func<bool> canDoMove = () => true;
}

public class PlayerController : MonoBehaviour
{
    public Transform visuals;
    public float cameraLookahead = 6;
    public Transform cameraTarget;
    private Animator visualsAnimator;

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

    #region Grind Check Variables
    [Header("Grind Check")]
    public Collider2D grindCollider;
    public LayerMask grindLayer;

    private bool isGrinding = false;
    private GameObject grindObject;
    #endregion

    [Header("Nose Grab")]
    public Collider2D noseGrabCollider;
    private bool isNoseGrabbing = false;

    [Header("Movement")]
    public float pushForce = 4;
    public float ollieForce = 3;
    public float brakeDragFactor = .7f;
    public float heavyFallFactor = 1.3f;
    public bool doingManual = false;


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

    private static KeyCode FORWARDS_INPUT = (KeyCode)13;
    private static KeyCode UP_FORWARDS_INPUT = (KeyCode)8;
    private static KeyCode BACKWARDS_INPUT = (KeyCode)12;

    private List<KeyCode> contextInputs = new List<KeyCode> {
        FORWARDS_INPUT,
        UP_FORWARDS_INPUT,
        BACKWARDS_INPUT
    };

    #endregion

    [Header("Visuals")]
    public float rotationDamping = 20f;

    void Start()
    {
        // initialize possible moves -- right now its just pushing left and right
        possibleMoves = new InputSequence[] {
            new InputSequence {
                name = "Push Left",
                sequence = new List<KeyCode> { KeyCode.A, KeyCode.D },
                doMove = PushLeft,
                canDoMove = IsGrounded
            },
            new InputSequence {
                name = "Push Right",
                sequence = new List<KeyCode> { KeyCode.D, KeyCode.A },
                doMove = PushRight,
                canDoMove = IsGrounded
            },
            new InputSequence {
                name = "Ollie",
                sequence = new List<KeyCode> { KeyCode.W, KeyCode.W },
                doMove = Ollie,
                canDoMove = IsGrounded
            },
            new InputSequence {
                name = "Kick Flip",
                sequence = new List<KeyCode> { KeyCode.S, (KeyCode)UP_FORWARDS_INPUT },
                doMove = Ollie,
                canDoMove = IsGrounded
            }
        };
        rb = GetComponent<Rigidbody2D>();
        visualsAnimator = visuals.GetComponent<Animator>();
    }


    void Update()
    {
        CheckGround();
        CheckKeys();

        if (!IsGrounded() && rb.velocity.y < 0)
        {
            rb.AddForce(Vector2.down * heavyFallFactor, ForceMode2D.Force);
        }

        noseGrabCollider.enabled = isNoseGrabbing;

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
            if (rb.velocity.magnitude > 0.1f && IsMoving() && !isNoseGrabbing)
            {
                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                bool invert = rb.velocity.x < 0;

                if (invert)
                {
                    angle = angle > 0 ? angle - 180 : angle + 180;
                }

                float tilt = (Mathf.Clamp(angle, -45f, 45) + 45) / 90;
                visualsAnimator.SetFloat("Tilt", invert ? tilt : 1 - tilt);

                targetRotation = Quaternion.Euler(0, 0, Mathf.Clamp(angle, -45f, 45f));
            }
            else
            {
                visualsAnimator.SetFloat("Tilt", 0);
                targetRotation = Quaternion.Euler(0, 0, 0);
            }

        }

        if (IsMoving())
        {
            visuals.localScale = new Vector3(-Mathf.Sign(rb.velocity.x), 1, 1);
            cameraTarget.transform.localPosition = new Vector3(Mathf.Sign(rb.velocity.x) * cameraLookahead, 0, 0);
        }
        visualsAnimator.SetBool("Manual", doingManual);

        visuals.rotation = Quaternion.RotateTowards(visuals.rotation, targetRotation, rotationDamping * 360 * Time.deltaTime);
    }
    bool IsVisualsState(string state)
    {
        return visualsAnimator.GetCurrentAnimatorStateInfo(0).IsName(state);
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
        var oldIsGrounded = groundedInfo.isGrounded;

        groundedInfo.angle = 0;
        groundedInfo.distanceToGround = 0;
        groundedInfo.surfaceNormal = Vector3.up;
        groundedInfo.groundHitPosition = transform.position;
        groundedInfo.floorObject = null;
        groundedInfo.isGrounded = false;


        if (groundCheckCooldown <= 0)
        {
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayer
            };

            List<RaycastHit2D> hits = new List<RaycastHit2D>();
            Vector3 groundCheckStart = transform.position + transform.TransformDirection(groundCheckOrigin);
            int numHit = Physics2D.CircleCast(
                groundCheckStart,
                groundCheckRadius,
                -transform.up,
                filter,
                hits,
                groundCheckDistance
            );

            var dots = hits.Select(h => Vector2.Dot(h.point - (Vector2)groundCheckStart, transform.up));

            if (numHit > 0 && dots.Min() <= 0)
            {
                var hit = hits[Array.IndexOf(dots.ToArray(), dots.Min())];
                groundedInfo.isGrounded = true;
                groundedInfo.angle = Vector3.Angle(hit.normal, transform.up);
                groundedInfo.distanceToGround = hit.distance;
                groundedInfo.surfaceNormal = hit.normal.normalized;
                groundedInfo.groundHitPosition = hit.distance > 0 ? hit.point : transform.position;
                groundedInfo.floorObject = hit.collider.gameObject;
            }
        }

        if (groundedInfo.isGrounded != oldIsGrounded)
        {
            if (groundedInfo.isGrounded)
            {
                OnGroundEnter();
            }
            else
            {
                OnGroundExit();
            }
        }
    }

    bool IsGrounded() => groundedInfo.isGrounded;
    bool IsMoving() => rb.velocity.magnitude > 0.1f;
    void OnGroundEnter()
    {
        isNoseGrabbing = false;
        visualsAnimator.Play("Ground");
    }

    void OnGroundExit()
    {
        visualsAnimator.Play("Air");
    }

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

    void PushLeft()
    {
        Push(Vector2.left * pushForce);
        visualsAnimator.Play("Push", 0, 0);
    }
    void PushRight()
    {
        Push(Vector2.right * pushForce);
        visualsAnimator.Play("Push", 0, 0);
    }

    void Push(Vector2 pushVector, ForceMode2D forceMode = ForceMode2D.Impulse)
    {
        if (!IsGrounded()) return;

        Vector2 normal = groundedInfo.surfaceNormal;
        Vector2 projection = Vector2.Dot(pushVector, normal) * normal;
        Vector2 projectedPushVector = pushVector - projection;

        rb.AddForce(projectedPushVector, forceMode);
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
            if (contextInputs.Contains(key))
            {
                if (Input.GetKey(key))
                {
                    currentSequence.Add(key);
                    sequenceTimeout = sequenceTimeoutDuration;
                    break;
                }
            }
            else if (Input.GetKeyDown(key))
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
            return;
        }


        if (!IsGrounded() && Input.GetKeyDown(KeyCode.S))
        {
            visualsAnimator.Play("NoseGrab", 0, 0);
            isNoseGrabbing = true;
        }

        bool shouldManual = false;
        // handle manual (s + backwards input)
        if (IsGrounded())
        {
            if (IsMoving())
            {
                var backwardsInput = Mathf.Sign(rb.velocity.x) < 0 ? Input.GetKey(KeyCode.D) : Input.GetKey(KeyCode.A);
                // if (Vector2.Angle(groundedInfo.surfaceNormal, Vector2.up) < 20)
                // {
                shouldManual = backwardsInput && Input.GetKey(KeyCode.S);
                // }
            }

            if (shouldManual != doingManual)
            {
                // on manual start
                doingManual = shouldManual;
            }
        }

        if (doingManual) { return; }

        // handle braking (a || d input)
        if (IsGrounded())
        {
            if (!doingManual)
            {
                if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && IsMoving())
                {
                    visualsAnimator.SetFloat("Brake", 1);
                    // brake along perpendicular to the ground
                    Vector2 brakeDirection = Vector2.Perpendicular(groundedInfo.surfaceNormal) * Mathf.Sign(rb.velocity.x);
                    rb.AddForce(brakeDirection * brakeDragFactor, ForceMode2D.Force);
                }
                else
                {
                    visualsAnimator.SetFloat("Brake", 0);
                }
            }
        }
    }

    void OnSequenceTimedOut()
    {
        currentSequence.Clear();
    }
    #endregion
}