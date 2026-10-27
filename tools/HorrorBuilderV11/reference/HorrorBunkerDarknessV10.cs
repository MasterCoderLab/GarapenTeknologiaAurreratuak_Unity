using UnityEngine;

/// <summary>
/// Bunkerraren beldur-giroa: linterna itzalita, irudiaren esposizioa
/// eta ertzetako ikusgarritasuna gutxitu. Linterna piztean argia berreskuratu.
/// Ez du eszenaren munduko argirik edo NavMeshik aldatzen.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorBunkerDarknessV10 : MonoBehaviour
{
    [SerializeField] private HorrorAtmosphereV9 giroa;
    [SerializeField] private Light linterna;
    [SerializeField, Range(.35f, .85f)] private float itzalitaOpakutasuna = .69f;
    [SerializeField, Range(.1f, 6f)] private float trantsizioa = 2.7f;
    private Texture2D bineta;
    private float iluntasuna;

    public void Konfiguratu(HorrorAtmosphereV9 atmosfera, Light argia)
    {
        if (atmosfera == null || argia == null)
            throw new System.InvalidOperationException("HBB10: iluntasunaren erreferentziak falta dira.");
        giroa = atmosfera;
        linterna = argia;
    }

    private void Awake()
    {
        bineta = Resources.Load<Texture2D>("HorrorHUDV10/bunker_vignette");
        if (giroa != null && linterna != null)
            iluntasuna = giroa.BunkerBlend * (linterna.enabled ? .025f : itzalitaOpakutasuna);
    }

    private void Update()
    {
        if (giroa == null || linterna == null) return;
        float helburua = giroa.BunkerBlend * (linterna.enabled ? .025f : itzalitaOpakutasuna);
        iluntasuna = Mathf.MoveTowards(iluntasuna, helburua,
            trantsizioa * Time.unscaledDeltaTime);
    }

    private void OnGUI()
    {
        if (iluntasuna <= .002f) return;
        // HUD eta garaipen-panelak gainetik marrazteko sakonera altua.
        GUI.depth = 60;
        Color zaharra = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, iluntasuna);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        if (bineta != null)
        {
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(iluntasuna * .70f));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), bineta,
                ScaleMode.StretchToFill, true);
        }
        GUI.color = zaharra;
    }
}
