using UnityEngine;

/// <summary>
/// Horror eszenarako jokalaria: Rigidbody bidezko mugimendua, hormen talkak errespetatuz.
/// Jatorrizko Personaje.cs eta irakaslearen SampleScene ez dira aldatzen.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class HorrorPlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 3.1f;
    [SerializeField] private float turnSpeed = 115f;
    private Rigidbody body;
    private float forwardInput;
    private float turnInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        // Proiektuak Active Input Handling = Both dauka; mugimendu klasikoa mantentzen dugu.
        forwardInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");
    }

    private void FixedUpdate()
    {
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f));
        Vector3 desired = transform.forward * (forwardInput * speed);
        Vector3 current = body.linearVelocity;
        body.linearVelocity = new Vector3(desired.x, current.y, desired.z);
    }

    // Gaur egun jatorrizko jokabidea gordetzen dugu; bizitzen sistema hurrengo fasean.
    public void PerderVida()
    {
        Debug.Log("HORROR_PLAYER_HIT: bizitzen sistema oraindik egiteke");
    }
}
