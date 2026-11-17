using UnityEngine;
using UnityEngine.Rendering;

namespace Eclipse.Jokalaria
{
    /// <summary>
    /// Protagonistaren geruza bisuala (HypJo v2 + Animator), kontrolatzailearen umea. Egoera irakurri eta Animator-a elikatzen du;
    /// eredua bakarrik biratzen du. Ez du kolisiorik, ez CharacterController-ik, ez root motion-ik.
    /// Aurrefabrikatua Resources-etik kargatzen da: misioetako eszenak ez dira aldatzen.
    /// </summary>
    public sealed class JokalariBisuala : MonoBehaviour
    {
        /// <summary>Resources.Load-en bidea: Assets/Resources/Jokalaria/PF_Jokalaria_HypJo.prefab.</summary>
        public const string BaliabideBidea = "Jokalaria/PF_Jokalaria_HypJo";

        private const string Marka = "ECL_JOKALARIA_BISUALA";
        private const float Moteltzea = 0.1f;

        private static readonly int AbiaduraId = Animator.StringToHash("Abiadura");
        private static readonly int AbiaduraXId = Animator.StringToHash("AbiaduraX");
        private static readonly int AbiaduraZId = Animator.StringToHash("AbiaduraZ");
        private static readonly int MakurtutaId = Animator.StringToHash("Makurtuta");
        private static readonly int AireanId = Animator.StringToHash("Airean");

        [SerializeField] private Animator _animator;

        private readonly JokalariAnimLogika _logika = new JokalariAnimLogika();
        private IJokalariEgoera _egoera;
        private Renderer[] _errendatzaileak;
        private bool _hirugarrena;
        private bool _kameraHurbil;

        /// <summary>A2: mugimendura. A3-k apuntatzean <see cref="OrientazioModua.Kamera"/> jarriko du.</summary>
        public OrientazioModua Modua { get; set; } = OrientazioModua.Mugimendua;

        /// <summary>Eredua kameran ikusten da (hirugarren pertsona eta kamera ez dago buruaren ondoan).</summary>
        public bool Ikusgai => _hirugarrena && !_kameraHurbil;

        public Animator Animatzailea => _animator;

        public AnimParametroak AzkenParametroak { get; private set; }

        /// <summary>
        /// Aurrefabrikatua kargatu eta gurasoaren azpian jarri. Ezin bada, null itzultzen du eta deitzaileak kapsula erabiltzen du.
        /// </summary>
        public static JokalariBisuala Sortu(Transform gurasoa, IJokalariEgoera egoera)
        {
            GameObject aurrefabrikatua = Resources.Load<GameObject>(BaliabideBidea);
            if (aurrefabrikatua == null)
            {
                Debug.LogWarning($"{Marka}_ERROR Ez dago Resources/{BaliabideBidea}: kapsula erabiliko da.");
                return null;
            }

            GameObject objektua = Instantiate(aurrefabrikatua, gurasoa, false);
            objektua.name = aurrefabrikatua.name;
            var bisuala = objektua.GetComponent<JokalariBisuala>();
            if (bisuala == null || bisuala._animator == null || bisuala._animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"{Marka}_ERROR {aurrefabrikatua.name}: JokalariBisuala, Animator edo kontrolagailua falta da; kapsula erabiliko da.");
                Destroy(objektua);
                return null;
            }

            bisuala.Konfiguratu(gurasoa, egoera);
            Debug.Log($"{Marka}_OK {aurrefabrikatua.name} kargatuta: {bisuala._errendatzaileak.Length} errendatzaile, geruza {gurasoa.gameObject.layer}.");
            return bisuala;
        }

        /// <summary>Lehen pertsonan itzalak bakarrik; hirugarrenean, osorik.</summary>
        public void PertsonaModua(bool hirugarrena)
        {
            _hirugarrena = hirugarrena;
            IkusgaitasunaAplikatu();
        }

        /// <summary>Kamera buruaren ondoan dago (horma baten kontra): eredua ezkutatu, kamera haren barruan egon ez dadin.</summary>
        public void KameraHurbil(bool hurbil)
        {
            if (_kameraHurbil != hurbil)
            {
                _kameraHurbil = hurbil;
                IkusgaitasunaAplikatu();
            }
        }

        /// <summary>Umeak gehitu ondoren (adib. arma): malla-errendatzaileen zerrenda berritu eta ikusgaitasuna aplikatu.
        /// Izpiak eta partikulak ez dira sartzen: lehen pertsonan ere ikusi behar dira.</summary>
        public void ErrendatzaileakEguneratu()
        {
            _errendatzaileak = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r is MeshRenderer || r is SkinnedMeshRenderer);
            IkusgaitasunaAplikatu();
        }

        private void Konfiguratu(Transform gurasoa, IJokalariEgoera egoera)
        {
            _egoera = egoera;
            _errendatzaileak = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r is MeshRenderer || r is SkinnedMeshRenderer);
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = gurasoa.gameObject.layer;
            }

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // CharacterController-a skinWidth-aren altueran geratzen da lurraren gainean (0,05 m): eredua hainbeste jaitsi, oinak lurrean egon daitezen.
            var kontrolagailua = gurasoa.GetComponent<CharacterController>();
            float behera = kontrolagailua != null ? kontrolagailua.skinWidth : 0f;
            transform.SetLocalPositionAndRotation(Vector3.down * behera, Quaternion.identity);
            _logika.Hasieratu(gurasoa.eulerAngles.y);
            IkusgaitasunaAplikatu();
        }

        private void IkusgaitasunaAplikatu()
        {
            if (_errendatzaileak == null)
            {
                return;
            }

            ShadowCastingMode modua = Ikusgai ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            foreach (Renderer errendatzailea in _errendatzaileak)
            {
                errendatzailea.shadowCastingMode = modua;
            }
        }

        private void LateUpdate()
        {
            if (_egoera == null)
            {
                return;
            }

            // Lehen pertsonan itzala begiradarekin batera; hirugarrenean, aukeratutako moduan.
            OrientazioModua modua = _egoera.HirugarrenPertsona ? Modua : OrientazioModua.Kamera;
            float dt = Time.deltaTime;
            AnimParametroak p = _logika.Eguneratu(_egoera.UnekoAbiadura, _egoera.Lurrean, _egoera.Makurtuta, _egoera.BegiradaYaw, modua, dt);
            AzkenParametroak = p;

            transform.rotation = Quaternion.Euler(0f, _logika.Yaw, 0f);
            _animator.SetFloat(AbiaduraId, p.Abiadura, Moteltzea, dt);
            _animator.SetFloat(AbiaduraXId, p.AbiaduraX, Moteltzea, dt);
            _animator.SetFloat(AbiaduraZId, p.AbiaduraZ, Moteltzea, dt);
            _animator.SetBool(MakurtutaId, p.Makurtuta);
            _animator.SetBool(AireanId, p.Airean);
        }
    }
}
