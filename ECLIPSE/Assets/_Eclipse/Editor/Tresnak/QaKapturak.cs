using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Proben kapturen helmuga: exekuzio bakoitzak bere karpeta du, Recordings/qa/&lt;data_ordua&gt;_&lt;pid&gt;/ (Git-ek ez du jarraitzen:
    /// /Recordings/ .gitignore-n dago, "kaptura gordinak"). Ez da inoiz aurreko kapturarik gainidazten: izena errepikatzen bada,
    /// atzizkia gehitzen da. Memoriarako aukeratutakoak eskuz kopiatzen dira docs/-era.
    /// </summary>
    internal static class QaKapturak
    {
        private static string _karpeta;

        public static string Erroa => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings", "qa"));

        /// <summary>Exekuzio honen karpeta (prozesu bakoitzeko bat; batch-ean, exekuzio bakoitzeko bat).</summary>
        public static string Karpeta
        {
            get
            {
                if (_karpeta == null)
                {
                    _karpeta = Path.Combine(Erroa, $"{DateTime.Now:yyyyMMdd_HHmmss}_{Process.GetCurrentProcess().Id}");
                    Directory.CreateDirectory(_karpeta);
                    UnityEngine.Debug.Log($"ECL_QAKAPTURAK {_karpeta}");
                }

                return _karpeta;
            }
        }

        /// <summary>Azpikarpeta bat exekuzio honen barruan (sortu egiten da).</summary>
        public static string Azpikarpeta(string izena)
        {
            string bidea = Path.Combine(Karpeta, izena);
            Directory.CreateDirectory(bidea);
            return bidea;
        }

        /// <summary>Fitxategi-bide librea: badago, _2, _3… atzizkiarekin. Ez du inoiz gainidazten.</summary>
        public static string BideLibrea(string karpeta, string izena)
        {
            Directory.CreateDirectory(karpeta);
            string oinarria = Path.GetFileNameWithoutExtension(izena);
            string luzapena = Path.GetExtension(izena);
            string bidea = Path.Combine(karpeta, izena);
            for (int i = 2; File.Exists(bidea); i++)
            {
                bidea = Path.Combine(karpeta, $"{oinarria}_{i}{luzapena}");
            }

            return bidea;
        }

        public static string Gorde(byte[] png, string karpeta, string izena)
        {
            string bidea = BideLibrea(karpeta, izena);
            File.WriteAllBytes(bidea, png);
            return bidea;
        }
    }
}
