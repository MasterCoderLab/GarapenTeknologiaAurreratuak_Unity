using UnityEngine;

/// <summary>
/// Horror jokorako HUD konpaktua: jokalariaren bizitza, kartutxoak eta etsaiak.
/// Eskuineko eta ezkerreko behealdean agertzen da, ikuspegia estali gabe.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorHUDV10 : MonoBehaviour
{
    [SerializeField] private HorrorGameManager kudeatzailea;
    [SerializeField] private HorrorShotgun eskopeta;
    private Texture2D kartutxoa;
    private Texture2D kartutxoHutsa;
    private GUIStyle kopurua;
    private GUIStyle txikia;
    private GUIStyle titulua;

    public void Konfiguratu(HorrorGameManager game, HorrorShotgun gun)
    {
        if (game == null || gun == null)
            throw new System.InvalidOperationException("HBB10: HUD erreferentziak falta dira.");
        kudeatzailea = game;
        eskopeta = gun;
    }

    private void Awake()
    {
        kartutxoa = Resources.Load<Texture2D>("HorrorHUDV10/shell_ready");
        kartutxoHutsa = Resources.Load<Texture2D>("HorrorHUDV10/shell_empty");
        if (kartutxoa == null || kartutxoHutsa == null)
            Debug.LogError("HBB10_HUD_ERROR: kartutxoen irudiak falta dira.");
    }

    private void PrestatuEstiloak()
    {
        if (kopurua != null) return;
        kopurua = new GUIStyle(GUI.skin.label) {
            fontSize = 30, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        kopurua.normal.textColor = Color.white;
        txikia = new GUIStyle(GUI.skin.label) {
            fontSize = 13, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        txikia.normal.textColor = new Color(.80f, .82f, .85f, 1f);
        titulua = new GUIStyle(GUI.skin.label) {
            fontSize = 17, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titulua.normal.textColor = new Color(.97f, .97f, .97f, 1f);
    }

    private static void Laukia(Rect rect, Color kolorea)
    {
        Color old = GUI.color;
        GUI.color = kolorea;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void OnGUI()
    {
        if (kudeatzailea == null || eskopeta == null || kudeatzailea.Ended) return;
        GUI.depth = -40;
        PrestatuEstiloak();
        float s = Mathf.Clamp(Screen.height / 720f, .75f, 1.45f);
        Matrix4x4 original = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float w = Screen.width / s;
        float h = Screen.height / s;

        // BIZITZA: behe-ezkerrean hiru atal gorri.
        float left = 18f, top = h - 111f;
        Laukia(new Rect(left, top, 234f, 92f), new Color(.015f, .018f, .025f, .78f));
        Laukia(new Rect(left, top, 4f, 92f), new Color(.85f, .10f, .15f, 1f));
        GUI.Label(new Rect(left + 17f, top + 7f, 180f, 25f), "BIZITZA", titulua);
        GUI.Label(new Rect(left + 174f, top + 7f, 55f, 25f),
            kudeatzailea.Lives + "/3", txikia);
        for (int i = 0; i < 3; i++)
        {
            Color segment = i < kudeatzailea.Lives
                ? new Color(.90f, .10f, .15f, 1f)
                : new Color(.21f, .23f, .26f, 1f);
            Laukia(new Rect(left + 17f + i * 67f, top + 44f, 58f, 22f), segment);
            Laukia(new Rect(left + 17f + i * 67f, top + 44f, 58f, 3f),
                i < kudeatzailea.Lives ? new Color(1f, .38f, .43f) : new Color(.35f, .35f, .35f));
        }

        // HELBURUA: txikia eta goiko eskuinean, korridorea libre uzteko.
        Laukia(new Rect(w - 176f, 18f, 158f, 42f), new Color(.015f, .018f, .025f, .78f));
        GUI.Label(new Rect(w - 162f, 24f, 144f, 30f),
            "ETSAIAK  " + kudeatzailea.DefeatedCount + "/3", titulua);

        // ESKOPETA: behe-eskuinean kartutxoaren ikonoa eta kopurua.
        float right = w - 269f;
        Laukia(new Rect(right, h - 111f, 251f, 92f), new Color(.015f, .018f, .025f, .78f));
        if (!eskopeta.Collected)
        {
            GUI.Label(new Rect(right + 12f, h - 98f, 230f, 24f), "ESKOPETA", titulua);
            GUI.Label(new Rect(right + 12f, h - 67f, 232f, 24f), "TXABOLAN BILATU", txikia);
        }
        else
        {
            GUI.Label(new Rect(right + 12f, h - 105f, 225f, 22f),
                eskopeta.IsReloading ? "BIRKARGATZEN..." : "ESKOPETA", txikia);
            if (kartutxoa != null)
                GUI.DrawTexture(new Rect(right + 12f, h - 78f, 25f, 42f), kartutxoa, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(right + 42f, h - 79f, 77f, 47f),
                "x" + eskopeta.Loaded, kopurua);
            GUI.Label(new Rect(right + 155f, h - 75f, 90f, 24f),
                "ERRESERBA", txikia);
            GUI.Label(new Rect(right + 168f, h - 53f, 70f, 24f),
                eskopeta.Reserve.ToString(), titulua);
            for (int i = 0; i < 6; i++)
            {
                Texture2D shell = i < eskopeta.Loaded ? kartutxoa : kartutxoHutsa;
                if (shell != null)
                    GUI.DrawTexture(new Rect(right + 10f + i * 17.5f, h - 28f, 13f, 19f), shell,
                        ScaleMode.ScaleToFit, true);
            }
        }
        GUI.matrix = original;
    }
}
