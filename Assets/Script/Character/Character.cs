using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Camera Reference")]
    public Transform cameraTransform; // assign Main Camera; falls back to Camera.main

    [Header("Movement (WASD)")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.6f;
    public bool useCameraRelative = true; // movement relative to camera yaw

    [Header("Car Steering")]
    public bool carSteeringEnabled = true; // gerak maju + belok seperti mobil
    public float acceleration = 8f;
    public float maxForwardSpeed = 6f;
    public float naturalDeceleration = 4f;
    public float brakeDeceleration = 12f;
    public float turnRate = 90f; // derajat/detik pada steer penuh

    [Header("Physics Fall (J)")]
    public float pushImpulse = 6f;
    public float torqueImpulse = 8f;
    public float fallDrag = 0.2f;
    public float fallAngularDrag = 0.05f;
    public float maxTiltDegrees = 50f; // batas gulir ke depan yang sederhana
    public float maxAngularVelocity = 2f; // batasi kecepatan putar agar tidak flip
    public float tiltMargin = 5f; // trigger jatuh saat mendekati max tilt
    public float returnTorque = 10f; // torsi balik jika belum cukup
    public float uprightSnapDegrees = 2f; // hampir tegak → snap kembali
    public float tiltStepDegrees = 15f; // tiap klik J menambah kemiringan
    public float tiltReturnSpeed = 40f; // kecepatan kembali tegak (deg/sec)
    public float tiltRiseSmoothTime = 0.15f; // durasi easing menuju step target
    public float tiltReturnSmoothTime = 0.4f; // durasi easing saat kembali tegak
    public float tiltHoldInterval = 0.15f; // interval penambahan saat J ditahan (detik)
    public float resetSmoothTime = 0.25f; // durasi easing saat reset ke tegak
    public float resetThresholdDegrees = 0.5f; // ambang selesai reset

    [Header("Distance / Zoom")]
    public float distance = 6f;
    public float minDistance = 2f;
    public float maxDistance = 20f;

    [Header("Rotation")]
    public float rotationSpeed = 180f;
    public float verticalSpeed = 120f;
    public float minPitch = -10f;
    public float maxPitch = 75f;
    public bool invertY = false;
    public int mouseButton = 0; // 0=LMB, 1=RMB

    [Header("Smoothing")]
    public float positionSmooth = 10f;
    public float rotationSmooth = 10f;

    [Header("Collision")]
    public bool collisionAvoidance = true;
    public LayerMask collisionMask = ~0;
    public float collisionPadding = 0.2f;

    private float yaw;
    private float pitch;
    private float targetDistance;
    private Rigidbody rb;
    private bool isFalling;
    private Quaternion startRotation;
    private float currentTilt; // derajat kemiringan saat ini
    private bool lockAtMax;    // jika sudah mencapai max, tidak kembali
    private float targetTilt;  // target derajat kemiringan
    private float tiltVelocity; // kecepatan untuk SmoothDamp
    private float tiltHoldTimer; // akumulator waktu saat J ditahan
    private bool resetInProgress; // sedang reset ke tegak
    private float objectYaw; // yaw untuk arah hadap objek (mobil)
    private float currentSpeed; // kecepatan maju

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Vector3 angles = cameraTransform != null ? cameraTransform.eulerAngles : transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        targetDistance = distance;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;   // aktifkan gravitasi fisika
        rb.isKinematic = false; // biar dipengaruhi fisika
        rb.constraints = RigidbodyConstraints.FreezeRotation; // keep upright until falling
        startRotation = transform.rotation;
        currentTilt = 0f;
        lockAtMax = false;
        targetTilt = 0f;
        tiltVelocity = 0f;
        tiltHoldTimer = 0f;
        resetInProgress = false;
        objectYaw = transform.eulerAngles.y;
        currentSpeed = 0f;
    }

    void Update()
    {
        HandleMovementInput();
        // Tekan J: tambah target kemiringan bertahap
        if (Input.GetKeyDown(KeyCode.J))
        {
            targetTilt = Mathf.Min(targetTilt + tiltStepDegrees, maxTiltDegrees);
            if (targetTilt >= maxTiltDegrees)
            {
                lockAtMax = true;
            }
            // Batalkan reset jika sedang berlangsung
            resetInProgress = false;
        }

        // Saat J ditahan, tambah tilt per interval
        if (Input.GetKey(KeyCode.J) && !lockAtMax)
        {
            tiltHoldTimer += Time.deltaTime;
            if (tiltHoldTimer >= tiltHoldInterval)
            {
                int steps = Mathf.FloorToInt(tiltHoldTimer / tiltHoldInterval);
                tiltHoldTimer -= steps * tiltHoldInterval;
                targetTilt = Mathf.Min(targetTilt + steps * tiltStepDegrees, maxTiltDegrees);
                if (targetTilt >= maxTiltDegrees)
                {
                    lockAtMax = true;
                }
            }
        }
        else
        {
            tiltHoldTimer = 0f;
        }

        // Jika tidak menekan dan belum mencapai max, set target kembali tegak
        if (!lockAtMax && !Input.GetKey(KeyCode.J))
        {
            targetTilt = 0f;
        }

        // Trigger reset manual: R untuk bangun ke posisi awal (smooth)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetUpright();
        }

        // Easing menuju targetTilt menggunakan SmoothDamp
        float smoothTime;
        if (resetInProgress)
        {
            smoothTime = resetSmoothTime;
        }
        else
        {
            smoothTime = (targetTilt > currentTilt) ? tiltRiseSmoothTime : tiltReturnSmoothTime;
        }
        currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, smoothTime);

        // Terapkan rotasi berdasarkan kemiringan + yaw (yaw dulu, lalu tilt agar tilt mengikuti arah maju)
        transform.rotation = startRotation * Quaternion.Euler(0f, objectYaw, 0f) * Quaternion.Euler(currentTilt, 0f, 0f);

        // Selesaikan reset jika hampir tegak
        if (resetInProgress && Mathf.Abs(currentTilt) <= resetThresholdDegrees)
        {
            currentTilt = 0f;
            targetTilt = 0f;
            tiltVelocity = 0f;
            transform.rotation = startRotation;
            resetInProgress = false;
            lockAtMax = false;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        HandleRotationInput();
        HandleZoomInput();

        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        distance = targetDistance;

        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = transform.position - desiredRot * Vector3.forward * distance;

        if (collisionAvoidance)
        {
            Vector3 dir = desiredPos - transform.position;
            float dist = dir.magnitude;
            if (Physics.SphereCast(transform.position, 0.1f, dir.normalized, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(hit.distance - collisionPadding, minDistance);
                desiredPos = transform.position + dir.normalized * safeDist;
            }
        }

        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRot, rotationSmooth * Time.deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPos, positionSmooth * Time.deltaTime);
    }

    // Catatan: mekanik gulir sekarang dijalankan secara manual tanpa impuls fisika.

    // API public untuk reset ke tegak secara smooth (bisa dipanggil dari script lain)
    public void ResetUpright()
    {
        lockAtMax = false;
        targetTilt = 0f;
        resetInProgress = true;
    }

    void HandleMovementInput()
    {
        if (carSteeringEnabled)
        {
            bool holdForward = Input.GetKey(KeyCode.W);
            bool steerLeft = Input.GetKey(KeyCode.A);
            bool steerRight = Input.GetKey(KeyCode.D);
            bool brake = Input.GetKey(KeyCode.S);

            // Kecepatan maju
            if (holdForward)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxForwardSpeed, acceleration * Time.deltaTime);
            }
            else if (brake)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, naturalDeceleration * Time.deltaTime);
            }

            // Steering hanya saat maju
            if (holdForward)
            {
                float steer = 0f;
                if (steerLeft) steer -= 1f;
                if (steerRight) steer += 1f;
                float speedFactor = Mathf.Clamp01(currentSpeed / maxForwardSpeed);
                objectYaw += steer * turnRate * Mathf.Lerp(0.4f, 1f, speedFactor) * Time.deltaTime;
            }

            // Gerak sepanjang forward objek (pakai Rigidbody agar konsisten dengan gravitasi)
            if (currentSpeed > 0f)
            {
                var nextPos = rb.position + transform.forward * currentSpeed * Time.deltaTime;
                rb.MovePosition(nextPos);
            }
        }
        else
        {
            float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
            float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

            Vector3 move = Vector3.zero;

            if (useCameraRelative && cameraTransform != null)
            {
                Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
                Vector3 camRight = cameraTransform.right;   camRight.y = 0f;   camRight.Normalize();
                move = camForward * v + camRight * h;
            }
            else
            {
                move = new Vector3(h, 0f, v);
            }

            if (move.sqrMagnitude > 0f)
            {
                float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
                var nextPos = rb.position + move.normalized * speed * Time.deltaTime;
                rb.MovePosition(nextPos);
            }
        }
    }

    void HandleRotationInput()
    {
        if (Input.GetMouseButton(mouseButton))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            yaw += mx * rotationSpeed * Time.deltaTime;
            pitch += (invertY ? my : -my) * verticalSpeed * Time.deltaTime;
        }

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                yaw += t.deltaPosition.x * 0.2f;
                pitch += (invertY ? t.deltaPosition.y : -t.deltaPosition.y) * 0.2f;
            }
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleZoomInput()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            targetDistance -= scroll;
        }

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;

            float prevMag = (prev0 - prev1).magnitude;
            float currMag = (t0.position - t1.position).magnitude;
            float delta = currMag - prevMag;

            targetDistance -= delta * 0.01f;
        }
    }
}
