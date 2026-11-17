using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Desmenbratze modularra (A3, E blokea, prototipoa): Etsaia-ren ZatiaHautsita gertaerari entzuten dio. Aukerakoa: aurrefabrikatu
    /// bati gehitzen zaio (PF_Etsaia_Desmenbragarria), eta misioetako etsaiek ez dute.
    /// - Zati bereizia: ereduaren kopia (uneko pose), gorputzaren gainerakoa ebakidura-puntuan bilduta (eskala ~0). Ez du mallarik
    ///   irakurtzen (Read/Write gabe ere badabil, build-ean ere). Rigidbody + kaxa, tiroaren norabidean botata.
    /// - Etsaian: hezurra eskala ~0 (besaurrea / hanka-behea), haren kalte-zonak itzalita, odol-zurrusta ebakiduran eta orbana lurrean.
    /// - Ondorioak DesmenbratzeLogika-tik: hanka bat herren, biak narrastaka (Animator-eko "Narrastaka", badago), besoak erasoa ahultzen.
    /// - Aldi baterakoak: zatiak eta gorpua denbora baten ondoren hondoratu eta ezabatzen dira.
    /// </summary>
    [RequireComponent(typeof(Etsaia))]
    public sealed class Desmenbratzea : MonoBehaviour
    {
        private const string Marka = "ECL_DESMENBRATZEA";
        private const float MoztuEskala = 0.0001f;
        private static readonly int NarrastakaId = Animator.StringToHash("Narrastaka");

        [SerializeField] private GameObject _orbanAurrefab;
        [SerializeField] private float _hilotzIraupena = 20f;
        [SerializeField] private float _zatiIraupena = 15f;
        [SerializeField] private float _hondoratzeDenbora = 1.5f;

        private readonly DesmenbratzeLogika _logika = new DesmenbratzeLogika();
        private readonly List<Transform> _moztuak = new List<Transform>();
        private readonly List<GameObject> _zatiak = new List<GameObject>();
        private readonly List<GameObject> _orbanak = new List<GameObject>();
        private Etsaia _etsaia;

        public DesmenbratzeLogika Logika => _logika;

        /// <summary>Bereizitako zatiak (ezabatutakoak null izan daitezke).</summary>
        public IReadOnlyList<GameObject> Zatiak => _zatiak;

        public IReadOnlyList<GameObject> Orbanak => _orbanak;

        public IReadOnlyList<Transform> Moztuak => _moztuak;

        /// <summary>Gorpua ezabatu arte (s). Probetan laburtu daiteke.</summary>
        public float HilotzIraupena
        {
            get => _hilotzIraupena;
            set => _hilotzIraupena = Mathf.Max(0f, value);
        }

        public float ZatiIraupena
        {
            get => _zatiIraupena;
            set => _zatiIraupena = Mathf.Max(0f, value);
        }

        public event Action<KalteZona, GameObject> ZatiaBereizita;

        public void Konfiguratu(GameObject orbanAurrefab, float hilotzIraupena, float zatiIraupena)
        {
            _orbanAurrefab = orbanAurrefab;
            _hilotzIraupena = hilotzIraupena;
            _zatiIraupena = zatiIraupena;
        }

        private void Awake()
        {
            _etsaia = GetComponent<Etsaia>();
        }

        private void OnEnable()
        {
            _etsaia.ZatiaHautsita += Hautsita;
            _etsaia.Hildakoa += Hilda;
        }

        private void OnDisable()
        {
            _etsaia.ZatiaHautsita -= Hautsita;
            _etsaia.Hildakoa -= Hilda;
        }

        private void OnDestroy()
        {
            foreach (GameObject o in _zatiak.Concat(_orbanak).Where(o => o != null))
            {
                Destroy(o);
            }
        }

        /// <summary>Animazioaren ondoren: moztutako hezurrek eskala ~0 mantentzen dute.</summary>
        private void LateUpdate()
        {
            foreach (Transform h in _moztuak)
            {
                h.localScale = Vector3.one * MoztuEskala;
            }
        }

        public static HumanBodyBones EbakiHezurra(KalteZona zona) => zona switch
        {
            KalteZona.BesoEzkerra => HumanBodyBones.LeftLowerArm,
            KalteZona.BesoEskuina => HumanBodyBones.RightLowerArm,
            KalteZona.HankaEzkerra => HumanBodyBones.LeftLowerLeg,
            KalteZona.HankaEskuina => HumanBodyBones.RightLowerLeg,
            _ => throw new ArgumentOutOfRangeException(nameof(zona), zona, "Ez da bereizgarria"),
        };

        private void Hautsita(Etsaia etsaia, KalteZona zona)
        {
            Animator an = _etsaia.Animatzailea;
            if (an == null || !an.isHuman)
            {
                Debug.LogWarning($"{Marka}_ERROR {name}: Animator humanoiderik ez; {zona} ez da bereizten.");
                return;
            }

            if (!_logika.Galdu(zona))
            {
                return;
            }

            Transform hezurra = an.GetBoneTransform(EbakiHezurra(zona));
            Transform aldaka = an.GetBoneTransform(HumanBodyBones.Hips);
            if (hezurra == null || aldaka == null)
            {
                Debug.LogWarning($"{Marka}_ERROR {name}: {EbakiHezurra(zona)} edo Hips hezurra falta da.");
                return;
            }

            Vector3 norabidea = (hezurra.position - hezurra.parent.position).normalized;
            GameObject zatia = ZatiaSortu(an, hezurra, aldaka, zona);
            _zatiak.Add(zatia);
            StartCoroutine(Hondoratu(zatia, _zatiIraupena));

            foreach (Collider k in hezurra.GetComponentsInChildren<Collider>(true))
            {
                k.enabled = false;
            }

            _moztuak.Add(hezurra);
            hezurra.localScale = Vector3.one * MoztuEskala;
            Zurrusta(hezurra.parent, hezurra.position, norabidea, 3f, 35f);
            Orbana(hezurra.position, 0.5f);

            _etsaia.AbiaduraFaktorea = _logika.AbiaduraFaktorea;
            _etsaia.ErasoFaktorea = _logika.ErasoFaktorea;
            if (an.parameters.Any(p => p.nameHash == NarrastakaId))
            {
                an.SetBool(NarrastakaId, _logika.Narrastaka);
            }

            Debug.Log($"{Marka}_ZATIA {name} {zona}: abiadura x{_logika.AbiaduraFaktorea:0.##}, erasoa x{_logika.ErasoFaktorea:0.##}, narrastaka {_logika.Narrastaka}");
            ZatiaBereizita?.Invoke(zona, zatia);
        }

        /// <summary>Ereduaren kopia: SkinnedMeshRenderer-ak eta hezurrak bakarrik; gorputza ebakidura-puntuan bilduta.</summary>
        private GameObject ZatiaSortu(Animator an, Transform hezurra, Transform aldaka, KalteZona zona)
        {
            string hezurBidea = Bidea(an.transform, hezurra);
            string aldakaBidea = Bidea(an.transform, aldaka);
            GameObject kopia = Instantiate(an.gameObject, an.transform.position, an.transform.rotation);
            kopia.transform.localScale = an.transform.lossyScale;
            foreach (KalteZonaKolisioa k in kopia.GetComponentsInChildren<KalteZonaKolisioa>(true))
            {
                DestroyImmediate(k.gameObject);
            }

            foreach (Component c in kopia.GetComponentsInChildren<Component>(true)
                         .Where(c => !(c is Transform) && !(c is SkinnedMeshRenderer)).OrderBy(c => c is Behaviour ? 0 : 1).ToList())
            {
                DestroyImmediate(c);
            }

            Transform kHezurra = kopia.transform.Find(hezurBidea);
            Transform kAldaka = kopia.transform.Find(aldakaBidea);
            var erroa = new GameObject($"Zatia_{zona}") { layer = gameObject.layer };
            erroa.transform.SetPositionAndRotation(hezurra.position, hezurra.rotation);
            kopia.transform.SetParent(erroa.transform, true);
            kHezurra.SetParent(erroa.transform, true);
            // Gainerako gorputza (Hips-en azpiko guztia) ebakidura-puntuan bildu: mugako erpinek kapa itxi bat osatzen dute.
            kAldaka.localScale = Vector3.one * MoztuEskala;
            kAldaka.position = kHezurra.position;
            foreach (SkinnedMeshRenderer r in kopia.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.updateWhenOffscreen = true;
            }

            // Kolisioa: hezurraren ondorengoen kaxa (esku/oina barne).
            var muga = new Bounds(Vector3.zero, Vector3.one * 0.05f);
            foreach (Transform t in kHezurra.GetComponentsInChildren<Transform>())
            {
                muga.Encapsulate(erroa.transform.InverseTransformPoint(t.position));
            }

            muga.Expand(0.1f);
            var kaxa = erroa.AddComponent<BoxCollider>();
            kaxa.center = muga.center;
            kaxa.size = muga.size;
            foreach (Collider k in GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(kaxa, k);
            }

            var gorputza = erroa.AddComponent<Rigidbody>();
            gorputza.mass = 4f;
            gorputza.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Vector3 bultzada = _etsaia.AzkenKaltea.Norabidea;
            bultzada.y = 0f;
            gorputza.linearVelocity = bultzada.normalized * 2.5f + Vector3.up * 1.5f;
            gorputza.angularVelocity = new Vector3(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-6f, 6f));
            Zurrusta(erroa.transform, kHezurra.position, -(kHezurra.position - hezurra.parent.position).normalized, 1.5f, 20f);
            return erroa;
        }

        /// <summary>Odol-zurrusta ebakiduran: etsaiaren "Odola" partikula-sistemaren kopia, denbora batez isurtzen.</summary>
        private void Zurrusta(Transform gurasoa, Vector3 lekua, Vector3 norabidea, float iraupena, float maiztasuna)
        {
            Transform odola = transform.Find("Odola");
            if (odola == null)
            {
                return;
            }

            GameObject o = Instantiate(odola.gameObject, lekua, Quaternion.LookRotation(norabidea.sqrMagnitude > 0.001f ? norabidea : Vector3.up), gurasoa);
            o.name = "Zurrusta";
            var ps = o.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.duration = iraupena;
            main.loop = false;
            ParticleSystem.EmissionModule emisioa = ps.emission;
            emisioa.rateOverTime = maiztasuna;
            ps.Play();
        }

        /// <summary>Odol-orbana lurrean (atrezzoko pieza, eskalatuta: zati batek gorpu batek baino gutxiago).</summary>
        private void Orbana(Vector3 lekua, float eskala)
        {
            if (_orbanAurrefab == null)
            {
                return;
            }

            if (Physics.Raycast(lekua + Vector3.up * 0.2f, Vector3.down, out RaycastHit lurra, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                GameObject o = Instantiate(_orbanAurrefab, lurra.point + Vector3.up * 0.005f, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                o.name = "Odol_Orbana";
                o.transform.localScale *= eskala;
                _orbanak.Add(o);
            }
        }

        private void Hilda(Etsaia etsaia)
        {
            Orbana(transform.position + transform.forward * 0.6f, 0.8f);
            StartCoroutine(Hondoratu(gameObject, _hilotzIraupena));
        }

        /// <summary>Itxaron, lurrean behera hondoratu eta ezabatu (zatiak eta gorpua).</summary>
        private IEnumerator Hondoratu(GameObject o, float itxaron)
        {
            yield return new WaitForSeconds(itxaron);
            if (o == null)
            {
                yield break;
            }

            if (o.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }

            foreach (Collider k in o.GetComponentsInChildren<Collider>())
            {
                k.enabled = false;
            }

            float hasiera = Time.time;
            while (o != null && Time.time - hasiera < _hondoratzeDenbora)
            {
                o.transform.position += Vector3.down * (0.5f * Time.deltaTime);
                yield return null;
            }

            if (o == gameObject)
            {
                Debug.Log($"{Marka}_GORPUA_EZABATUTA {name}");
            }

            Destroy(o);
        }

        private static string Bidea(Transform erroa, Transform t)
        {
            var izenak = new List<string>();
            for (Transform u = t; u != null && u != erroa; u = u.parent)
            {
                izenak.Add(u.name);
            }

            izenak.Reverse();
            return string.Join("/", izenak);
        }
    }
}
