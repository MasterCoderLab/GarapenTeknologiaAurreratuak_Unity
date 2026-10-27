using System;
using UnityEngine;

/// <summary>
/// E: txabolako eskopeta hartu. Sagua ezker: tiro egin. R: birkargatu.
/// Karga 6 + erreserba 12; etsai bakoitzak bi tiro behar ditu.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorShotgun : MonoBehaviour
{
    [SerializeField] private HorrorGameManager manager;
    [SerializeField] private Transform player;
    [SerializeField] private Transform pickup;
    [SerializeField] private GameObject[] tableVisuals;
    [SerializeField] private GameObject viewModel;
    [SerializeField] private int loaded = 6;
    [SerializeField] private int reserve = 12;
    [SerializeField] private float range = 27f;
    private Camera cam;
    private bool collected;
    private float nextShot;
    private float reloadingUntil;
    private bool reloading;
    private float flashUntil;

    public bool Collected => collected;
    public bool IsReloading => reloading;
    public int Loaded => loaded;
    public int Reserve => reserve;

    public string StatusText
    {
        get
        {
            if (!collected) return "ESKOPETA: txabolan bilatu";
            if (reloading) return "BIRKARGATZEN...";
            return "ESKOPETA: " + loaded + "/6   ERRESERBA: " + reserve + "   R: birkargatu";
        }
    }

    public void Configure(HorrorGameManager gameManager, Transform playerRoot,
        Transform pickupTransform, GameObject[] visuals, GameObject firstPersonModel)
    {
        manager = gameManager;
        player = playerRoot;
        pickup = pickupTransform;
        tableVisuals = visuals;
        viewModel = firstPersonModel;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (viewModel != null) viewModel.SetActive(false);
    }

    private void Update()
    {
        if (manager == null || manager.Ended) return;
        if (reloading && Time.time >= reloadingUntil)
        {
            int n = Mathf.Min(6 - loaded, reserve);
            loaded += n;
            reserve -= n;
            reloading = false;
            Debug.Log("HORROR_RELOAD: " + loaded + "/6, reserve=" + reserve);
        }
        if (!collected)
        {
            if (pickup != null && player != null &&
                Vector3.Distance(player.position, pickup.position) <= 2.7f && Input.GetKeyDown(KeyCode.E))
            {
                collected = true;
                if (tableVisuals != null)
                    foreach (GameObject visual in tableVisuals)
                        if (visual != null) visual.SetActive(false);
                if (viewModel != null) viewModel.SetActive(true);
                Debug.Log("HORROR_PICKUP: shotgun acquired");
            }
            return;
        }
        if (Input.GetKeyDown(KeyCode.R) && !reloading && loaded < 6 && reserve > 0)
        {
            reloading = true;
            reloadingUntil = Time.time + 1.4f;
            Debug.Log("HORROR_RELOAD_START");
        }
        if (Input.GetMouseButtonDown(0) && !reloading && loaded > 0 && Time.time >= nextShot)
            Fire();
    }

    private void Fire()
    {
        loaded--;
        nextShot = Time.time + .8f;
        flashUntil = Time.time + .09f;
        RaycastHit[] hits = Physics.RaycastAll(new Ray(cam.transform.position, cam.transform.forward),
            range, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (player != null && hit.transform.IsChildOf(player)) continue;
            HorrorEnemyHealth enemy = hit.collider.GetComponentInParent<HorrorEnemyHealth>();
            if (enemy != null) enemy.TakeShot(cam.transform.position, cam.transform.forward, hit.point);
            break; // El primer objeto real bloquea los disparos (paredes incluidas).
        }
        Debug.Log("HORROR_SHOT: shells=" + loaded + ", reserve=" + reserve);
    }

    private void OnGUI()
    {
        if (manager == null || manager.Ended) return;
        if (!collected && player != null && pickup != null &&
            Vector3.Distance(player.position, pickup.position) <= 2.7f)
            GUI.Box(new Rect(Screen.width / 2f - 165, Screen.height - 115, 330, 36),
                "Sakatu E eskopeta hartzeko");
        if (collected)
        {
            GUI.Label(new Rect(Screen.width / 2f - 6, Screen.height / 2f - 10, 24, 24), "+");
            if (Time.time < flashUntil)
                GUI.Box(new Rect(Screen.width / 2f - 30, Screen.height / 2f - 30, 60, 60), "DANBA!");
        }
    }
}
