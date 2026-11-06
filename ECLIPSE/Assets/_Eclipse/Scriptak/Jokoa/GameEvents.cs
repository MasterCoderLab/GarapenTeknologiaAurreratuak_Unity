using System;
using UnityEngine;

namespace Eclipse.Jokoa
{
    /// <summary>Sistemen arteko abisuak. Harpidetzak OnEnable-n eta bajak OnDisable-n; hemen ez da eszenako egoerarik gordetzen.</summary>
    public static class GameEvents
    {
        public static event Action<string> SektoreaHasi;
        public static event Action<string> HelburuaBeteta;
        public static event Action<string> SektoreaAmaitu;

        public static void JakinaraziSektoreaHasi(string izena) => SektoreaHasi?.Invoke(izena);

        public static void JakinaraziHelburuaBeteta(string id) => HelburuaBeteta?.Invoke(id);

        public static void JakinaraziSektoreaAmaitu(string izena) => SektoreaAmaitu?.Invoke(izena);

        /// <summary>Domain Reload desaktibatuta dago: Play bakoitzean harpidetza zaharrak garbitu.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Garbitu()
        {
            SektoreaHasi = null;
            HelburuaBeteta = null;
            SektoreaAmaitu = null;
        }
    }
}
