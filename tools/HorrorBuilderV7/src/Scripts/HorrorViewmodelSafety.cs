using UnityEngine;

/// <summary>
/// Lehen pertsonako objektuak hormetara edo zorura hurbiltzean atzeratzen ditu.
/// Jokoaren fisika eta eskopetaren tiro-izpia ez dira aldatzen.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class HorrorViewmodelSafety : MonoBehaviour
{
    [SerializeField] private Transform eskopeta;
    [SerializeField] private Transform linterna;
    [SerializeField] private Transform jokalaria;
    [SerializeField, Range(0.5f, 2f)] private float arriskuDistantzia = 1.30f;
    [SerializeField, Range(1f, 20f)] private float leuntasuna = 9f;

    private Vector3 eskopetaHasiera;
    private Vector3 linternaHasiera;
    private readonly RaycastHit[] talkak = new RaycastHit[48];
    private readonly Collider[] hurbilekoak = new Collider[24];
    private static readonly Vector3[] Norabideak = {
        Vector3.forward,
        new Vector3(.26f, -.13f, 1f).normalized,
        new Vector3(-.27f, -.13f, 1f).normalized
    };

    /// <summary>Eszenaren instalatzaileak ikusgai dauden bi objektuak lotzen ditu.</summary>
    public void Konfiguratu(Transform arma, Transform argia, Transform pertsona)
    {
        eskopeta = arma;
        linterna = argia;
        jokalaria = pertsona;
        eskopetaHasiera = arma.localPosition;
        linternaHasiera = argia.localPosition;
    }

    private void Awake()
    {
        if (eskopeta == null) eskopeta = transform.Find("HB3_EscopetaEnMano");
        if (linterna == null) linterna = transform.Find("HB4_LinternaIkusgai");
        if (jokalaria == null) jokalaria = transform.parent;
        if (eskopeta != null) eskopetaHasiera = eskopeta.localPosition;
        if (linterna != null) linternaHasiera = linterna.localPosition;
    }

    private void Start()
    {
        if (eskopeta != null && linterna != null)
            Debug.Log("HORROR_VIEWMODEL_READY: arma eta linterna lehen planoan; horma hurbilekin atzeratzea aktibo.");
    }

    private void LateUpdate()
    {
        if (eskopeta == null || linterna == null) return;
        float kopurua = 0f;
        // Hormak, sabaia eta zorua aztertu; jokalariaren kapsula baztertu.
        float hurbilen = arriskuDistantzia;
        // Kamera pareta barruan ia badago, SphereCast-ek ez du gainjartzea hautematen.
        int gainez = Physics.OverlapSphereNonAlloc(transform.position, .17f, hurbilekoak,
            ~(1 << 30), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < gainez; i++)
        {
            Collider col = hurbilekoak[i];
            if (col != null && (jokalaria == null || !col.transform.IsChildOf(jokalaria)))
                hurbilen = 0f;
        }
        foreach (Vector3 norabidea in Norabideak)
        {
            Ray izpia = new Ray(transform.position, transform.TransformDirection(norabidea));
            int kop = Physics.SphereCastNonAlloc(izpia, .12f, talkak, arriskuDistantzia,
                ~(1 << 30), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < kop; i++)
            {
                Collider col = talkak[i].collider;
                if (col == null || (jokalaria != null && col.transform.IsChildOf(jokalaria))) continue;
                if (talkak[i].distance < hurbilen) hurbilen = talkak[i].distance;
            }
        }
        kopurua = Mathf.Clamp01((arriskuDistantzia - hurbilen) / (arriskuDistantzia - .17f));

        // Kameraren aurrean geratzen dira baina pareta hurbiltzean behera eta barrurantz mugitzen dira.
        Vector3 armaHelburua = eskopetaHasiera + new Vector3(0f, -.28f, -.48f) * kopurua;
        Vector3 argiHelburua = linternaHasiera + new Vector3(0f, -.12f, -.23f) * kopurua;
        float t = 1f - Mathf.Exp(-leuntasuna * Time.deltaTime);
        eskopeta.localPosition = Vector3.Lerp(eskopeta.localPosition, armaHelburua, t);
        linterna.localPosition = Vector3.Lerp(linterna.localPosition, argiHelburua, t);
    }
}
