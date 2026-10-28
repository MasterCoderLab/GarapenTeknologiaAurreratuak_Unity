using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hasiera eszenako menu nagusia. Jokoaren mekanikak ez ditu aldatzen.
/// Bolumena gorde egiten du hurrengo saioetarako.
/// </summary>
public sealed class HorrorMainMenuV12 : MonoBehaviour
{
    private const string BolumenGakoa = "LaberintoaHorror_Bolumena";
    private bool aukerak;
    private float bolumena;
    private Font letra;
    private Texture2D itzala;
    private GUIStyle izenburua, azpititulua, txikia, botoiLetra, oharra;
    private Vector3 hasierakoKameraPosizioa;
    private Camera kamera;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        bolumena = Mathf.Clamp01(PlayerPrefs.GetFloat(BolumenGakoa, .78f));
        AudioListener.volume = bolumena;
        letra = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        kamera = Camera.main;
        if (kamera != null) hasierakoKameraPosizioa = kamera.transform.position;
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

    private void Update()
    {
        if (kamera != null)
        {
            float sway = Mathf.Sin(Time.time * .26f) * .055f;
            kamera.transform.position = hasierakoKameraPosizioa + new Vector3(sway, 0f, 0f);
        }
        if (aukerakItxi()) aukerak = false;
    }

    // Beharrezkoa denean aukera-paneletik itzuli.
    private bool aukerakItxi() => aukerak && Input.GetKeyDown(KeyCode.Escape);

    private void EstiloakPrestatu()
    {
        if (izenburua != null) return;
        izenburua = SortuEstiloa(65, FontStyle.Bold, new Color(.96f, .97f, .97f), TextAnchor.MiddleLeft);
        azpititulua = SortuEstiloa(92, FontStyle.Bold, new Color(.79f, .12f, .17f), TextAnchor.MiddleLeft);
        txikia = SortuEstiloa(14, FontStyle.Bold, new Color(.67f, .72f, .76f), TextAnchor.MiddleLeft);
        botoiLetra = SortuEstiloa(27, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        oharra = SortuEstiloa(16, FontStyle.Normal, new Color(.86f, .88f, .91f), TextAnchor.MiddleLeft);
    }

    private GUIStyle SortuEstiloa(int tamaina, FontStyle lodiera, Color kolorea, TextAnchor lerrokatzea)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            font = letra,
            fontSize = tamaina,
            fontStyle = lodiera,
            alignment = lerrokatzea,
            clipping = TextClipping.Clip,
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
            nabarmentzekoa ? new Color(.20f, .035f, .06f, .93f) : new Color(.05f, .07f, .10f, .88f));
        Laukia(new Rect(eremua.x, y, 4f, 52f), gainetik ? new Color(1f, .24f, .28f) : new Color(.67f, .10f, .14f));
        GUI.Label(new Rect(eremua.x + 23f, y + 3f, 300f, 45f), testua, botoiLetra);
        return GUI.Button(eremua, GUIContent.none, GUIStyle.none);
    }

    private void OnGUI()
    {
        EstiloakPrestatu();
        float eskala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        Matrix4x4 aurrekoa = GUI.matrix;
        float marjinaX = (Screen.width - 1280f * eskala) * .5f;
        float marjinaY = (Screen.height - 720f * eskala) * .5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(marjinaX, marjinaY, 0f),
            Quaternion.identity, new Vector3(eskala, eskala, 1f));
        Color zaharra = GUI.color;
        GUI.color = new Color(.008f, .016f, .028f, .43f);
        GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
        GUI.color = zaharra;
        // Eszenatokia agerian utzi: ezkerretik eskuinera gardenago.
        GUI.DrawTexture(new Rect(0, 0, 820, 720), itzala);
        Laukia(new Rect(89f, 111f, 4f, 478f), new Color(.70f, .11f, .17f, .90f));
        GUI.Label(new Rect(117f, 95f, 420f, 35f), "GAUEKO BIZIRAUPENA  /  SURVIVAL HORROR", txikia);
        GUI.Label(new Rect(110f, 168f, 600f, 86f), "LABERINTOA", izenburua);
        GUI.Label(new Rect(108f, 234f, 580f, 110f), "HORROR", azpititulua);
        Laukia(new Rect(118f, 350f, 335f, 1f), new Color(.78f, .19f, .22f, .52f));
        if (!aukerak)
        {
            if (Botoia("JOLASTU", 392f, true)) SceneManager.LoadScene("Laberintoa_Horror");
            if (Botoia("AUKERAK", 454f, false)) aukerak = true;
            if (Botoia("IRTEN", 516f, false)) Irten();
        }
        else
        {
            GUI.Label(new Rect(118f, 382f, 300f, 32f), "SOINUAREN BOLUMENA", oharra);
            float berria = GUI.HorizontalSlider(new Rect(118f, 430f, 335f, 24f), bolumena, 0f, 1f);
            if (!Mathf.Approximately(berria, bolumena))
            {
                bolumena = berria;
                AudioListener.volume = berria;
                PlayerPrefs.SetFloat(BolumenGakoa, berria);
                PlayerPrefs.Save();
            }
            GUI.Label(new Rect(118f, 458f, 300f, 29f), Mathf.RoundToInt(bolumena * 100f) + "%", txikia);
            if (Botoia("ATZERA", 518f, false)) aukerak = false;
        }
        GUI.Label(new Rect(118f, 632f, 570f, 28f), "ILUNPETAN, ZURE ARGI BAKARRA LINTERNA DA.", oharra);
        GUI.Label(new Rect(118f, 662f, 520f, 24f), "GARAPEN TEKNOLOGIA AURRERATUAK  /  2AG3", txikia);
        GUI.matrix = aurrekoa;
    }

    private void Irten()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (itzala != null) Destroy(itzala);
    }
}
