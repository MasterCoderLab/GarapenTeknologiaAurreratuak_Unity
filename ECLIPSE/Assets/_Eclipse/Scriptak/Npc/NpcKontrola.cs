using System.Linq;
using Eclipse.Borroka;
using Eclipse.Proba;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.Npc
{
    /// <summary>
    /// Zientzialari edo soldadu baten egokitzailea (A3, E blokea, prototipoa): NpcEgoeraMakina + NavMeshAgent + Animator + burua.
    /// Pertzepzioa: jokalaria (EsploratzaileKontrola; misioetan ere badago), munstroak (Etsaia bizirik) eta jokalariaren tiro-hotsak.
    /// Soldaduak munstroei bakarrik egiten die tiro (inoiz ez jokalariari). Kalte-zonarik ez du: jokalariaren tiroek zeharkatzen dute
    /// (fuego amigo-a erabakitzeko dago). Misioetan ez dago: 06_NpcProba eszenan bakarrik.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcKontrola : MonoBehaviour
    {
        private const string Marka = "ECL_NPC";
        private static readonly int AbiaduraId = Animator.StringToHash("Abiadura");
        private static readonly int LaneanId = Animator.StringToHash("Lanean");
        private static readonly int ZainduId = Animator.StringToHash("Zaindu");

        [SerializeField] private NpcRola _rola = NpcRola.Zientzialaria;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _lanLekua;
        [SerializeField] private Transform[] _patruila = new Transform[0];
        [SerializeField] private Transform _kanoia;

        /// <summary>Soldaduaren fusila (eskuineko eskuaren umea): orientazioa mundu-koordenatuetan, hezurraren ardatzekiko independentea.</summary>
        [SerializeField] private Transform _fusila;
        [SerializeField] private LineRenderer _tiroIzpia;
        [SerializeField] private AudioSource _audioa;
        [SerializeField] private float _oinezAbiadura = 1.4f;
        [SerializeField] private float _korrikaAbiadura = 4.5f;
        [SerializeField] private float _tiroKaltea = 12f;
        [SerializeField] private float _ikusDistantzia = 14f;

        private const float EntzunDistantzia = 25f;
        private const float PatruilaItxaron = 2f;
        private const float BuruAngelua = 70f;

        private NavMeshAgent _agentea;
        private NpcEgoeraMakina _makina;
        private Transform _jokalaria;
        private JokalariArma _arma;
        private Etsaia _mehatxua;
        private Transform _burua;
        private bool _alarma;
        private float _hurrengoBilaketa;
        private int _patruilaIndizea;
        private float _itxaronArte;
        private float _ihesBerritu;
        private float _izpiaItzali;
        private float _buruPisua;
        private Vector3 _arriskuLekua;

        public NpcRola Rola => _rola;

        public NpcEgoera Egoera => _makina != null ? _makina.Egoera : NpcEgoera.Lanean;

        public Transform LanLekua => _lanLekua;

        public Transform Burua => _burua;

        /// <summary>Lan-lekuan iritsita eta lanean (Animator-eko "Lanean").</summary>
        public bool LanLekuan { get; private set; }

        public int Tiroak { get; private set; }

        public int Jotakoak { get; private set; }

        public void Konfiguratu(NpcRola rola, Animator animatzailea, Transform lanLekua, Transform[] patruila, Transform kanoia, LineRenderer izpia, AudioSource audioa)
        {
            _rola = rola;
            _animator = animatzailea;
            _lanLekua = lanLekua;
            _patruila = patruila ?? new Transform[0];
            _kanoia = kanoia;
            _tiroIzpia = izpia;
            _audioa = audioa;
        }

        /// <summary>Lan-lekua eta patruila runtime-n ezarri (MisioEdukiKargatzailea): egoera-makina berriz hasten da.</summary>
        public void KokapenaEzarri(Transform lanLekua, Transform[] patruila)
        {
            _lanLekua = lanLekua;
            _patruila = patruila ?? new Transform[0];
            _makina = new NpcEgoeraMakina(_rola, _lanLekua != null);
            _patruilaIndizea = 0;
        }

        private void Awake()
        {
            _agentea = GetComponent<NavMeshAgent>();
            _makina = new NpcEgoeraMakina(_rola, _lanLekua != null);
            _burua = _animator != null && _animator.isHuman ? _animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (_tiroIzpia != null)
            {
                _tiroIzpia.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (_arma != null)
            {
                _arma.TiroEginda -= TiroaEntzun;
                _arma = null;
            }
        }

        private void TiroaEntzun(TiroInfo tiroa)
        {
            if (_jokalaria != null && Vector3.Distance(_jokalaria.position, transform.position) <= EntzunDistantzia)
            {
                _alarma = true;
                _arriskuLekua = _jokalaria.position;
            }
        }

        private void Update()
        {
            if (_jokalaria == null)
            {
                EsploratzaileKontrola k = FindAnyObjectByType<EsploratzaileKontrola>();
                _jokalaria = k != null ? k.transform : null;
            }

            if (_arma == null && _jokalaria != null && _jokalaria.TryGetComponent(out JokalariArma arma))
            {
                _arma = arma;
                _arma.TiroEginda += TiroaEntzun;
            }

            if (Time.time >= _hurrengoBilaketa)
            {
                _hurrengoBilaketa = Time.time + 0.25f;
                _mehatxua = MehatxuHurbilena();
            }

            if (_tiroIzpia != null && _tiroIzpia.enabled && Time.time >= _izpiaItzali)
            {
                _tiroIzpia.enabled = false;
            }

            float jokalariDistantzia = float.PositiveInfinity;
            bool jokalariaIkusten = false;
            if (_jokalaria != null)
            {
                jokalariDistantzia = Horizontala(_jokalaria.position - transform.position).magnitude;
                jokalariaIkusten = jokalariDistantzia <= _ikusDistantzia && Libre(_jokalaria.position);
            }

            float mehatxuDistantzia = float.PositiveInfinity;
            bool mehatxuaIkusten = false;
            if (_mehatxua != null)
            {
                mehatxuDistantzia = Horizontala(_mehatxua.transform.position - transform.position).magnitude;
                mehatxuaIkusten = mehatxuDistantzia <= _ikusDistantzia + 4f && Libre(_mehatxua.transform.position);
                if (mehatxuaIkusten)
                {
                    _arriskuLekua = _mehatxua.transform.position;
                }
            }

            var pertzepzioa = new NpcPertzepzioa(jokalariDistantzia, jokalariaIkusten, mehatxuDistantzia, mehatxuaIkusten, _alarma);
            _alarma = false;
            NpcErabakia erabakia = _makina.Eguneratu(Time.deltaTime, pertzepzioa);
            if (erabakia.EgoeraAldatu)
            {
                Debug.Log($"{Marka}_EGOERA {name} ({_rola}): {erabakia.Egoera}");
            }

            LanLekuan = false;
            if (_agentea.isOnNavMesh)
            {
                Jokatu(erabakia);
            }

            if (_animator != null)
            {
                _animator.SetFloat(AbiaduraId, _agentea.velocity.magnitude, 0.1f, Time.deltaTime);
                _animator.SetBool(LaneanId, LanLekuan);
                _animator.SetBool(ZainduId, erabakia.Egoera == NpcEgoera.Zaindu);
            }
        }

        private void Jokatu(in NpcErabakia erabakia)
        {
            switch (erabakia.Egoera)
            {
                case NpcEgoera.Lanean:
                    if (Horizontala(_lanLekua.position - transform.position).magnitude > 0.35f)
                    {
                        Joan(_lanLekua.position, _oinezAbiadura);
                    }
                    else
                    {
                        Gelditu();
                        Begiratu(_lanLekua.position + _lanLekua.forward, 240f);
                        LanLekuan = true;
                    }

                    break;
                case NpcEgoera.Patruila:
                    Patruilatu();
                    break;
                case NpcEgoera.Begiratu:
                    Gelditu();
                    Begiratu(_jokalaria.position, 180f);
                    break;
                case NpcEgoera.Ihes:
                    if (erabakia.EgoeraAldatu || Time.time >= _ihesBerritu || (!_agentea.pathPending && _agentea.remainingDistance < 0.6f))
                    {
                        _ihesBerritu = Time.time + 1.5f;
                        Joan(IhesPuntua(_arriskuLekua), _korrikaAbiadura);
                    }

                    _agentea.speed = _korrikaAbiadura;
                    break;
                case NpcEgoera.Zaindu:
                    Gelditu();
                    Begiratu(_mehatxua != null ? _mehatxua.transform.position : _arriskuLekua, 300f);
                    if (erabakia.Tiro)
                    {
                        Tiro();
                    }

                    break;
            }
        }

        private void Patruilatu()
        {
            if (_patruila.Length == 0)
            {
                Gelditu();
                return;
            }

            Transform helburua = _patruila[_patruilaIndizea % _patruila.Length];
            if (Horizontala(helburua.position - transform.position).magnitude > 0.5f)
            {
                _itxaronArte = Time.time + PatruilaItxaron;
                Joan(helburua.position, _oinezAbiadura);
                return;
            }

            Gelditu();
            if (Time.time >= _itxaronArte)
            {
                _patruilaIndizea++;
            }
        }

        private void Joan(Vector3 helburua, float abiadura)
        {
            _agentea.isStopped = false;
            _agentea.speed = abiadura;
            if ((_agentea.destination - helburua).sqrMagnitude > 0.04f || !_agentea.hasPath)
            {
                _agentea.SetDestination(helburua);
            }
        }

        private void Gelditu()
        {
            _agentea.isStopped = true;
            _agentea.velocity = Vector3.zero;
        }

        private void Begiratu(Vector3 lekua, float abiadura)
        {
            Vector3 norabidea = Horizontala(lekua - transform.position);
            if (norabidea.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(norabidea), abiadura * Time.deltaTime);
            }
        }

        /// <summary>Arriskutik urrun dagoen NavMesh-eko puntu iristerraza (8 m, zortzi norabidetan saiatuta).</summary>
        private Vector3 IhesPuntua(Vector3 arriskua)
        {
            Vector3 kanpora = Horizontala(transform.position - arriskua);
            kanpora = kanpora.sqrMagnitude > 0.01f ? kanpora.normalized : -transform.forward;
            var bidea = new NavMeshPath();
            foreach (float angelua in new[] { 0f, 40f, -40f, 80f, -80f, 120f, -120f, 180f })
            {
                Vector3 hautagaia = transform.position + Quaternion.Euler(0f, angelua, 0f) * kanpora * 8f;
                if (NavMesh.SamplePosition(hautagaia, out NavMeshHit jo, 2f, NavMesh.AllAreas)
                    && _agentea.CalculatePath(jo.position, bidea) && bidea.status == NavMeshPathStatus.PathComplete
                    && Vector3.Distance(jo.position, arriskua) > Vector3.Distance(transform.position, arriskua))
                {
                    return jo.position;
                }
            }

            return transform.position;
        }

        /// <summary>Soldadua: munstroari tiro (hormek gelditzen dute). Inoiz ez jokalariari: helburua beti Etsaia bat da.</summary>
        private void Tiro()
        {
            if (_mehatxua == null || !_mehatxua.Bizirik)
            {
                return;
            }

            Vector3 jatorria = _kanoia != null ? _kanoia.position : transform.position + Vector3.up * 1.4f;
            Vector3 helburua = _mehatxua.transform.position + Vector3.up * 1.2f;
            Vector3 norabidea = (helburua - jatorria).normalized;
            Tiroak++;
            Vector3 inpaktua = helburua;
            if (Physics.Linecast(jatorria, helburua, out RaycastHit horma, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                inpaktua = horma.point;
            }
            else
            {
                Jotakoak++;
                _mehatxua.KalteaJaso(new KalteInfo(KalteZona.Enborra, _tiroKaltea, helburua, norabidea));
            }

            if (_tiroIzpia != null)
            {
                _tiroIzpia.SetPosition(0, jatorria);
                _tiroIzpia.SetPosition(1, inpaktua);
                _tiroIzpia.enabled = true;
                _izpiaItzali = Time.time + 0.06f;
            }

            if (_audioa != null && _audioa.clip != null)
            {
                _audioa.pitch = Random.Range(0.75f, 0.85f);
                _audioa.PlayOneShot(_audioa.clip, 0.7f);
            }
        }

        private void LateUpdate()
        {
            ArmaOrientatu();
            BuruaBiratu();
        }

        /// <summary>Fusila: Zaindu-n munstroari zuzen; bestela, behera eta aurrera (prest-jarrera).</summary>
        private void ArmaOrientatu()
        {
            if (_fusila == null)
            {
                return;
            }

            Vector3 norabidea = Egoera == NpcEgoera.Zaindu && _mehatxua != null
                ? _mehatxua.transform.position + Vector3.up * 1.2f - _fusila.position
                : Quaternion.AngleAxis(35f, transform.right) * transform.forward;
            if (norabidea.sqrMagnitude > 0.0001f)
            {
                _fusila.rotation = Quaternion.LookRotation(norabidea, Vector3.up);
            }
        }

        /// <summary>Burua jokalariari (Begiratu) edo munstroari (Zaindu) begira, animazioaren gainean eta mugatuta.</summary>
        private void BuruaBiratu()
        {
            if (_burua == null)
            {
                return;
            }

            Vector3? helburua = Egoera switch
            {
                NpcEgoera.Begiratu when _jokalaria != null => _jokalaria.position + Vector3.up * 1.6f,
                NpcEgoera.Zaindu when _mehatxua != null => _mehatxua.transform.position + Vector3.up * 1.3f,
                _ => null,
            };
            _buruPisua = Mathf.MoveTowards(_buruPisua, helburua.HasValue ? 1f : 0f, 3f * Time.deltaTime);
            if (_buruPisua <= 0f || !helburua.HasValue)
            {
                return;
            }

            Vector3 nahia = helburua.Value - _burua.position;
            Vector3 mugatua = Vector3.RotateTowards(transform.forward, nahia.normalized, BuruAngelua * Mathf.Deg2Rad, 0f);
            Quaternion aldea = Quaternion.FromToRotation(transform.forward, mugatua);
            _burua.rotation = Quaternion.Slerp(Quaternion.identity, aldea, _buruPisua) * _burua.rotation;
        }

        /// <summary>Munstro bizirik hurbilena, ikusten direnak lehenengo (hormaren atzekoak ez dute ikusgai dagoena ezkutatzen).</summary>
        private Etsaia MehatxuHurbilena() =>
            FindObjectsByType<Etsaia>(FindObjectsInactive.Exclude)
                .Where(e => e.Bizirik && Vector3.Distance(e.transform.position, transform.position) <= 30f)
                .OrderBy(e => Libre(e.transform.position) ? 0 : 1)
                .ThenBy(e => (e.transform.position - transform.position).sqrMagnitude)
                .FirstOrDefault();

        /// <summary>Begietatik bularrera hormarik ez (trigger-ak eta Ignore Raycast geruza baztertuta).</summary>
        private bool Libre(Vector3 lekua) =>
            !Physics.Linecast(transform.position + Vector3.up * 1.6f, lekua + Vector3.up * 1.2f, ~(1 << 2), QueryTriggerInteraction.Ignore);

        private static Vector3 Horizontala(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
