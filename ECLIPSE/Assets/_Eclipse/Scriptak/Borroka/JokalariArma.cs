using System;
using Eclipse.Jokalaria;
using Eclipse.Proba;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Borroka
{
    /// <summary>Azken tiroaren emaitza (HUD, probak eta, geroago, estatistikak).</summary>
    public readonly struct TiroInfo
    {
        public TiroInfo(Vector3 inpaktua, IKalteHartzailea jo, KalteZona zona, bool oztopatuta, Collider kolisioa)
        {
            Inpaktua = inpaktua;
            Jo = jo;
            Zona = zona;
            Oztopatuta = oztopatuta;
            Kolisioa = kolisioa;
        }

        public Vector3 Inpaktua { get; }

        /// <summary>Kaltea jaso duena, edo null (horma edo hutsunea).</summary>
        public IKalteHartzailea Jo { get; }

        public KalteZona Zona { get; }

        /// <summary>Erretikulak zerbait jotzen zuen, baina muturretik ez zegoen bide librerik (horma hurbil batek gelditu du).</summary>
        public bool Oztopatuta { get; }

        public Collider Kolisioa { get; }
    }

    /// <summary>
    /// EMP pistola jokalariaren eskuan (ArmaLogika-ren egokitzailea). Sarrera: eskuineko botoia apuntatu, ezkerrekoa tiro, R birkargatu.
    /// Tiroa erretikulatik (kameraren erdigunetik) kalkulatzen da, eta muturretik inpakturako lerroa egiaztatzen da: hormen ondoan
    /// ez dago tirorik horma zeharkatzen duenik. Kontrolatzailearen mugimendua ez da aldatzen; kamera hurbiltzen da apuntatzean.
    /// </summary>
    public sealed class JokalariArma : MonoBehaviour
    {
        public const string PistolaBidea = "Borroka/PF_EmpPistola";
        private const string Marka = "ECL_BORROKA";
        private const float Irismena = 80f;
        private const float OinarriKaltea = 25f;
        private const float ZarataErradioa = 25f;
        private const int ArmaGeruza = 1;

        /// <summary>Apuntatzean kamera: sorbaldatik hurbilago.</summary>
        public static readonly Vector3 ApuntuDesplazamendua = new Vector3(0.6f, 0.05f, -1.3f);

        private static readonly int ApuntatzenId = Animator.StringToHash("Apuntatzen");
        private static readonly int ApuntuPitchId = Animator.StringToHash("ApuntuPitch");
        private static readonly int TiroId = Animator.StringToHash("Tiro");
        private static readonly int BirkargaId = Animator.StringToHash("Birkarga");

        private EsploratzaileKontrola _kontrola;
        private JokalariBizitza _bizitza;
        private BorrokaHud _hud;
        private EmpPistolaBisuala _pistola;
        private Animator _an;
        private Transform _eskua;
        private Transform _besaurrea;
        private Vector3 _azkenApuntua;

        /// <summary>Heldulekua ukabilaren erdian: eskumuturretik besaurrearen norabidean (m). Kapturekin doitua.</summary>
        private const float HelduDistantzia = 0.075f;

        public ArmaLogika Logika { get; } = new ArmaLogika();

        public bool Apuntatzen { get; private set; }

        public EmpPistolaBisuala Pistola => _pistola;

        public TiroInfo AzkenTiroa { get; private set; }

        public event Action<TiroInfo> TiroEginda;

        /// <summary>Jokalariari arma eta bizitza gehitu (proba-eszena; misioetan, Ion-ek onartu ondoren).</summary>
        public static JokalariArma Gehitu(EsploratzaileKontrola kontrola, bool berpiztuAutomatikoki)
        {
            BorrokaHud hud = BorrokaHud.Sortu();
            var bizitza = kontrola.gameObject.AddComponent<JokalariBizitza>();
            bizitza.Konfiguratu(kontrola, hud);
            bizitza.BerpiztuAutomatikoki = berpiztuAutomatikoki;
            var arma = kontrola.gameObject.AddComponent<JokalariArma>();
            arma.Konfiguratu(kontrola, bizitza, hud);
            return arma;
        }

        private void Konfiguratu(EsploratzaileKontrola kontrola, JokalariBizitza bizitza, BorrokaHud hud)
        {
            _kontrola = kontrola;
            _bizitza = bizitza;
            _hud = hud;
            JokalariBisuala bisuala = kontrola.Bisuala;
            _an = bisuala != null ? bisuala.Animatzailea : null;
            Transform eskua = _an != null ? _an.GetBoneTransform(HumanBodyBones.RightHand) : null;
            _eskua = eskua;
            _besaurrea = _an != null ? _an.GetBoneTransform(HumanBodyBones.RightLowerArm) : null;
            Transform aingura = eskua != null ? eskua.Find("Arma_Eskuina") : null;
            GameObject aurrefab = Resources.Load<GameObject>(PistolaBidea);
            if (aingura == null || aurrefab == null)
            {
                Debug.LogWarning($"{Marka}_ARMA_ERROR Pistolaren eredua ez da jarri (aingura {aingura != null}, aurrefab {aurrefab != null}): tiroak kameratik.");
            }
            else
            {
                GameObject pistola = Instantiate(aurrefab, aingura, false);
                pistola.name = aurrefab.name;
                foreach (Transform t in pistola.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = kontrola.gameObject.layer;
                }

                _pistola = pistola.GetComponent<EmpPistolaBisuala>();
                bisuala.ErrendatzaileakEguneratu();
            }

            if (_an != null)
            {
                _an.SetLayerWeight(ArmaGeruza, 1f);
            }

            Debug.Log($"{Marka}_ARMA_OK EMP pistola: {Logika.Kargadorean}/{Logika.Erreserba}, eredua {_pistola != null}");
        }

        private void Update()
        {
            Logika.Eguneratu(Time.time);
            Mouse sagua = Mouse.current;
            Keyboard teklatua = Keyboard.current;
            bool sarrera = _kontrola != null && _kontrola.enabled && !_kontrola.SarreraBlokeatuta && Cursor.lockState == CursorLockMode.Locked
                           && sagua != null && teklatua != null && (_bizitza == null || _bizitza.Bizirik);
            if (sarrera)
            {
                Apuntatu(sagua.rightButton.isPressed);
                if (sagua.leftButton.wasPressedThisFrame)
                {
                    TiroEgin();
                }

                if (teklatua.rKey.wasPressedThisFrame)
                {
                    Birkargatu();
                }
            }
            else if (_bizitza != null && !_bizitza.Bizirik)
            {
                Apuntatu(false);
            }

            if (_an != null && _kontrola != null)
            {
                _an.SetBool(ApuntatzenId, Apuntatzen);
                _an.SetFloat(ApuntuPitchId, _kontrola.BegiradaPitch, 0.08f, Time.deltaTime);
            }

            if (_hud != null)
            {
                _hud.Eguneratu(Logika, _bizitza != null ? _bizitza.Logika : null, Apuntatzen);
            }
        }

        private void LateUpdate()
        {
            if (_pistola == null)
            {
                return;
            }

            // Pose hezurretatik (Mixamo-ren ardatz lokalen menpe egon gabe): heldulekua ukabilean, kanoia besaurrearen norabidean;
            // apuntatzean, erretikularen puntura (tiroaren lerroa eta eredua bat datoz).
            if (_eskua == null || _besaurrea == null)
            {
                return;
            }

            Vector3 besoa = (_eskua.position - _besaurrea.position).normalized;
            Vector3 heldulekua = _eskua.position + besoa * HelduDistantzia;
            Vector3 aurrera = besoa;
            if (Apuntatzen)
            {
                Vector3 norabidea = ApuntuPuntua() - heldulekua;
                if (norabidea.sqrMagnitude > 0.01f)
                {
                    aurrera = norabidea.normalized;
                }
            }

            Vector3 gora = Vector3.ProjectOnPlane(Vector3.up, aurrera);
            _pistola.transform.SetPositionAndRotation(heldulekua, Quaternion.LookRotation(aurrera, gora.sqrMagnitude > 1e-4f ? gora : Vector3.forward));
        }

        public void Apuntatu(bool apuntatu)
        {
            if (Apuntatzen == apuntatu || _kontrola == null)
            {
                return;
            }

            Apuntatzen = apuntatu;
            if (_kontrola.Bisuala != null)
            {
                _kontrola.Bisuala.Modua = apuntatu ? OrientazioModua.Kamera : OrientazioModua.Mugimendua;
            }

            _kontrola.KameraDesplazamendua = apuntatu ? ApuntuDesplazamendua : EsploratzaileKontrola.SorbaldaLehenetsia;
        }

        public bool Birkargatu()
        {
            if (!Logika.BirkargaHasi(Time.time))
            {
                return false;
            }

            if (_an != null)
            {
                _an.SetTrigger(BirkargaId);
            }

            if (_pistola != null)
            {
                _pistola.Birkarga();
            }

            return true;
        }

        public TiroEmaitza TiroEgin()
        {
            TiroEmaitza emaitza = Logika.Tiro(Time.time);
            if (emaitza == TiroEmaitza.Hutsik && _pistola != null)
            {
                _pistola.Hutsik();
            }

            if (emaitza == TiroEmaitza.Tiro)
            {
                Jaurtiketa();
            }

            return emaitza;
        }

        /// <summary>Erretikulak jotzen duen puntua: trigger-ak eta jokalaria baztertuta (kalte-zonak izan ezik).</summary>
        private bool ErretikulaJo(out RaycastHit jo)
        {
            Camera kamera = _kontrola.Kamera;
            Ray izpia = kamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] guztiak = Physics.RaycastAll(izpia, Irismena, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(guztiak, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit h in guztiak)
            {
                if (!h.collider.isTrigger)
                {
                    jo = h;
                    return true;
                }

                var zona = h.collider.GetComponent<KalteZonaKolisioa>();
                if (zona != null && zona.Jabea != null && zona.Jabea.Bizirik)
                {
                    jo = h;
                    return true;
                }
            }

            jo = default;
            return false;
        }

        private Vector3 ApuntuPuntua()
        {
            if (ErretikulaJo(out RaycastHit jo))
            {
                _azkenApuntua = jo.point;
            }
            else
            {
                Transform k = _kontrola.Kamera.transform;
                _azkenApuntua = k.position + k.forward * Irismena;
            }

            return _azkenApuntua;
        }

        private void Jaurtiketa()
        {
            Transform k = _kontrola.Kamera.transform;
            Vector3 mutura = _pistola != null ? _pistola.Mutura.position : k.position;
            bool baDu = ErretikulaJo(out RaycastHit jo);
            Vector3 helburua = baDu ? jo.point : k.position + k.forward * Irismena;
            Vector3 normala = baDu ? jo.normal : -k.forward;
            Collider kolisioa = baDu ? jo.collider : null;

            // Bularretik muturrera eta muturretik helburura bide librea? (Muturra horma baten barruan badago, izpi batek ez du
            // detektatzen hasten den kolisioa: horregatik bularretik ere egiaztatzen da.) Horma batek gelditzen badu, han jotzen du.
            bool oztopatuta = false;
            Vector3 bularra = transform.position + Vector3.up * 1.4f;
            foreach ((Vector3 a, Vector3 b) in new[] { (bularra, mutura), (mutura, helburua) })
            {
                Vector3 lerroa = b - a;
                if (Physics.Raycast(a, lerroa.normalized, out RaycastHit oztopoa, Mathf.Max(0f, lerroa.magnitude - 0.05f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    oztopatuta = true;
                    helburua = oztopoa.point;
                    normala = oztopoa.normal;
                    kolisioa = oztopoa.collider;
                    break;
                }
            }

            IKalteHartzailea hartzailea = null;
            KalteZona zona = KalteZona.Enborra;
            if (!oztopatuta && kolisioa != null)
            {
                var zonaKolisioa = kolisioa.GetComponent<KalteZonaKolisioa>();
                if (zonaKolisioa != null && zonaKolisioa.Jabea != null)
                {
                    hartzailea = zonaKolisioa.Jabea;
                    zona = zonaKolisioa.Zona;
                    hartzailea.KalteaJaso(new KalteInfo(zona, OinarriKaltea, helburua, (helburua - mutura).normalized));
                    if (_hud != null)
                    {
                        _hud.Jo(!hartzailea.Bizirik);
                    }
                }
            }

            if (_pistola != null)
            {
                _pistola.Tiroa(helburua, normala, hartzailea != null);
            }

            if (_an != null)
            {
                _an.SetTrigger(TiroId);
            }

            foreach (Etsaia etsaia in FindObjectsByType<Etsaia>(FindObjectsInactive.Exclude))
            {
                if (Vector3.Distance(etsaia.transform.position, transform.position) <= ZarataErradioa)
                {
                    etsaia.Entzun(transform.position);
                }
            }

            AzkenTiroa = new TiroInfo(helburua, hartzailea, zona, oztopatuta, kolisioa);
            TiroEginda?.Invoke(AzkenTiroa);
        }
    }
}
