using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Unity.Netcode; using UnityEngine.UI;
public enum MovementStates { Walking, Jumping, WallJumping, Falling, }
public class PlayerMovement : NetworkBehaviour
{
    [Header("Object References")]
    [SerializeField] internal Transform viewPosition;
    [SerializeField] internal Transform mainCamera;
    [SerializeField] private TMPro.TMP_Text healthText;
    [SerializeField] private Image pulse;
    internal PlayerNetwork p;
    internal Rigidbody rb;

    [Header("Camera Parameters")]
    [SerializeField] private Vector2 sensitivity;
    private Vector3 viewRotation;
    private Vector2 mouseRotation;

    [Header("Collision Parameters")] 
    [SerializeField] internal LayerMask layers;
    [SerializeField] private float bottomOffset, collisionRadius;

    [Header("Movement Parameters")]
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float gravity;
    [SerializeField] private Vector2 groundVelocityChange, 
                                     airVelocityChange;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCancelSpeed,
                                   jumpBufferTime,
                                   coyoteTime;

    [Header("Movement States")]
    public MovementStates currentState = MovementStates.Falling,
                          previousState = MovementStates.Walking;
    private float stateDuration;

    [Header("Movement Variables")]
    private Vector3 currentVelocity;
    private Vector2 viewTilt, currentTiltSpeed;
    private Coroutine jumpStop;
    private bool Grounded;
    private float jumpBuffer;

    [Header("Network References")]
    internal bool forceSpawn = false;
    private int healthPoints = 10;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<Rigidbody>().isKinematic = true;
            Destroy(viewPosition.gameObject);
            Destroy(this);
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        p = GetComponent<PlayerNetwork>();
        Cursor.lockState = CursorLockMode.Locked;
        healthText.text = "";
    }

    private void Update()
    {
        //if (p.healthPoints.Value != healthPoints)
        //{
        //    StartCoroutine(Pulse());
        //    healthPoints = p.healthPoints.Value;
        //    healthText.text = "";
        //}

        //if (p.requireSpawn.Value && forceSpawn == false) StartCoroutine(Spawn());

        // Inputs
        Vector2 mousePos    = new Vector2(Input.GetAxis("Mouse X") * sensitivity.x, Input.GetAxis("Mouse Y") * sensitivity.y);
        Vector2 input       = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool    inputting   = input != new Vector2(0, 0);


        float rayDist = 1.05f;

        // Speeds
        float   speed          = walkingSpeed,
                velocityChange = inputting ? Grounded ? groundVelocityChange.x  : airVelocityChange.x : Grounded ? groundVelocityChange.y : airVelocityChange.y;

        // Velocity
        Vector3 moveDir = (viewPosition.forward * input.y + viewPosition.right * input.x).normalized,
                velocity       = new(moveDir.x * speed, rb.velocity.y, moveDir.z * speed);

        Grounded = false;
        rb.useGravity = true;

        if (Physics.SphereCast(rb.transform.position, 0.45f, Vector3.down, out RaycastHit obj, rayDist, layers))
        {
            Grounded = true;
            if (Vector3.Angle(obj.normal, Vector3.up) > 0 && currentState != MovementStates.Jumping)
            {
                Vector3 slopeNormal = rb.transform.InverseTransformDirection(obj.normal);
                velocity = Vector3.ProjectOnPlane(new Vector3(moveDir.x, 0, moveDir.z), slopeNormal).normalized * speed;

                if (!inputting && !Input.GetKey(KeyCode.Space))
                {
                    velocity = Vector3.zero;
                    rb.useGravity = false;
                }
            }
        }

        States();
        CameraTilt();

        #region Camera

        mouseRotation.x -= mousePos.y;
        mouseRotation.y += mousePos.x;
        mouseRotation.x = Mathf.Clamp(mouseRotation.x, -89f, 89f);

        Vector3 direction = new(mouseRotation.x, transform.rotation.y, transform.rotation.z);

        viewPosition.localRotation = Quaternion.Euler(new(viewPosition.localRotation.x + viewRotation.x, mouseRotation.y, viewPosition.localRotation.z + viewRotation.z));
        mainCamera.localRotation = Quaternion.Euler(direction);

        #endregion

        rb.velocity = Vector3.SmoothDamp(rb.velocity, velocity, ref currentVelocity, velocityChange);
    }

    private void FixedUpdate()
    {
        if (!Grounded) rb.AddForce(Vector3.down * gravity, ForceMode.Impulse);

        jumpBuffer -= Time.deltaTime;
    }

    #region States
    private void States()
    {
        switch (currentState)
        {
            case MovementStates.Walking:
                {
                    RunStates(
                    Enter: null,

                    Stay: () =>
                    {
                        if (Grounded)
                        {
                            if (Input.GetKeyDown(KeyCode.Space) || jumpBuffer > 0)
                            {
                                if (jumpStop != null)
                                {
                                    StopCoroutine(jumpStop);
                                    jumpStop = null;
                                }

                                ChangeState(MovementStates.Jumping);
                            }
                        }
                        else ChangeState(MovementStates.Falling);
                    }

                    );
                }
                break;

            case MovementStates.Jumping:
                {
                    RunStates(
                    Enter: () =>
                    {
                        jumpBuffer = 0;
                        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    },

                    Stay: () =>
                    {
                        if (!Input.GetKey(KeyCode.Space) || Input.GetKeyUp(KeyCode.Space))
                        {
                            jumpStop = StartCoroutine(CancelJump());
                            ChangeState(MovementStates.Falling);
                        }

                        if (rb.velocity.y < 0 && stateDuration > 0.1f) ChangeState(MovementStates.Falling);
                    }

                    );
                }
                break;

            case MovementStates.Falling:
                {
                    RunStates(
                    Enter: null,

                    Stay: () =>
                    {
                        if (Grounded)
                        {
                            ChangeState(MovementStates.Walking);
                        }

                        if (Input.GetKeyDown(KeyCode.Space))
                        {
                            if (previousState == MovementStates.Walking && stateDuration <= coyoteTime)
                            {
                                ChangeState(MovementStates.Jumping);
                            }
                            else jumpBuffer = jumpBufferTime;
                        }
                    }

                    );
                }
                break;
        }
    }
    private void RunStates(System.Action Enter, System.Action Stay)
    {
        if (stateDuration == 0)
        {
            stateDuration += Time.deltaTime;
            Enter?.Invoke();
        }

        stateDuration += Time.deltaTime;
        Stay?.Invoke();
    }

    public void ChangeState(MovementStates state)
    {
        stateDuration = 0;
        previousState = currentState;
        currentState = state;
    }

    #endregion

    private IEnumerator CancelJump()
    {
        while (rb.velocity.y > 0)
        {
            rb.velocity = new Vector3(rb.velocity.x, Mathf.SmoothDamp(rb.velocity.y, 0, ref currentVelocity.y, jumpCancelSpeed), rb.velocity.z);
            yield return new WaitForEndOfFrame();
        }

        jumpStop = null;
    }

    public IEnumerator ScreenShake(float duration, float intensity)
    {
        float time = Time.time;

        while (Time.time <= duration + time)
        {
            viewTilt += Random.insideUnitCircle * intensity;
            yield return new WaitForEndOfFrame();
        }
    }
    public IEnumerator Pulse()
    {
        pulse.color = new Color(1, 0, 0, 0.45f);

        viewTilt.y += Random.Range(-4f, 6f);
        viewTilt.x += Random.Range(-4f, 6f);

        while (pulse.color != new Color(1, 0, 0, 0))
        {
            pulse.color = new Color(1, 0, 0, Mathf.MoveTowards(pulse.color.a, 0, Time.deltaTime * 2));
            yield return new WaitForEndOfFrame();
        }

        pulse.color = new Color(1, 0, 0, 0);
    }

    private IEnumerator Spawn()
    {
        //transform.position = p.spawnPosition.Value;
        forceSpawn = true;

        //p.SetSpawnPositionServerRPC(false, new Vector3(0, 0, 0));

        yield return new WaitForSeconds(1);

        forceSpawn = false;
    }

    private void CameraTilt()
    {
        Vector2 xTiltValues = new(2f, 2f);

        Vector2 tiltStrength = -new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) * xTiltValues;

        if (!Grounded && currentState != MovementStates.Walking) tiltStrength.y -= rb.velocity.y / 2.5f;

        viewTilt = new(
        Mathf.SmoothDamp(viewTilt.x, tiltStrength.x, ref currentTiltSpeed.x, 0.1f),
        Mathf.SmoothDamp(viewTilt.y, tiltStrength.y, ref currentTiltSpeed.y, 0.1f));

        viewRotation = new Vector3(-viewTilt.y, 0, viewTilt.x);
    }
}
