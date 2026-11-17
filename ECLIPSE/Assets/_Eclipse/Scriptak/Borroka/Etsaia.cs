using System;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Etsai baten egokitzailea: EtsaiEgoeraMakina + BizitzaLogika + GorputzKalteLogika, NavMeshAgent-arekin eta Animator-arekin.
    /// Eredu bisualarekiko independentea: behin-behineko etsaia eta etorkizuneko munstroak aurrefabrikatu hutsak dira osagai honekin.
    /// Desmenbratzea: ZatiaHautsita gertaera; Desmenbratzea osagaiak (aukerakoa) zatiak bereizten ditu eta faktoreak aldatzen ditu.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Etsaia : MonoBehaviour, IKalteHartzailea
    {
        private const string Marka = "ECL_BORROKA";
        private static readonly int AbiaduraId = Animator.StringToHash("Abiadura");
        private static readonly int ErasoId = Animator.StringToHash("Eraso");
        private static readonly int KalteaId = Animator.StringToHash("Kaltea");
        private static readonly int HildaId = Animator.StringToHash("Hilda");

        [SerializeField] private Animator _animator;
        [SerializeField] private ParticleSystem _odola;
        [SerializeField] private ParticleSystem _txundidura;
        [SerializeField] private float _bizitza = 100f;
        [SerializeField] private float _erasoKaltea = 20f;
        [SerializeField] private float _ikusDistantzia = 18f;
        [SerializeField] private float _ikusAngelua = 75f;
        [SerializeField] private float _hurbilAntzematea = 4f;
        [SerializeField] private float _jarraituAbiadura = 4.2f;
        [SerializeField] private float _bilatuAbiadura = 2.2f;

        private NavMeshAgent _agentea;
        private EtsaiEgoeraMakina _makina;
        private BizitzaLogika _bizitzaLogika;
        private GorputzKalteLogika _gorputza;
        private JokalariBizitza _jokalaria;
        private Vector3 _azkenLekua;
        private float _hurrengoBidea;
        private float _geldirikDenbora;

        /// <summary>Kolpea emateko gehienezko altuera-diferentzia (m): beste solairu batetik ez da jotzen.</summary>
        private const float KolpeAltuera = 1.2f;

        /// <summary>Jarraitzean denbora hau mugitu gabe: bidea berriro kalkulatu; bikoitza: bilatzera (trabatuta).</summary>
        private const float TrabaDenbora = 1.5f;

        public EtsaiEgoera Egoera => _makina != null ? _makina.Egoera : EtsaiEgoera.Zain;

        public BizitzaLogika Bizitza => _bizitzaLogika;

        public GorputzKalteLogika Gorputza => _gorputza;

        public bool Bizirik => _bizitzaLogika != null && _bizitzaLogika.Bizirik;

        /// <summary>Probetarako: IA geldituta (jomuga gisa). Kaltea eta heriotza berdin dabiltza.</summary>
        public bool AdimenaGelditu { get; set; }

        public int Kolpeak { get; private set; }

        /// <summary>Distantziagatik, hormagatik edo altueragatik baztertutako kolpeak (probetarako).</summary>
        public int KolpeBaztertuak { get; private set; }

        public int Trabak { get; private set; }

        /// <summary>Mugimendu-abiaduraren biderkatzailea (desmenbratzea: hanka gabe motelago). 1 lehenetsita.</summary>
        public float AbiaduraFaktorea { get; set; } = 1f;

        /// <summary>Eraso-kaltearen biderkatzailea (desmenbratzea: besorik gabe ahulago). 1 lehenetsita.</summary>
        public float ErasoFaktorea { get; set; } = 1f;

        public Animator Animatzailea => _animator;

        /// <summary>Jasotako azken kaltea (desmenbratzea: zatia tiroaren norabidean botatzeko).</summary>
        public KalteInfo AzkenKaltea { get; private set; }

        public event Action<Etsaia, KalteZona> ZatiaHautsita;

        public event Action<Etsaia> Hildakoa;

        public void Konfiguratu(Animator animatzailea, ParticleSystem odola, ParticleSystem txundidura)
        {
            _animator = animatzailea;
            _odola = odola;
            _txundidura = txundidura;
        }

        private void Awake()
        {
            _agentea = GetComponent<NavMeshAgent>();
            _makina = new EtsaiEgoeraMakina();
            _bizitzaLogika = new BizitzaLogika(_bizitza);
            _gorputza = new GorputzKalteLogika();
            _azkenLekua = transform.position;
        }

        private void Update()
        {
            if (!Bizirik)
            {
                return;
            }

            if (_jokalaria == null)
            {
                _jokalaria = FindAnyObjectByType<JokalariBizitza>();
            }

            if (AdimenaGelditu || _jokalaria == null || !_agentea.isOnNavMesh)
            {
                if (_agentea.isOnNavMesh)
                {
                    _agentea.isStopped = true;
                }

                Animatu(0f);
                return;
            }

            Vector3 jokalaria = _jokalaria.transform.position;
            Vector3 bektorea = jokalaria - transform.position;
            bektorea.y = 0f;
            float distantzia = bektorea.magnitude;
            bool ikusten = _jokalaria.Bizirik && Ikusten(jokalaria, bektorea, distantzia);
            if (ikusten)
            {
                _azkenLekua = jokalaria;
            }

            // Azken lekura iritsi da: distantzia erreala (remainingDistance 0 da agenteak oraindik biderik ez duenean).
            Vector3 azkenera = _azkenLekua - transform.position;
            azkenera.y = 0f;
            bool helburuan = azkenera.magnitude <= _agentea.stoppingDistance + 0.5f;
            EtsaiErabakia erabakia = _makina.Eguneratu(Time.deltaTime, ikusten, distantzia, helburuan);
            if (erabakia.Mugitu)
            {
                _agentea.isStopped = false;
                _agentea.speed = (erabakia.Egoera == EtsaiEgoera.Jarraitu ? _jarraituAbiadura : _bilatuAbiadura) * AbiaduraFaktorea;
                if (Time.time >= _hurrengoBidea)
                {
                    _hurrengoBidea = Time.time + 0.2f;
                    _agentea.SetDestination(_azkenLekua);
                }

                TrabaEgiaztatu();
            }
            else
            {
                _agentea.isStopped = true;
                _geldirikDenbora = 0f;
            }

            if (_txundidura != null)
            {
                ParticleSystem.EmissionModule emisioa = _txundidura.emission;
                emisioa.enabled = erabakia.Egoera == EtsaiEgoera.Txundituta;
            }

            if (erabakia.Egoera == EtsaiEgoera.Eraso && distantzia > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(bektorea), 360f * Time.deltaTime);
            }

            if (erabakia.EgoeraAldatu && erabakia.Egoera == EtsaiEgoera.Eraso && _animator != null)
            {
                _animator.SetTrigger(ErasoId);
            }

            if (erabakia.Kolpea)
            {
                // Kolpea benetan iristen da: altuera antzekoa eta bularretik bularrera hormarik ez.
                bool altuera = Mathf.Abs(jokalaria.y - transform.position.y) <= KolpeAltuera;
                bool librea = !Physics.Linecast(transform.position + Vector3.up * 1.3f, jokalaria + Vector3.up * 1.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (altuera && librea)
                {
                    Kolpeak++;
                    _jokalaria.KalteaJaso(new KalteInfo(KalteZona.Enborra, _erasoKaltea * ErasoFaktorea, jokalaria + Vector3.up, bektorea.normalized));
                }
                else
                {
                    KolpeBaztertuak++;
                }
            }

            Animatu(_agentea.velocity.magnitude);
        }

        /// <summary>Jarraitzen ari da baina ez da mugitzen: bidea berriro kalkulatu; luzaroan, bilatzera (azken lekua ezin da iritsi).</summary>
        private void TrabaEgiaztatu()
        {
            bool urrun = !_agentea.pathPending && _agentea.remainingDistance > _agentea.stoppingDistance + 0.5f;
            _geldirikDenbora = urrun && _agentea.velocity.sqrMagnitude < 0.01f ? _geldirikDenbora + Time.deltaTime : 0f;
            if (_geldirikDenbora >= TrabaDenbora && _geldirikDenbora - Time.deltaTime < TrabaDenbora)
            {
                _agentea.ResetPath();
                _agentea.SetDestination(_azkenLekua);
            }
            else if (_geldirikDenbora >= TrabaDenbora * 2f)
            {
                Trabak++;
                _geldirikDenbora = 0f;
                _agentea.ResetPath();
                _makina.Galdu();
            }
        }

        private bool Ikusten(Vector3 jokalaria, Vector3 bektorea, float distantzia)
        {
            if (distantzia > _ikusDistantzia)
            {
                return false;
            }

            if (distantzia > _hurbilAntzematea && Vector3.Angle(transform.forward, bektorea) > _ikusAngelua)
            {
                return false;
            }

            // Begietatik jokalariaren bularrera: hormak bakarrik (trigger-ak eta Ignore Raycast geruza baztertuta).
            return !Physics.Linecast(transform.position + Vector3.up * 1.6f, jokalaria + Vector3.up * 1.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void Animatu(float abiadura)
        {
            if (_animator != null)
            {
                _animator.SetFloat(AbiaduraId, abiadura, 0.1f, Time.deltaTime);
            }
        }

        /// <summary>Tiroa: hurbileko soinuak bezala jarraitzera bultzatzen du.</summary>
        public void Entzun(Vector3 lekua)
        {
            if (!Bizirik)
            {
                return;
            }

            _azkenLekua = lekua;
            _makina.Entzun();
        }

        public void KalteaJaso(in KalteInfo kaltea)
        {
            if (!Bizirik)
            {
                return;
            }

            AzkenKaltea = kaltea;
            ZonaKaltea zonaKaltea = _gorputza.Aplikatu(kaltea.Zona, kaltea.Kopurua);
            KalteEmaitza emaitza = _bizitzaLogika.KalteaHartu(zonaKaltea.Kaltea, Time.time);
            if (_odola != null)
            {
                _odola.transform.SetPositionAndRotation(kaltea.Puntua, Quaternion.LookRotation(-kaltea.Norabidea));
                _odola.Emit(zonaKaltea.Zona == KalteZona.Burua ? 40 : 24);
            }

            if (zonaKaltea.ZatiaHautsiDa)
            {
                Debug.Log($"{Marka}_ZATIA {name} {zonaKaltea.Zona} hautsita");
                ZatiaHautsita?.Invoke(this, zonaKaltea.Zona);
            }

            if (_jokalaria != null)
            {
                _azkenLekua = _jokalaria.transform.position;
            }

            if (emaitza == KalteEmaitza.Hilda)
            {
                Hil();
                return;
            }

            _makina.KalteaJaso();
            if (_animator != null)
            {
                _animator.SetTrigger(KalteaId);
            }

            // Atzera-bultzada txikia (NavMesh-ean, hormarik zeharkatu gabe).
            if (_agentea.isOnNavMesh)
            {
                Vector3 bultzada = kaltea.Norabidea;
                bultzada.y = 0f;
                _agentea.Move(bultzada.normalized * 0.12f);
            }
        }

        private void Hil()
        {
            _makina.Hil();
            if (_agentea.isOnNavMesh)
            {
                _agentea.isStopped = true;
            }

            _agentea.enabled = false;
            foreach (Collider k in GetComponentsInChildren<Collider>())
            {
                k.enabled = false;
            }

            if (_animator != null)
            {
                _animator.SetBool(HildaId, true);
            }

            Debug.Log($"{Marka}_ETSAIA_HIL {name} {transform.position}");
            Hildakoa?.Invoke(this);
        }
    }
}
