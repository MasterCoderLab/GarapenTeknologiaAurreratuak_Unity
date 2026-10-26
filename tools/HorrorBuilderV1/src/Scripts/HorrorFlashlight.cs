using UnityEngine;

/// <summary>
/// F tekla: linterna piztu/itzali. Kamera barruko Spot Light kontrolatzen du.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorFlashlight : MonoBehaviour
{
    [SerializeField] private Light flashlight;
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    private void Awake()
    {
        if (flashlight == null)
            flashlight = transform.Find("HB_Linterna")?.GetComponent<Light>();
    }

    private void Update()
    {
        if (flashlight != null && Input.GetKeyDown(toggleKey))
            flashlight.enabled = !flashlight.enabled;
    }
}
