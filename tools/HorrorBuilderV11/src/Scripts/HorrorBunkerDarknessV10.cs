using UnityEngine;

/// <summary>
/// V11: benetako linterna-argia errespetatzen du. Ez du pantaila osoa
/// belzten edo argitzen F sakatzean; ertzetako bineta leuna soilik.
/// Iluntasun fisikoa HorrorAtmosphereV9 osagaiak kontrolatzen du.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorBunkerDarknessV10 : MonoBehaviour
{
    // V10eko erreferentziak mantendu, eszena berriro lotu beharrik ez izateko.
    [SerializeField] private HorrorAtmosphereV9 giroa;
    [SerializeField] private Light linterna;
    [SerializeField, Range(0f, .60f)] private float ertzekoOpakutasuna = .36f;
    [SerializeField, Range(.1f, 6f)] private float trantsizioa = 2.7f;
    private Texture2D bineta;
    private float iluntasuna;

    public void Konfiguratu(HorrorAtmosphereV9 atmosfera, Light argia)
    {
        if (atmosfera == null || argia == null)
            throw new System.InvalidOperationException("V11: giroa edo linterna falta da.");
        giroa = atmosfera;
        linterna = argia;
    }

    private void Awake()
    {
        bineta = Resources.Load<Texture2D>("HorrorHUDV10/bunker_vignette");
        if (giroa != null) iluntasuna = giroa.BunkerBlend * ertzekoOpakutasuna;
    }

    private void Update()
    {
        if (giroa == null) return;
        float helburua = giroa.BunkerBlend * ertzekoOpakutasuna;
        iluntasuna = Mathf.MoveTowards(iluntasuna, helburua,
            trantsizioa * Time.unscaledDeltaTime);
    }

    private void OnGUI()
    {
        if (bineta == null || iluntasuna <= .002f) return;
        GUI.depth = 60;
        Color zaharra = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, iluntasuna);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), bineta,
            ScaleMode.StretchToFill, true);
        GUI.color = zaharra;
    }
}
