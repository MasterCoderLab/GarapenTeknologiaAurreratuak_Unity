using UnityEngine;

/// <summary>
/// Saguarekin ikuspegia kontrolatu: X ardatza jokalariaren biraketa,
/// Y ardatza kameraren inklinazioa. Eskopeta eta linterna kamerarekin doaz.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorMouseLook : MonoBehaviour
{
    [SerializeField] private HorrorPlayerMovement mugimendua;
    [SerializeField] private float horizontalSentikortasuna = 2.1f;
    [SerializeField] private float bertikalSentikortasuna = 1.8f;
    [SerializeField] private float goikoMuga = 72f;
    [SerializeField] private float behekoMuga = 72f;

    private float inklinazioa;

    private void Awake()
    {
        if (mugimendua == null)
            mugimendua = GetComponentInParent<HorrorPlayerMovement>();
        inklinazioa = Mathf.DeltaAngle(0f, transform.localEulerAngles.x);
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // ESC: sagua askatu. Jokoaren leihoan klik: berriro hartu.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        if (mugimendua == null || !mugimendua.enabled) return;

        // Mouse X/Y Unity-ren sarrera klasikoarekin (Active Input Handling: Both).
        // Ez biderkatu Time.deltaTime-rekin: Mouse X/Y dagoeneko fotogramako deltak dira.
        float dx = Input.GetAxisRaw("Mouse X");
        float dy = Input.GetAxisRaw("Mouse Y");
        mugimendua.GehituSaguBiraketa(dx * horizontalSentikortasuna);
        inklinazioa = Mathf.Clamp(inklinazioa - dy * bertikalSentikortasuna,
            -goikoMuga, behekoMuga);
        transform.localRotation = Quaternion.Euler(inklinazioa, 0f, 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
