using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Bunkerrean ikuspen laburragoa eta kanpoan linterna urrunagoa.
/// Play moduan bakarrik aldatzen ditu giro-argia, lainoa, ilargia eta linternaren argia.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorAtmosphereV9 : MonoBehaviour
{
    [SerializeField] private Transform jokalaria;
    [SerializeField] private Light linterna;
    [SerializeField] private Light ilargia;
    [SerializeField] private BoxCollider[] bunkerrekoTeilatuak;

    [SerializeField, Range(.04f, .23f)] private float bunkerLainoDentsitatea = .15f;
    [SerializeField, Range(.01f, .2f)] private float bunkerIlargiIntentsitatea = .045f;
    [SerializeField, Range(20f, 38f)] private float basokoLinternaIrismena = 28f;
    [SerializeField, Range(10f, 19f)] private float basokoLinternaIntentsitatea = 14f;
    [SerializeField, Range(.2f, 3f)] private float trantsizioAbiadura = .85f;

    private float jatorrizkoLainoa;
    private Color jatorrizkoLainoKolorea;
    private Color jatorrizkoGiroArgia;
    private AmbientMode jatorrizkoGiroMota;
    private float jatorrizkoIlargia;
    private float jatorrizkoLinternaIrismena;
    private float jatorrizkoLinternaIntentsitatea;
    private float jatorrizkoLinternaBarneAngelua;
    private bool jatorrizkoLainoAktibatua;
    private FogMode jatorrizkoLainoMota;
    private float bunkerNahasketa;
    private bool hasieratuta;
    [SerializeField, Range(.04f, .22f)] private float v11BunkerLainoDentsitatea = .135f;
    [SerializeField, Range(0f, .035f)] private float v11BunkerIlargia = .009f;

    public float BunkerBlend => bunkerNahasketa;

    /// <summary>Editor Builderrak lau bunker-teilatuak eta argiak lotzen ditu.</summary>
    public void Konfiguratu(Transform pertsona, Light eskuArgia, Light kanpokoIlargia,
                           BoxCollider[] teilatuak)
    {
        if (pertsona == null || eskuArgia == null || kanpokoIlargia == null ||
            teilatuak == null || teilatuak.Length != 4)
            throw new System.InvalidOperationException("V9: bunkerreko erreferentzia bat falta da.");
        foreach (BoxCollider teilatua in teilatuak)
            if (teilatua == null)
                throw new System.InvalidOperationException("V9: bunkerreko teilatu bat falta da.");

        jokalaria = pertsona;
        linterna = eskuArgia;
        ilargia = kanpokoIlargia;
        bunkerrekoTeilatuak = teilatuak;
    }

    private void Start()
    {
        if (jokalaria == null || linterna == null || ilargia == null ||
            bunkerrekoTeilatuak == null || bunkerrekoTeilatuak.Length != 4)
        {
            Debug.LogError("HORROR_V9_ERROR: argiak edo bunkerreko teilatuak falta dira.");
            enabled = false;
            return;
        }

        jatorrizkoLainoa = RenderSettings.fogDensity;
        jatorrizkoLainoKolorea = RenderSettings.fogColor;
        jatorrizkoGiroArgia = RenderSettings.ambientLight;
        jatorrizkoGiroMota = RenderSettings.ambientMode;
        jatorrizkoLainoAktibatua = RenderSettings.fog;
        jatorrizkoLainoMota = RenderSettings.fogMode;
        jatorrizkoIlargia = ilargia.intensity;
        jatorrizkoLinternaIrismena = linterna.range;
        jatorrizkoLinternaIntentsitatea = linterna.intensity;
        jatorrizkoLinternaBarneAngelua = linterna.innerSpotAngle;
        bunkerNahasketa = BunkerreanDago() ? 1f : 0f;
        hasieratuta = true;
        EguneratuArgiztapena();
        Debug.Log("HORROR_V9_READY: A/D mugimendu laterala; bunkerraren iluntasuna eta basoko linterna aktibo.");
    }

    private void LateUpdate()
    {
        if (!hasieratuta) return;
        float helburua = BunkerreanDago() ? 1f : 0f;
        bunkerNahasketa = Mathf.MoveTowards(bunkerNahasketa, helburua,
            trantsizioAbiadura * Time.deltaTime);
        EguneratuArgiztapena();
    }

    private bool BunkerreanDago()
    {
        Vector3 posizioa = jokalaria.position;
        foreach (BoxCollider teilatua in bunkerrekoTeilatuak)
        {
            // Teilatuen X/Z aztarna erabiltzen dugu: jokalaria azpian dagoenean.
            Bounds eremua = teilatua.bounds;
            if (posizioa.x >= eremua.min.x && posizioa.x <= eremua.max.x &&
                posizioa.z >= eremua.min.z && posizioa.z <= eremua.max.z &&
                posizioa.y < eremua.min.y)
                return true;
        }
        return false;
    }

    private void EguneratuArgiztapena()
    {
        // Kanpoan V8ko egoera berreskuratzen dugu, egun-giroa sortu gabe.
        float barruan = bunkerNahasketa;
        // URPk distantziako lainoa erabil dezan, bunkerrean benetan aktibatu.
        // Kanpoan eszenaren jatorrizko egoera mantendu.
        RenderSettings.fog = barruan > .01f || jatorrizkoLainoAktibatua;
        RenderSettings.fogMode = barruan > .01f ? FogMode.ExponentialSquared : jatorrizkoLainoMota;
        RenderSettings.fogDensity = Mathf.Lerp(jatorrizkoLainoa,
            v11BunkerLainoDentsitatea, barruan);
        // Flat mode: ambientLight benetan aplika dadin, skybox-aren argia gabe.
        RenderSettings.ambientMode = barruan > .01f ? AmbientMode.Flat : jatorrizkoGiroMota;
        RenderSettings.fogColor = Color.Lerp(jatorrizkoLainoKolorea,
            new Color(.003f, .004f, .006f), barruan);
        RenderSettings.ambientLight = Color.Lerp(jatorrizkoGiroArgia,
            new Color(.005f, .006f, .009f), barruan);
        ilargia.intensity = Mathf.Lerp(jatorrizkoIlargia, v11BunkerIlargia, barruan);
        linterna.range = Mathf.Lerp(basokoLinternaIrismena,
            jatorrizkoLinternaIrismena, barruan);
        linterna.intensity = Mathf.Lerp(basokoLinternaIntentsitatea,
            jatorrizkoLinternaIntentsitatea, barruan);
        // Barne-konoa txikitu, argiaren ertzean trantsizio leunagoa izateko.
        // Kanpoan jatorrizko linterna mantendu.
        float barneHelburua = Mathf.Min(jatorrizkoLinternaBarneAngelua, linterna.spotAngle * .54f);
        linterna.innerSpotAngle = Mathf.Lerp(jatorrizkoLinternaBarneAngelua, barneHelburua, barruan);
    }

    private void OnDisable()
    {
        if (!hasieratuta) return;
        RenderSettings.fog = jatorrizkoLainoAktibatua;
        RenderSettings.fogMode = jatorrizkoLainoMota;
        RenderSettings.fogDensity = jatorrizkoLainoa;
        RenderSettings.fogColor = jatorrizkoLainoKolorea;
        RenderSettings.ambientLight = jatorrizkoGiroArgia;
        RenderSettings.ambientMode = jatorrizkoGiroMota;
        if (ilargia != null) ilargia.intensity = jatorrizkoIlargia;
        if (linterna != null)
        {
            linterna.range = jatorrizkoLinternaIrismena;
            linterna.intensity = jatorrizkoLinternaIntentsitatea;
            linterna.innerSpotAngle = jatorrizkoLinternaBarneAngelua;
        }
        hasieratuta = false;
    }
}
