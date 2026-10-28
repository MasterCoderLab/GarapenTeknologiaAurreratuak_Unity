using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror jokoaren egoera: hiru bizitza, hiru etsai eta garaipena/porrota.
/// V14: amaierako pantailak Hasiera menuaren estilo berean erakusten dira.
/// </summary>
public sealed class HorrorGameManager : MonoBehaviour
{
    [SerializeField] private HorrorPlayerMovement movement;
    [SerializeField] private HorrorShotgun shotgun;
    [SerializeField] private HorrorEnemyHealth[] enemies;
    [SerializeField] private int lives = 3;
    private int defeated;
    private float lastHit = -100f;
    private bool ended;
    private bool won;

    // Hasiera V12ko neurriak, koloreak eta tipografia mantendu.
    private Font letra;
    private Texture2D itzala;
    private GUIStyle izenburua, azpititulua, txikia, botoiLetra, oharra;

    public bool Ended => ended;
    public int Lives => lives;
    public int DefeatedCount => defeated;

    public void Configure(HorrorPlayerMovement playerMovement, HorrorShotgun gun,
        HorrorEnemyHealth[] enemyHealth)
    {
        movement = playerMovement;
        shotgun = gun;
        enemies = enemyHealth;
    }

    public void RegisterHit()
    {
        if (ended || Time.time - lastHit < 1.5f) return;
        lastHit = Time.time;
        lives = Mathf.Max(0, lives - 1);
        Debug.Log("HORROR_LIFE: " + lives + "/3");
        if (lives == 0) Finish(false);
    }

    public void EnemyDefeated()
    {
        if (ended) return;
        defeated++;
        Debug.Log("HORROR_KILL: " + defeated + "/3");
        if (defeated >= 3) Finish(true);
    }

    private void Finish(bool victory)
    {
        ended = true;
        won = victory;
        if (movement != null)
        {
            movement.enabled = false;
            Rigidbody rb = movement.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
        if (enemies != null)
        {
            foreach (HorrorEnemyHealth hp in enemies)
            {
                if (hp == null || !hp.gameObject.activeInHierarchy) continue;
                HorrorEnemyAI ai = hp.GetComponent<HorrorEnemyAI>();
                if (ai != null) ai.enabled = false;
                UnityEngine.AI.NavMeshAgent agent = hp.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = true;
            }
        }
        // Saguko botoiek funtziona dezaten, ikuspegiaren blokeoa askatu.
        HorrorMouseLook begirada = FindFirstObjectByType<HorrorMouseLook>();
        if (begirada != null) begirada.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log(victory ? "HORROR_VICTORY: 3/3 enemies defeated" : "HORROR_GAME_OVER: no lives remaining");
    }

    private void EstiloakPrestatu()
    {
        if (izenburua != null) return;
        letra = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        izenburua = SortuEstiloa(76, FontStyle.Bold,
            new Color(.96f, .97f, .97f), TextAnchor.MiddleLeft);
        azpititulua = SortuEstiloa(35, FontStyle.Bold,
            new Color(.81f, .14f, .19f), TextAnchor.MiddleLeft);
        txikia = SortuEstiloa(14, FontStyle.Bold,
            new Color(.67f, .72f, .76f), TextAnchor.MiddleLeft);
        botoiLetra = SortuEstiloa(27, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        oharra = SortuEstiloa(16, FontStyle.Normal,
            new Color(.86f, .88f, .91f), TextAnchor.MiddleLeft);
        itzala = new Texture2D(128, 2, TextureFormat.RGBA32, false);
        itzala.wrapMode = TextureWrapMode.Clamp;
        itzala.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < 128; x++)
        {
            float a = Mathf.Lerp(.97f, .02f, x / 127f);
            for (int y = 0; y < 2; y++)
                itzala.SetPixel(x, y, new Color(.007f, .011f, .02f, a));
        }
        itzala.Apply();
    }

    private GUIStyle SortuEstiloa(int tamaina, FontStyle lodiera,
        Color kolorea, TextAnchor lerrokatzea)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            font = letra,
            fontSize = tamaina,
            fontStyle = lodiera,
            alignment = lerrokatzea,
            clipping = TextClipping.Clip
        };
        s.normal.textColor = kolorea;
        return s;
    }

    private static void Laukia(Rect eremua, Color kolorea)
    {
        Color zaharra = GUI.color;
        GUI.color = kolorea;
        GUI.DrawTexture(eremua, Texture2D.whiteTexture);
        GUI.color = zaharra;
    }

    private bool Botoia(string testua, float y, bool nabarmentzekoa)
    {
        Rect eremua = new Rect(118f, y, 335f, 52f);
        bool gainetik = eremua.Contains(Event.current.mousePosition);
        Laukia(eremua,
            gainetik ? new Color(.38f, .055f, .09f, .98f) :
            nabarmentzekoa ? new Color(.20f, .035f, .06f, .93f) :
            new Color(.05f, .07f, .10f, .88f));
        Laukia(new Rect(eremua.x, y, 4f, 52f),
            gainetik ? new Color(1f, .24f, .28f) : new Color(.67f, .10f, .14f));
        GUI.Label(new Rect(eremua.x + 23f, y + 3f, 300f, 45f), testua, botoiLetra);
        return GUI.Button(eremua, GUIContent.none, GUIStyle.none);
    }

    private static void EszenaKargatu(string izena)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(izena);
    }

    private void OnGUI()
    {
        if (!ended) return;
        GUI.depth = -100;
        EstiloakPrestatu();
        float eskala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        Matrix4x4 aurrekoa = GUI.matrix;
        Color zaharra = GUI.color;
        float marjinaX = (Screen.width - 1280f * eskala) * .5f;
        float marjinaY = (Screen.height - 720f * eskala) * .5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(marjinaX, marjinaY, 0f),
            Quaternion.identity, new Vector3(eskala, eskala, 1f));

        GUI.color = new Color(.008f, .016f, .028f, .77f);
        GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
        GUI.color = zaharra;
        GUI.DrawTexture(new Rect(0, 0, 820, 720), itzala);
        Laukia(new Rect(89f, 111f, 4f, 478f), new Color(.70f, .11f, .17f, .90f));
        GUI.Label(new Rect(117f, 95f, 530f, 35f),
            "GAUEKO BIZIRAUPENA  /  SURVIVAL HORROR", txikia);
        GUI.Label(new Rect(110f, 166f, 700f, 110f),
            won ? "GARAIPENA" : "PORROTA", izenburua);
        GUI.Label(new Rect(118f, 267f, 660f, 65f),
            won ? "BIZIRIK ATERA ZARA." : "ILUNTASUNAK GARAITU ZAITU.", azpititulua);
        Laukia(new Rect(118f, 350f, 335f, 1f), new Color(.78f, .19f, .22f, .52f));

        if (Botoia(won ? "BERRIRO JOLASTU" : "SAIATU BERRIRO", 392f, true))
            EszenaKargatu("Laberintoa_Horror");
        if (Botoia("MENU NAGUSIA", 454f, false))
            EszenaKargatu("Hasiera");

        GUI.Label(new Rect(118f, 539f, 570f, 31f),
            won ? "HIRU ETSAIAK GARAITU DITUZU." : "HIRU BIZITZAK AGORTU DIRA.", oharra);
        GUI.Label(new Rect(118f, 632f, 570f, 28f),
            won ? "GAUA GAINDITU DUZU." : "GAUAK BESTE AUKERA BAT EMANGO DIZU.", oharra);
        GUI.Label(new Rect(118f, 662f, 570f, 24f),
            "GARAPEN TEKNOLOGIA AURRERATUAK  /  2AG3", txikia);

        GUI.color = zaharra;
        GUI.matrix = aurrekoa;
    }

    private void OnDestroy()
    {
        if (itzala != null) Destroy(itzala);
    }
}
