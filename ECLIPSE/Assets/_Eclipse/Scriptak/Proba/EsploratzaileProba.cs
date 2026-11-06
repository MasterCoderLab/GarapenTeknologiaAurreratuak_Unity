#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Eclipse.Proba
{
    /// <summary>
    /// Behin-behineko esploratzailea, eszena probatzeko: ez da protagonista. Play-n bakarrik sortzen da
    /// 01_Instalazioa eszenan, eta ezer ez da gordetzen eszenan. Azken build-etik kanpo geratzen da.
    /// </summary>
    public static class EsploratzaileAbiarazlea
    {
        private const string Marka = "ECL_ESPLORATZAILEA";
        private const string Eszena = "01_Instalazioa";
        private const string Sarrera = "Sektorea1/Gela_S1_Sarrera";
        private const string Korridorea = "Sektorea1/Gela_S1_Korridorea";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Abiarazi()
        {
            if (SceneManager.GetActiveScene().name != Eszena)
            {
                return;
            }

            // Benetako jokalaria dagoenean, esploratzailea ez da sortzen.
            if (Object.FindAnyObjectByType<CharacterController>() != null)
            {
                Debug.Log($"{Marka}_OK CharacterController bat badago eszenan: ez da esploratzailerik sortu.");
                return;
            }

            GameObject sarrera = GameObject.Find(Sarrera);
            GameObject korridorea = GameObject.Find(Korridorea);
            if (sarrera == null || korridorea == null)
            {
                Debug.LogError($"{Marka}_ERROR Ez da aurkitu {Sarrera} edo {Korridorea}.");
                return;
            }

            Physics.SyncTransforms();
            if (!PosizioSeguruaBilatu(sarrera.transform.position, out Vector3 posizioa))
            {
                Debug.LogError($"{Marka}_ERROR Ez da posizio segururik aurkitu Sarreran.");
                return;
            }

            Vector3 norabidea = korridorea.transform.position - posizioa;
            norabidea.y = 0f;
            EsploratzaileKontrola.Sortu(posizioa, Quaternion.LookRotation(norabidea));
            Debug.Log($"{Marka}_OK Esploratzailea sortuta: {posizioa}, korridorerantz begira.");
        }

        /// <summary>
        /// Gelaren erditik hasi eta inguruan bilatu: benetako kolisioekin egiaztatzen da zorua dagoela eta kapsula sartzen dela.
        /// </summary>
        private static bool PosizioSeguruaBilatu(Vector3 erdia, out Vector3 posizioa)
        {
            for (int erradioa = 0; erradioa <= 4; erradioa++)
            {
                for (int x = -erradioa; x <= erradioa; x++)
                {
                    for (int z = -erradioa; z <= erradioa; z++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) == erradioa
                            && EsploratzaileKontrola.PosizioaLibre(erdia + new Vector3(x, 0f, z), out posizioa))
                        {
                            return true;
                        }
                    }
                }
            }

            posizioa = erdia;
            return false;
        }
    }

    /// <summary>Mugimendua, begirada eta bi kamera-moduak. Sarrera Input System-etik zuzenean (proiektuan hori bakarrik dago aktibo).</summary>
    public sealed class EsploratzaileKontrola : MonoBehaviour
    {
        private const float Altuera = 1.8f;
        private const float MakurAltuera = 1.1f;
        private const float BegiTartea = 0.15f;
        private const float AltueraAbiadura = 5f;
        private const float MakurAbiadura = 1.5f;
        private const float JauziAbiadura = 6.26f;
        private const float Erradioa = 0.3f;
        private const float Abiadura = 3f;
        private const float KorrikaAbiadura = 6f;
        private const float Grabitatea = -19.6f;
        private const float Sentikortasuna = 0.1f;
        private const float HirugarrenDistantzia = 3f;
        private const float KameraErradioa = 0.2f;
        private const float ErorketaMuga = -10f;
        private const int IgnoreRaycastGeruza = 2;

        private CharacterController _kontrolagailua;
        private Transform _burua;
        private Camera _kamera;
        private Renderer _gorputza;
        private Vector3 _hasiera;
        private Quaternion _hasierakoBiraketa;
        private float _pitch;
        private float _abiaduraY;
        private bool _hirugarrena;
        private bool _sarreraBlokeatuta;
        private bool _makurtuNahi;
        private float _altuera = Altuera;

        /// <summary>Interfaze bat irekita: ez mugitu, ez begiratu, kurtsorea askatu. Ixtean kurtsorea berriz hartzen da.</summary>
        public bool SarreraBlokeatuta
        {
            get => _sarreraBlokeatuta;
            set
            {
                if (_sarreraBlokeatuta != value)
                {
                    _sarreraBlokeatuta = value;
                    KurtsoreaBlokeatu(!value);
                }
            }
        }

        public float UnekoAltuera => _altuera;

        /// <summary>Makurtuta edo zutitzen: oraindik ezin da korrika egin ez jauzi.</summary>
        public bool Makurtuta => _makurtuNahi || _altuera < Altuera;

        /// <summary>Teklatutik kanpo makurtzeko (probak). Teklatua irakurtzen den fotograman, teklak agintzen du.</summary>
        public void MakurtuEzarri(bool makurtu) => _makurtuNahi = makurtu;

        /// <summary>Oin-posizio honetan zutik jar daiteke: makurtuta dagoen kapsularen gainean ez dago ezer.</summary>
        public static bool ZutikLibre(Vector3 oinak)
        {
            return !Physics.CheckCapsule(
                oinak + Vector3.up * (MakurAltuera - Erradioa),
                oinak + Vector3.up * (Altuera - Erradioa + 0.05f),
                Erradioa - 0.01f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>HUD berriak bere kontrol-laguntza du: OnGUI lerroa ez marraztu.</summary>
        public bool OnGuiEzkutatu { get; set; }

        public static void Sortu(Vector3 posizioa, Quaternion biraketa)
        {
            var objektua = new GameObject("Esploratzailea_Proba") { layer = IgnoreRaycastGeruza };
            objektua.transform.SetPositionAndRotation(posizioa, biraketa);
            objektua.AddComponent<EsploratzaileKontrola>();
        }

        /// <summary>Oin-posizio batean: azpian zorua dago eta kapsula ez du ezerekin talka egiten.</summary>
        public static bool PosizioaLibre(Vector3 oinak, out Vector3 zuzendua)
        {
            zuzendua = oinak;
            if (!Physics.Raycast(oinak + Vector3.up, Vector3.down, out RaycastHit jo, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            zuzendua = jo.point;
            Vector3 behea = zuzendua + Vector3.up * (Erradioa + 0.05f);
            Vector3 goia = zuzendua + Vector3.up * (Altuera - Erradioa + 0.05f);
            return !Physics.CheckCapsule(behea, goia, Erradioa, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void Awake()
        {
            _hasiera = transform.position;
            _hasierakoBiraketa = transform.rotation;

            _kontrolagailua = gameObject.AddComponent<CharacterController>();
            _kontrolagailua.height = Altuera;
            _kontrolagailua.radius = Erradioa;
            _kontrolagailua.center = Vector3.up * (Altuera * 0.5f);
            _kontrolagailua.skinWidth = 0.05f;
            _kontrolagailua.stepOffset = 0.3f;
            _kontrolagailua.minMoveDistance = 0f;

            GameObject gorputza = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            DestroyImmediate(gorputza.GetComponent<Collider>());
            gorputza.name = "Gorputza";
            gorputza.layer = IgnoreRaycastGeruza;
            gorputza.transform.SetParent(transform, false);
            gorputza.transform.localPosition = Vector3.up * (Altuera * 0.5f);
            gorputza.transform.localScale = new Vector3(Erradioa * 2f, Altuera * 0.5f, Erradioa * 2f);
            _gorputza = gorputza.GetComponent<Renderer>();

            _burua = new GameObject("Burua").transform;
            _burua.SetParent(transform, false);
            _burua.localPosition = Vector3.up * (Altuera - BegiTartea);

            _kamera = Camera.main;
            if (_kamera == null)
            {
                var kameraObjektua = new GameObject("Esploratzailea_Kamera") { tag = "MainCamera" };
                _kamera = kameraObjektua.AddComponent<Camera>();
                kameraObjektua.AddComponent<AudioListener>();
            }

            // Eszenako kamera berrerabili: Play amaitzean jatorrizko egoerara itzultzen da.
            _kamera.nearClipPlane = 0.05f;
            _kamera.transform.SetParent(_burua, false);
            KameraModuaEzarri(false);
            KurtsoreaBlokeatu(true);
        }

        private void Update()
        {
            Keyboard teklatua = Keyboard.current;
            Mouse sagua = Mouse.current;
            if (teklatua == null || sagua == null)
            {
                return;
            }

            if (_sarreraBlokeatuta)
            {
                // Grabitatea bakarrik: jokalaria ez da airean geratzen.
                Mugitu(null);
                return;
            }

            if (teklatua.escapeKey.wasPressedThisFrame)
            {
                KurtsoreaBlokeatu(false);
            }
            else if (Cursor.lockState != CursorLockMode.Locked && sagua.leftButton.wasPressedThisFrame)
            {
                KurtsoreaBlokeatu(true);
            }

            bool blokeatuta = Cursor.lockState == CursorLockMode.Locked;
            if (blokeatuta)
            {
                Begiratu(sagua.delta.ReadValue());
                if (teklatua.vKey.wasPressedThisFrame)
                {
                    KameraModuaEzarri(!_hirugarrena);
                }
            }

            Mugitu(blokeatuta ? teklatua : null);
        }

        private void LateUpdate()
        {
            if (!_hirugarrena)
            {
                return;
            }

            // Kamera atzera, baina horma baten aurretik geldituta. Trigger-ak (Gela_*) ez dira kontuan hartzen.
            Vector3 atzera = -_burua.forward;
            float distantzia = HirugarrenDistantzia;
            if (Physics.SphereCast(_burua.position, KameraErradioa, atzera, out RaycastHit jo, HirugarrenDistantzia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                distantzia = Mathf.Max(jo.distance - 0.05f, 0f);
            }

            _kamera.transform.SetPositionAndRotation(_burua.position + atzera * distantzia, _burua.rotation);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (OnGuiEzkutatu)
            {
                return;
            }

            string kurtsorea = Cursor.lockState == CursorLockMode.Locked ? "Esc: kurtsorea askatu" : "Egin klik jokoan kurtsorea hartzeko";
            GUI.Label(new Rect(10f, 10f, 700f, 24f), $"WASD: mugitu  |  Shift: korrika  |  Ctrl: makurtu  |  Zuriunea: jauzi  |  V: kamera ({(_hirugarrena ? "3." : "1.")} pertsona)  |  {kurtsorea}");
        }

        private void Begiratu(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * Sentikortasuna, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y * Sentikortasuna, -85f, 85f);
            _burua.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Mugitu(Keyboard teklatua)
        {
            var sarrera = Vector2.zero;
            bool korrika = false;
            bool jauzi = false;
            if (teklatua != null)
            {
                _makurtuNahi = teklatua.leftCtrlKey.isPressed;
                jauzi = teklatua.spaceKey.wasPressedThisFrame;
                sarrera.x = (teklatua.dKey.isPressed ? 1f : 0f) - (teklatua.aKey.isPressed ? 1f : 0f);
                sarrera.y = (teklatua.wKey.isPressed ? 1f : 0f) - (teklatua.sKey.isPressed ? 1f : 0f);
                korrika = teklatua.shiftKey.isPressed;
            }

            AltueraEguneratu();
            bool makurtuta = Makurtuta;
            Vector3 norabidea = Vector3.ClampMagnitude(transform.right * sarrera.x + transform.forward * sarrera.y, 1f);
            if (_kontrolagailua.isGrounded)
            {
                _abiaduraY = jauzi && !makurtuta ? JauziAbiadura : -2f;
            }
            else
            {
                _abiaduraY += Grabitatea * Time.deltaTime;
            }

            float lurAbiadura = makurtuta ? MakurAbiadura : korrika ? KorrikaAbiadura : Abiadura;
            CollisionFlags talkak = _kontrolagailua.Move((norabidea * lurAbiadura + Vector3.up * _abiaduraY) * Time.deltaTime);
            if ((talkak & CollisionFlags.Above) != 0 && _abiaduraY > 0f)
            {
                // Burua jo du: jauzia hor amaitzen da.
                _abiaduraY = 0f;
            }

            if (transform.position.y < ErorketaMuga)
            {
                Debug.LogWarning($"{name}: eszenatik erori da {transform.position} puntuan; hasierara itzuli da.");
                _kontrolagailua.enabled = false;
                transform.SetPositionAndRotation(_hasiera, _hasierakoBiraketa);
                _kontrolagailua.enabled = true;
                _abiaduraY = 0f;
            }
        }

        /// <summary>Altuera pixkanaka aldatzen da, oinak lekuan direla. Gainean zerbait badago, ez da zutitzen, tekla askatuta ere.</summary>
        private void AltueraEguneratu()
        {
            float helburua = _makurtuNahi || (_altuera < Altuera && !ZutikLibre(transform.position)) ? MakurAltuera : Altuera;
            if (Mathf.Approximately(_altuera, helburua))
            {
                return;
            }

            _altuera = Mathf.MoveTowards(_altuera, helburua, AltueraAbiadura * Time.deltaTime);
            _kontrolagailua.height = _altuera;
            _kontrolagailua.center = Vector3.up * (_altuera * 0.5f);
            _gorputza.transform.localPosition = Vector3.up * (_altuera * 0.5f);
            _gorputza.transform.localScale = new Vector3(Erradioa * 2f, _altuera * 0.5f, Erradioa * 2f);
            _burua.localPosition = Vector3.up * (_altuera - BegiTartea);
        }

        private void KameraModuaEzarri(bool hirugarrena)
        {
            _hirugarrena = hirugarrena;
            _gorputza.enabled = hirugarrena;
            if (!hirugarrena)
            {
                // Lehen pertsona: kamera kapsularen barruan dago, beraz ezin du hormarik zeharkatu.
                _kamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private static void KurtsoreaBlokeatu(bool blokeatu)
        {
            Cursor.lockState = blokeatu ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !blokeatu;
        }
    }
}
#endif
