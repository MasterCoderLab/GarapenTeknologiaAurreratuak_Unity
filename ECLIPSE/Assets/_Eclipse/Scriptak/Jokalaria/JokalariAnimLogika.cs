using UnityEngine;

namespace Eclipse.Jokalaria
{
    /// <summary>
    /// Jokalariaren egoera, geruza bisualak irakurtzeko bakarrik. Kontrolatzaileak (EsploratzaileKontrola) betetzen du;
    /// bisualak ez du ezer aldatzen: ez mugimendua, ez biraketa, ez kamera.
    /// </summary>
    public interface IJokalariEgoera
    {
        /// <summary>CharacterController-aren azken abiadura, munduan (m/s).</summary>
        Vector3 UnekoAbiadura { get; }

        bool Lurrean { get; }

        bool Makurtuta { get; }

        bool HirugarrenPertsona { get; }

        /// <summary>Begiradaren yaw-a (kontrolatzailearen erroa), gradutan.</summary>
        float BegiradaYaw { get; }
    }

    /// <summary>
    /// Eredu bisualaren orientazioa. A2: mugimenduaren norabidera (alboko klipik ez dago).
    /// A3: apuntatzean kamerara begira, eta alboko/atzerako klipak <see cref="AnimParametroak.AbiaduraX"/> eta
    /// <see cref="AnimParametroak.AbiaduraZ"/> parametroekin.
    /// </summary>
    public enum OrientazioModua
    {
        Mugimendua,
        Kamera,
    }

    /// <summary>Animator-erako balioak, fotograma bakoitzeko.</summary>
    public readonly struct AnimParametroak
    {
        public AnimParametroak(float abiadura, float abiaduraX, float abiaduraZ, bool makurtuta, bool airean)
        {
            Abiadura = abiadura;
            AbiaduraX = abiaduraX;
            AbiaduraZ = abiaduraZ;
            Makurtuta = makurtuta;
            Airean = airean;
        }

        /// <summary>Abiadura horizontala (m/s).</summary>
        public float Abiadura { get; }

        /// <summary>Abiadura horizontala ereduaren eskuinera (m/s); A3-ko alboko mugimendurako.</summary>
        public float AbiaduraX { get; }

        /// <summary>Abiadura horizontala ereduaren aurrera (m/s); negatiboa = atzera.</summary>
        public float AbiaduraZ { get; }

        public bool Makurtuta { get; }

        public bool Airean { get; }
    }

    /// <summary>
    /// Geruza bisualaren logika hutsa (MonoBehaviour gabe, EditMode testekin): ereduaren yaw-a eta Animator-aren parametroak.
    /// Ez du kontrolatzailearen biraketa ukitzen: eredu bisuala bakarrik biratzen da.
    /// </summary>
    public sealed class JokalariAnimLogika
    {
        /// <summary>Abiadura honen azpitik ez da mugitzen ari: ereduak bere norabidea gordetzen du.</summary>
        public const float MugimenduMuga = 0.2f;

        /// <summary>Ereduaren biraketa-abiadura maximoa (°/s): bat-bateko jauzirik gabe.</summary>
        public const float BiraAbiadura = 720f;

        /// <summary>Lurrik gabe denbora hau igaro arte ez da "airean" (eskaileretan eta maldetan dardararik ez).</summary>
        public const float AireMuga = 0.15f;

        /// <summary>Gorako abiadura honetatik aurrera jauzia da berehala, itxaron gabe.</summary>
        public const float JauziMuga = 1f;

        private float _yaw;
        private float _aireDenbora;

        public float Yaw => _yaw;

        public void Hasieratu(float yaw)
        {
            _yaw = yaw;
            _aireDenbora = 0f;
        }

        public AnimParametroak Eguneratu(Vector3 abiadura, bool lurrean, bool makurtuta, float begiradaYaw, OrientazioModua modua, float dt)
        {
            var horizontala = new Vector3(abiadura.x, 0f, abiadura.z);
            float bizkortasuna = horizontala.magnitude;

            float helburua = _yaw;
            if (modua == OrientazioModua.Kamera)
            {
                helburua = begiradaYaw;
            }
            else if (bizkortasuna > MugimenduMuga)
            {
                helburua = Mathf.Atan2(horizontala.x, horizontala.z) * Mathf.Rad2Deg;
            }

            _yaw = Mathf.Repeat(Mathf.MoveTowardsAngle(_yaw, helburua, BiraAbiadura * dt), 360f);

            _aireDenbora = lurrean ? 0f : _aireDenbora + dt;
            bool airean = !lurrean && (_aireDenbora > AireMuga || abiadura.y > JauziMuga);

            Vector3 lokala = Quaternion.Euler(0f, -_yaw, 0f) * horizontala;
            return new AnimParametroak(bizkortasuna, lokala.x, lokala.z, makurtuta, airean);
        }
    }
}
