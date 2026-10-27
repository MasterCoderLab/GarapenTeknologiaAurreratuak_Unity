using UnityEngine;

/// <summary>
/// Zuriunearekin jauzi egin, lurrean gaudenean bakarrik.
/// Aurreko mugimendua eta Rigidbody osagaia mantentzen ditu.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(HorrorPlayerMovement))]
public sealed class HorrorPlayerJump : MonoBehaviour
{
    [SerializeField] private float jumpSpeed = 3.4f;
    [SerializeField] private float groundRayLength = 1.31f;

    private Rigidbody body;
    private HorrorPlayerMovement movement;
    private bool jumpRequested;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        movement = GetComponent<HorrorPlayerMovement>();
    }

    private void Update()
    {
        if (movement == null || !movement.enabled)
        {
            jumpRequested = false;
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space)) jumpRequested = true;
    }

    private void FixedUpdate()
    {
        if (!jumpRequested) return;
        jumpRequested = false;
        if (movement == null || !movement.enabled || body.linearVelocity.y > 0.15f) return;
        if (!LurreanDago()) return;
        body.AddForce(Vector3.up * jumpSpeed, ForceMode.VelocityChange);
        Debug.Log("HORROR_JUMP_OK");
    }

    private bool LurreanDago()
    {
        Vector3 start = transform.position + Vector3.up * 0.15f;
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down,
            groundRayLength, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.normal.y > 0.65f) return true;
        }
        return false;
    }
}
