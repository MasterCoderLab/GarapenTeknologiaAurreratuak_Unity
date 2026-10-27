using System;
using UnityEngine;

/// <summary>
/// Etsaien osasuna: gorputzeko hiru kolpe edo buruan tiro bakar bat.
/// Bere barra pantailan erakusten da, oztopoen atzean agertu gabe.
/// </summary>
public sealed class HorrorEnemyHealth : MonoBehaviour
{
    [SerializeField] private HorrorGameManager manager;
    [SerializeField, Min(1)] private int hitPoints = 3;
    [SerializeField, Range(.68f, .92f)] private float buruarenAltuera = .80f;
    [SerializeField, Range(.08f, .25f)] private float buruarenErradioa = .16f;
    private bool dead;
    private Camera kamera;
    private GUIStyle estiloa;
    private Renderer[] ereduak;

    public void Configure(HorrorGameManager gameManager) { manager = gameManager; }

    private void Awake() { ereduak = GetComponentsInChildren<Renderer>(true); }

    private bool LortuMugak(out Bounds bounds)
    {
        bounds = default(Bounds);
        if (ereduak == null || ereduak.Length == 0)
            ereduak = GetComponentsInChildren<Renderer>(true);
        bool found = false;
        foreach (Renderer renderer in ereduak)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found && bounds.size.y > .05f;
    }

    /// <summary>
    /// Raycast-ak lehen oztopo erreala jo duenean deitzen da. Altueraren eta
    /// tiroaren norabidearen arabera buruko inpaktua identifikatu.
    /// </summary>
    public void TakeShot(Vector3 origin, Vector3 direction, Vector3 impact)
    {
        if (dead || manager == null || manager.Ended) return;
        bool buruan = false;
        Bounds bounds;
        if (LortuMugak(out bounds))
        {
            Vector3 buruZentroa = new Vector3(bounds.center.x,
                bounds.min.y + bounds.size.y * .87f, bounds.center.z);
            float radius = Mathf.Clamp(bounds.size.y * buruarenErradioa, .18f, .66f);
            Vector3 dir = direction.normalized;
            float distantzia = Mathf.Max(0f, Vector3.Dot(buruZentroa - origin, dir));
            Vector3 hurbilena = origin + dir * distantzia;
            bool bururaBegira = (hurbilena - buruZentroa).sqrMagnitude <= radius * radius;
            bool goikoAldean = impact.y >= bounds.min.y + bounds.size.y * buruarenAltuera;
            buruan = bururaBegira && goikoAldean;
        }
        if (buruan)
            hitPoints = 0;
        else
            hitPoints = Mathf.Max(0, hitPoints - 1);

        Debug.Log("HORROR_ENEMY_HIT: " + name + " HP=" + hitPoints +
            (buruan ? " HEADSHOT" : " BODY"));
        if (hitPoints > 0) return;
        dead = true;
        Debug.Log("HORROR_ENEMY_DEAD: " + name);
        manager.EnemyDefeated();
        gameObject.SetActive(false);
    }

    // Zaharreko TakeShot() deia babesten dugu, beste deiak egotekotan.
    public void TakeShot()
    {
        if (dead || manager == null || manager.Ended) return;
        hitPoints = Mathf.Max(0, hitPoints - 1);
        if (hitPoints > 0) return;
        dead = true;
        manager.EnemyDefeated();
        gameObject.SetActive(false);
    }

    private void OnGUI()
    {
        if (dead || manager == null || manager.Ended) return;
        if (kamera == null) kamera = Camera.main;
        if (kamera == null) return;
        Bounds b;
        if (!LortuMugak(out b)) return;
        Vector3 helmuga = new Vector3(b.center.x, b.max.y + .35f, b.center.z);
        float distantzia = Vector3.Distance(kamera.transform.position, helmuga);
        if (distantzia > 19f) return;
        Vector3 pantaila = kamera.WorldToScreenPoint(helmuga);
        if (pantaila.z <= 0 || pantaila.x < 70 || pantaila.x > Screen.width - 70 ||
            pantaila.y < 15 || pantaila.y > Screen.height - 20) return;

        // Etsaia hormaren atzean badago, ez marraztu bere osasun-barra.
        Vector3 dir = helmuga - kamera.transform.position;
        RaycastHit[] hits = Physics.RaycastAll(kamera.transform.position,
            dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b2) => a.distance.CompareTo(b2.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(kamera.transform.root)) continue;
            if (!hit.transform.IsChildOf(transform)) return;
            break;
        }

        GUI.depth = -30;
        float x = pantaila.x - 56f;
        float y = Screen.height - pantaila.y - 26f;
        Color old = GUI.color;
        GUI.color = new Color(.03f, .04f, .06f, .86f);
        GUI.DrawTexture(new Rect(x - 5f, y - 5f, 122f, 29f), Texture2D.whiteTexture);
        GUI.color = new Color(.24f, .25f, .27f, 1f);
        GUI.DrawTexture(new Rect(x, y + 10f, 112f, 9f), Texture2D.whiteTexture);
        GUI.color = new Color(.94f, .14f, .18f, 1f);
        GUI.DrawTexture(new Rect(x, y + 10f, 112f * Mathf.Clamp01(hitPoints / 3f), 9f),
            Texture2D.whiteTexture);
        GUI.color = old;
        if (estiloa == null)
        {
            estiloa = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter };
            estiloa.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(x, y - 6f, 112f, 16f), "ETSAIA  " + hitPoints + "/3", estiloa);
    }
}
