
using UnityEngine;

/// <summary>
/// Jokalariaren mugimendu horizontala, jauziak eta
/// hasierako posiziora itzultzeko prozesua kudeatzen ditu.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Player : MonoBehaviour
{
    // Mugimenduaren eta jauziaren balio finkoak.
    // Balio hauek kodean definitzen dira, Inspector-eko
    // aurretik gordetako balioek ez ditzaten ordezkatu.
    private const float VelocidadHorizontal = 2.9f;
    private const float ImpulsoSalto = 4.3f;
    private const float DistanciaSuelo = 0.08f;

    // Jokalariaren hasierako posizioa.
    private Vector2 posicionInicial;

    // Jokalariaren osagai fisikoak eta animazio-sistema.
    private Rigidbody2D cuerpo;
    private Collider2D colisionador;
    private Animator anim;

    // Teklatuaren bidez jasotako mugimendu-eskaerak.
    private float horizontal;
    private bool saltoSolicitado;

    // Lurraren detekzioan erabiliko diren datuak.
    private readonly RaycastHit2D[] impactosSuelo = new RaycastHit2D[8];
    private ContactFilter2D filtroSuelo;

    /// <summary>
    /// Jokalariaren osagaiak eskuratzen ditu eta
    /// lurraren detekziorako iragazkia prestatzen du.
    /// </summary>
    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        colisionador = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        // Trigger motako objektuak lur gisa baztertzen dira.
        filtroSuelo = ContactFilter2D.noFilter;
        filtroSuelo.useTriggers = false;
    }

    /// <summary>
    /// Jokalariaren hasierako posizioa gordetzen du.
    /// </summary>
    private void Start()
    {
        posicionInicial = cuerpo.position;
    }

    /// <summary>
    /// Fotograma bakoitzean teklatuaren sarrera irakurtzen du
    /// eta pertsonaiaren animazioa kontrolatzen du.
    /// </summary>
    private void Update()
    {
        // Ezkerreko eta eskuineko mugimendua irakurtzen da.
        horizontal = Input.GetAxis("Horizontal");

        // Jauziaren eskaera tekla sakatzean behin bakarrik gordetzen da.
        if (Input.GetButtonDown("Jump"))
        {
            saltoSolicitado = true;
        }

        // Jokalaria mugitzen ari denean korrika-animazioa aktibatzen da.
        if (Mathf.Abs(horizontal) > 0.1f && anim != null)
        {
            anim.Play("PersonajeCorriendo");
        }
    }

    /// <summary>
    /// Fisikaren eguneratze bakoitzean mugimendu horizontala
    /// aplikatzen du eta eskatutako jauzia prozesatzen du.
    /// </summary>
    private void FixedUpdate()
    {
        // Abiadura horizontala eguneratzen da,
        // abiadura bertikala aldatu gabe.
        Vector2 abiadura = cuerpo.linearVelocity;

        abiadura.x = horizontal * VelocidadHorizontal;

        cuerpo.linearVelocity = abiadura;

        // Jauzi-eskaerarik ez badago, metodoa amaitzen da.
        if (!saltoSolicitado)
        {
            return;
        }

        // Jauzi-eskaera kontsumitzen da, ez errepikatzeko.
        saltoSolicitado = false;

        // Jokalaria airean badago, ezin du berriro salto egin.
        if (!EstaEnSuelo())
        {
            return;
        }

        // Aurreko abiadura bertikala ezabatzen da,
        // jauzi bakoitza baldintza berdinetatik hasteko.
        cuerpo.linearVelocity = new Vector2(
            cuerpo.linearVelocity.x,
            0f
        );

        // Goranzko bulkada bakarra aplikatzen da.
        cuerpo.AddForce(
            Vector2.up * ImpulsoSalto,
            ForceMode2D.Impulse
        );
    }

    /// <summary>
    /// Jokalariaren kolisionatzailea beherantz proiektatzen du,
    /// lurra edo plataforma bat detektatzeko.
    /// </summary>
    /// <returns>
    /// true, jokalaria gainazal baten gainean badago;
    /// false, airean badago.
    /// </returns>
    private bool EstaEnSuelo()
    {
        // Kolisionatzailearen azpiko eremua aztertzen da.
        int kopurua = colisionador.Cast(
            Vector2.down,
            filtroSuelo,
            impactosSuelo,
            DistanciaSuelo
        );

        // Detektatutako gainazal guztiak aztertzen dira.
        for (int i = 0; i < kopurua; i++)
        {
            RaycastHit2D inpaktua = impactosSuelo[i];

            // Gainazalak baliozkoa izan behar du,
            // eta gorantz orientatutako normala eduki behar du.
            if (inpaktua.collider != null &&
                !inpaktua.collider.isTrigger &&
                inpaktua.normal.y > 0.5f)
            {
                return true;
            }
        }

        // Ez da lur egokirik detektatu.
        return false;
    }

    /// <summary>
    /// Jokalaria hasierako posiziora itzultzen du,
    /// eta mugimenduaren abiadura berrezartzen du.
    /// </summary>
    public void Recolocar()
    {
        // Hasierako posizioa berreskuratzen da.
        cuerpo.position = posicionInicial;

        // Mugimenduaren inertzia ezabatzen da.
        cuerpo.linearVelocity = Vector2.zero;
        cuerpo.angularVelocity = 0f;

        // Aurreko mugimendu- eta jauzi-eskaerak garbitzen dira.
        horizontal = 0f;
        saltoSolicitado = false;
    }
}
