using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eclipse.EditorTresnak
{
    /// <summary>Tresna guztien oinarria: hautapena balidatu eta bideak idatzi.</summary>
    internal static class TresnaLaguntzailea
    {
        private static readonly Regex SektoreIzena = new Regex(@"^Sektorea[123]$");

        /// <summary>
        /// Hautapenak eszena irekiko sektore-erro bakarra izan behar du.
        /// Tresnek ez dute ezer ukitzen erro horretatik kanpo.
        /// </summary>
        public static bool LortuSektoreErroa(out GameObject erroa, out string errorea)
        {
            erroa = null;
            errorea = null;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                errorea = "Play moduan ezin da exekutatu.";
                return false;
            }

            GameObject[] hautapena = Selection.gameObjects;
            if (hautapena.Length != 1)
            {
                errorea = $"Hautatu sektore-erro bakarra (Sektorea1, Sektorea2 edo Sektorea3). Hautatuta: {hautapena.Length}.";
                return false;
            }

            GameObject hautatua = hautapena[0];
            bool eszenaIrekian = !EditorUtility.IsPersistent(hautatua)
                                 && hautatua.scene.IsValid()
                                 && hautatua.scene.isLoaded
                                 && !EditorSceneManager.IsPreviewScene(hautatua.scene);
            if (!eszenaIrekian)
            {
                errorea = $"'{hautatua.name}' ez dago eszena ireki batean (prefab moduan ezin da exekutatu).";
                return false;
            }

            if (hautatua.transform.parent != null || !SektoreIzena.IsMatch(hautatua.name))
            {
                errorea = $"'{hautatua.name}' ez da sektore-erroa: eszenaren erroan egon behar du eta Sektorea1, Sektorea2 edo Sektorea3 izena izan.";
                return false;
            }

            erroa = hautatua;
            return true;
        }

        /// <summary>Objektuaren bidea hierarkian, sektore-errotik hasita.</summary>
        public static string Bidea(Transform objektua, Transform erroa)
        {
            var bidea = new StringBuilder(objektua.name);
            for (Transform uneko = objektua.parent;
                 uneko != null && objektua != erroa;
                 uneko = uneko.parent)
            {
                bidea.Insert(0, uneko.name + "/");
                if (uneko == erroa)
                {
                    break;
                }
            }

            return bidea.ToString();
        }
    }

    /// <summary>Informe bat: docs/qa/tresnak/ karpetan idazten da eta marka bat uzten du log-ean.</summary>
    internal sealed class Informea
    {
        private readonly string _tresna;
        private readonly string _marka;
        private readonly string _modua;
        private readonly StringBuilder _edukia = new StringBuilder();

        public Informea(string tresna, string marka, string modua, GameObject erroa)
            : this(tresna, marka, modua, erroa.scene.path, erroa.name)
        {
        }

        public Informea(string tresna, string marka, string modua, string eszena, string esparrua)
        {
            _tresna = tresna;
            _marka = marka;
            _modua = modua;

            _edukia.AppendLine($"# {tresna}: {modua}");
            _edukia.AppendLine();
            _edukia.AppendLine($"- **Data:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _edukia.AppendLine($"- **Eszena:** {eszena}");
            _edukia.AppendLine($"- **Sektorea:** {esparrua}");
        }

        public void Lerroa(string testua)
        {
            _edukia.AppendLine(testua);
        }

        /// <summary>Atal bat zerrenda batekin; zerrenda hutsa bada, ez da ezer idazten.</summary>
        public void Atala(string izenburua, ICollection<string> lerroak)
        {
            if (lerroak.Count == 0)
            {
                return;
            }

            _edukia.AppendLine();
            _edukia.AppendLine($"## {izenburua} ({lerroak.Count})");
            _edukia.AppendLine();
            foreach (string lerroa in lerroak)
            {
                _edukia.AppendLine($"- {lerroa}");
            }
        }

        /// <summary>Informea idatzi eta <c>_OK</c> marka utzi; ezin bada idatzi, <c>_ERROR</c> marka.</summary>
        public void AmaituOndo(string laburpena)
        {
            string bidea = Idatzi();
            if (bidea != null)
            {
                Debug.Log($"{_marka}_OK {_tresna} ({_modua}): {laburpena}. Informea: {bidea}");
            }
        }

        /// <summary>Informea idatzi (ahal bada) eta <c>_ERROR</c> marka utzi.</summary>
        public void AmaituErrorearekin(string errorea)
        {
            Lerroa(string.Empty);
            Lerroa($"**ERROREA:** {errorea}");
            string bidea = Idatzi();
            Debug.LogError($"{_marka}_ERROR {_tresna} ({_modua}): {errorea}. Informea: {bidea ?? "ez da idatzi"}");
        }

        private string Idatzi()
        {
            try
            {
                string karpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "qa", "tresnak"));
                Directory.CreateDirectory(karpeta);
                string oinarria = $"{_tresna}_{DateTime.Now:yyyy-MM-dd_HHmmss}";
                string bidea = Path.Combine(karpeta, $"{oinarria}.md");
                for (int i = 2; File.Exists(bidea); i++)
                {
                    bidea = Path.Combine(karpeta, $"{oinarria}_{i}.md");
                }

                File.WriteAllText(bidea, _edukia.ToString());
                return bidea;
            }
            catch (Exception salbuespena) when (salbuespena is IOException || salbuespena is UnauthorizedAccessException)
            {
                Debug.LogError($"{_marka}_ERROR Ezin izan da informea idatzi: {salbuespena.Message}");
                return null;
            }
        }
    }
}
