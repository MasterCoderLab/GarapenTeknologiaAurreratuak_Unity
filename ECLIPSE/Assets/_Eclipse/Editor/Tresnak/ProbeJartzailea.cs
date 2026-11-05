using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Gela_* bakoitzeko (BoxCollider-ak markatzen du tamaina): Reflection Probe horneatua
    /// Box Projection-ekin eta Light Probe sarea. Izenez bilatzen du eta ez du bikoizten.
    /// </summary>
    internal static class ProbeJartzailea
    {
        private const string Tresna = "ProbeJartzailea";
        private const string Marka = "ECL_PROBEJARTZAILEA";
        private const string Menua = "ECLIPSE/Probeak/ProbeJartzailea: ";

        private const string GelaAurrizkia = "Gela_";
        private const string ProbeAurrizkia = "RP_";
        private const string TaldeAurrizkia = "LPG_";

        private const int ProbeBereizmena = 256;
        private const float SareTartea = 2f;
        private const float HormaMarjina = 0.3f;
        private const float SabaiMarjina = 0.1f;
        private const float Tolerantzia = 0.001f;
        private static readonly float[] Altuerak = { 0.5f, 1.7f };

        private sealed class GelaPlana
        {
            public Transform Gela;
            public Bounds Kaxa;
            public Vector3[] Puntuak;
            public Transform ProbeSemea;
            public ReflectionProbe Probe;
            public Transform TaldeSemea;
            public LightProbeGroup Taldea;
            public bool ProbeAldatu;
            public bool TaldeaAldatu;

            public string ProbeIzena => ProbeAurrizkia + Gela.name;
            public string TaldeIzena => TaldeAurrizkia + Gela.name;
            public int AldaketaKopurua => (ProbeAldatu ? 1 : 0) + (TaldeaAldatu ? 1 : 0);
        }

        private sealed class Analisia
        {
            public readonly List<GelaPlana> Planak = new List<GelaPlana>();
            public readonly List<string> Oharrak = new List<string>();
            public int Aldaketak;
        }

        [MenuItem(Menua + "Aurrebista")]
        private static void Aurrebista()
        {
            if (!TresnaLaguntzailea.LortuSektoreErroa(out GameObject erroa, out string errorea))
            {
                Debug.LogError($"{Marka}_ERROR {errorea}");
                return;
            }

            Analisia analisia = Analizatu(erroa);
            var informea = new Informea(Tresna, Marka, "Aurrebista", erroa);
            InformeaBete(informea, analisia, erroa.transform);
            informea.AmaituOndo($"{analisia.Aldaketak} aldaketa {analisia.Planak.Count} gelatan (ez da ezer aldatu)");
        }

        [MenuItem(Menua + "Aplikatu")]
        private static void Aplikatu()
        {
            if (!TresnaLaguntzailea.LortuSektoreErroa(out GameObject erroa, out string errorea))
            {
                Debug.LogError($"{Marka}_ERROR {errorea}");
                return;
            }

            Analisia analisia = Analizatu(erroa);
            var informea = new Informea(Tresna, Marka, "Aplikatu", erroa);
            InformeaBete(informea, analisia, erroa.transform);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("ECLIPSE ProbeJartzailea");
            int taldea = Undo.GetCurrentGroup();
            try
            {
                foreach (GelaPlana plana in analisia.Planak)
                {
                    if (plana.ProbeAldatu)
                    {
                        AplikatuSemea(plana.Gela, plana.ProbeSemea, plana.Probe, plana.ProbeIzena, plana.Kaxa.center,
                            (probe, berria) => KonfiguratuProbe(probe, plana.Kaxa, berria));
                    }

                    if (plana.TaldeaAldatu)
                    {
                        AplikatuSemea(plana.Gela, plana.TaldeSemea, plana.Taldea, plana.TaldeIzena, plana.Kaxa.center,
                            (probeTaldea, berria) => probeTaldea.probePositions = plana.Puntuak);
                    }
                }

                Undo.CollapseUndoOperations(taldea);
            }
            catch (Exception salbuespena)
            {
                Undo.RevertAllDownToGroup(taldea);
                informea.AmaituErrorearekin(salbuespena.Message);
                return;
            }

            if (analisia.Aldaketak > 0)
            {
                EditorSceneManager.MarkSceneDirty(erroa.scene);
            }

            // Egiaztapena: aplikatu ondoren analisiak 0 aldaketa eman behar du eta kaxak bat etorri.
            Analisia egiaztapena = Analizatu(erroa);
            List<string> kaxaHutsak = KaxaEgiaztatu(egiaztapena, erroa.transform);
            informea.Atala("Egiaztapena: probe-aren kaxa ez dator bat", kaxaHutsak);
            if (egiaztapena.Aldaketak > 0)
            {
                informea.AmaituErrorearekin($"Aplikatu ondoren {egiaztapena.Aldaketak} aldaketa geratzen dira: ez da idenpotentea gela horietan");
                return;
            }

            informea.AmaituOndo($"{analisia.Aldaketak} aldaketa {analisia.Planak.Count} gelatan");
        }

        private static Analisia Analizatu(GameObject erroa)
        {
            var analisia = new Analisia();
            Transform erroTransform = erroa.transform;

            foreach (Transform gela in erroa.GetComponentsInChildren<Transform>(true))
            {
                if (!gela.name.StartsWith(GelaAurrizkia, StringComparison.Ordinal))
                {
                    continue;
                }

                string bidea = TresnaLaguntzailea.Bidea(gela, erroTransform);
                var kolisionatzailea = gela.GetComponent<BoxCollider>();
                if (kolisionatzailea == null)
                {
                    analisia.Oharrak.Add($"{bidea}: ez du BoxCollider-ik; omitituta.");
                    continue;
                }

                bool ardatzetan = ArdatzetanLerrokatua(gela);
                if (!ardatzetan && !EskalaUniformea(gela))
                {
                    analisia.Oharrak.Add($"{bidea}: biraketa (90°-ren multiploa ez dena) eta eskala ez-uniformea batera; omitituta, zuzendu eskala.");
                    continue;
                }

                Bounds kaxa = MunduKaxa(gela, kolisionatzailea);
                if (kaxa.size.x <= Tolerantzia || kaxa.size.y <= Tolerantzia || kaxa.size.z <= Tolerantzia)
                {
                    analisia.Oharrak.Add($"{bidea}: kaxa hutsa ({kaxa.size}); omitituta.");
                    continue;
                }

                if (!ardatzetan)
                {
                    analisia.Oharrak.Add($"{bidea}: biraketa ez da 90°-ren multiploa. Probe-aren kaxa munduko ardatzetan lerrokatuta doa, beraz sala baino handiagoa izango da.");
                }

                if (kolisionatzailea.enabled && !kolisionatzailea.isTrigger)
                {
                    analisia.Oharrak.Add($"{bidea}: BoxCollider-a aktibo dago eta ez da trigger: kapsula blokeatuko du. Markatu trigger gisa edo desaktibatu.");
                }

                Vector3[] puntuak = SareaKalkulatu(kaxa.size, bidea, analisia.Oharrak);
                if (puntuak.Length == 0)
                {
                    analisia.Oharrak.Add($"{bidea}: altuera ({kaxa.size.y:0.##} m) txikiegia da Light Probe-entzat; omitituta.");
                    continue;
                }

                GelaPlana plana = PlanaEraiki(gela, kaxa, puntuak);
                analisia.Planak.Add(plana);
                analisia.Aldaketak += plana.AldaketaKopurua;
            }

            if (analisia.Planak.Count == 0)
            {
                analisia.Oharrak.Add("Ez da gela baliodunik aurkitu sektore honetan (Gela_* + BoxCollider).");
            }

            return analisia;
        }

        private static GelaPlana PlanaEraiki(Transform gela, Bounds kaxa, Vector3[] puntuak)
        {
            var plana = new GelaPlana { Gela = gela, Kaxa = kaxa, Puntuak = puntuak };

            plana.ProbeSemea = SemeaBilatu(gela, plana.ProbeIzena);
            plana.Probe = plana.ProbeSemea != null ? plana.ProbeSemea.GetComponent<ReflectionProbe>() : null;
            plana.ProbeAldatu = plana.Probe == null
                                || !TransformZuzena(plana.ProbeSemea, kaxa.center)
                                || plana.Probe.mode != ReflectionProbeMode.Baked
                                || !plana.Probe.boxProjection
                                || !Berdin(plana.Probe.center, Vector3.zero)
                                || !Berdin(plana.Probe.size, kaxa.size);

            plana.TaldeSemea = SemeaBilatu(gela, plana.TaldeIzena);
            plana.Taldea = plana.TaldeSemea != null ? plana.TaldeSemea.GetComponent<LightProbeGroup>() : null;
            plana.TaldeaAldatu = plana.Taldea == null
                                 || !TransformZuzena(plana.TaldeSemea, kaxa.center)
                                 || !PuntuakBerdinak(plana.Taldea.probePositions, puntuak);

            return plana;
        }

        /// <summary>
        /// Light Probe sarea, kaxaren erdiarekiko: tarte finkoa, zentratua, hormetatik urrun.
        /// Oinarria kaxaren behealdea da (zorua).
        /// </summary>
        private static Vector3[] SareaKalkulatu(Vector3 tamaina, string bidea, List<string> oharrak)
        {
            float[] xOffsetak = ArdatzekoOffsetak(tamaina.x);
            float[] zOffsetak = ArdatzekoOffsetak(tamaina.z);
            var puntuak = new List<Vector3>();

            foreach (float altuera in Altuerak)
            {
                if (altuera > tamaina.y - SabaiMarjina)
                {
                    oharrak.Add($"{bidea}: {altuera:0.0} m-ko maila ez da sartzen ({tamaina.y:0.##} m-ko altuera); omitituta.");
                    continue;
                }

                float y = altuera - tamaina.y * 0.5f;
                foreach (float x in xOffsetak)
                {
                    foreach (float z in zOffsetak)
                    {
                        puntuak.Add(new Vector3(x, y, z));
                    }
                }
            }

            return puntuak.ToArray();
        }

        private static float[] ArdatzekoOffsetak(float tamaina)
        {
            float erabilgarria = tamaina - 2f * HormaMarjina;
            int kopurua = erabilgarria <= 0f ? 1 : Mathf.FloorToInt(erabilgarria / SareTartea + 0.0001f) + 1;

            var offsetak = new float[kopurua];
            float hasiera = -(kopurua - 1) * SareTartea * 0.5f;
            for (int i = 0; i < kopurua; i++)
            {
                offsetak[i] = hasiera + i * SareTartea;
            }

            return offsetak;
        }

        /// <summary>BoxCollider-aren kaxa munduan. Ez du <c>bounds</c> erabiltzen: hutsa da kolisionatzailea desaktibatuta badago.</summary>
        private static Bounds MunduKaxa(Transform gela, BoxCollider kolisionatzailea)
        {
            Vector3 erdia = kolisionatzailea.center;
            Vector3 e = kolisionatzailea.size * 0.5f;
            var kaxa = new Bounds(gela.TransformPoint(erdia + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(-e.x, -e.y, e.z)));
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(-e.x, e.y, -e.z)));
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(-e.x, e.y, e.z)));
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(e.x, -e.y, -e.z)));
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(e.x, -e.y, e.z)));
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(e.x, e.y, -e.z)));
            kaxa.Encapsulate(gela.TransformPoint(erdia + new Vector3(e.x, e.y, e.z)));
            return kaxa;
        }

        /// <summary>Semea sortu edo eguneratu, Undo talde berean.</summary>
        private static void AplikatuSemea<T>(Transform gela, Transform semea, T osagaia, string izena, Vector3 posizioa,
            Action<T, bool> konfiguratu) where T : Component
        {
            if (semea == null)
            {
                var objektua = new GameObject(izena);
                objektua.transform.SetParent(gela, false);
                MunduanLerrokatu(objektua.transform, posizioa);
                konfiguratu(objektua.AddComponent<T>(), true);
                Undo.RegisterCreatedObjectUndo(objektua, izena);
                return;
            }

            Undo.RecordObject(semea, izena);
            MunduanLerrokatu(semea, posizioa);

            bool berria = osagaia == null;
            T helburua = berria ? Undo.AddComponent<T>(semea.gameObject) : osagaia;
            Undo.RecordObject(helburua, izena);
            konfiguratu(helburua, berria);

            if (PrefabUtility.IsPartOfPrefabInstance(semea))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(semea);
                PrefabUtility.RecordPrefabInstancePropertyModifications(helburua);
            }
        }

        private static void KonfiguratuProbe(ReflectionProbe probe, Bounds kaxa, bool berria)
        {
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.center = Vector3.zero;
            probe.size = kaxa.size;
            if (berria)
            {
                // Bereizmena sortzean bakarrik: gero Ion-ek eskuz doitu dezake.
                probe.resolution = ProbeBereizmena;
            }
        }

        /// <summary>Semea munduan: posizio hau, biraketarik gabe eta eskala 1, gurasoa edozein dela ere.</summary>
        private static void MunduanLerrokatu(Transform semea, Vector3 posizioa)
        {
            Transform gurasoa = semea.parent;
            Quaternion biraketa = Quaternion.Inverse(gurasoa.rotation);
            semea.localRotation = biraketa;
            semea.localScale = new Vector3(
                1f / Vector3.Dot(gurasoa.TransformVector(biraketa * Vector3.right), Vector3.right),
                1f / Vector3.Dot(gurasoa.TransformVector(biraketa * Vector3.up), Vector3.up),
                1f / Vector3.Dot(gurasoa.TransformVector(biraketa * Vector3.forward), Vector3.forward));
            semea.position = posizioa;
        }

        private static bool TransformZuzena(Transform semea, Vector3 posizioa)
        {
            return Berdin(semea.position, posizioa)
                   && Berdin(semea.lossyScale, Vector3.one)
                   && Quaternion.Angle(semea.rotation, Quaternion.identity) < 0.1f;
        }

        private static Transform SemeaBilatu(Transform gurasoa, string izena)
        {
            for (int i = 0; i < gurasoa.childCount; i++)
            {
                Transform semea = gurasoa.GetChild(i);
                if (semea.name == izena)
                {
                    return semea;
                }
            }

            return null;
        }

        private static bool ArdatzetanLerrokatua(Transform transformazioa)
        {
            return ArdatzBatean(transformazioa.right)
                   && ArdatzBatean(transformazioa.up)
                   && ArdatzBatean(transformazioa.forward);
        }

        private static bool ArdatzBatean(Vector3 norabidea)
        {
            return Mathf.Max(Mathf.Abs(norabidea.x), Mathf.Abs(norabidea.y), Mathf.Abs(norabidea.z)) > 0.9999f;
        }

        private static bool EskalaUniformea(Transform transformazioa)
        {
            Vector3 eskala = transformazioa.lossyScale;
            return Mathf.Abs(Mathf.Abs(eskala.x) - Mathf.Abs(eskala.y)) < Tolerantzia
                   && Mathf.Abs(Mathf.Abs(eskala.y) - Mathf.Abs(eskala.z)) < Tolerantzia;
        }

        private static bool Berdin(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < Tolerantzia * Tolerantzia;
        }

        private static bool PuntuakBerdinak(Vector3[] a, Vector3[] b)
        {
            if (a == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (!Berdin(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> KaxaEgiaztatu(Analisia analisia, Transform erroa)
        {
            var hutsak = new List<string>();
            foreach (GelaPlana plana in analisia.Planak)
            {
                if (plana.Probe == null)
                {
                    continue;
                }

                Bounds benetakoa = plana.Probe.bounds;
                if (!Berdin(benetakoa.center, plana.Kaxa.center) || !Berdin(benetakoa.size, plana.Kaxa.size))
                {
                    hutsak.Add($"{TresnaLaguntzailea.Bidea(plana.Gela, erroa)}: probe {benetakoa} / kaxa {plana.Kaxa}");
                }
            }

            return hutsak;
        }

        private static void InformeaBete(Informea informea, Analisia analisia, Transform erroa)
        {
            var gelak = new List<string>();
            foreach (GelaPlana plana in analisia.Planak)
            {
                string probeEgoera = Egoera(plana.ProbeAldatu, plana.ProbeSemea != null && plana.Probe != null);
                string taldeEgoera = Egoera(plana.TaldeaAldatu, plana.TaldeSemea != null && plana.Taldea != null);
                gelak.Add($"{TresnaLaguntzailea.Bidea(plana.Gela, erroa)}: kaxa {plana.Kaxa.size}; "
                          + $"{plana.ProbeIzena} {probeEgoera}; {plana.TaldeIzena} {taldeEgoera} ({plana.Puntuak.Length} probe)");
            }

            informea.Lerroa($"- **Gelak:** {analisia.Planak.Count}");
            informea.Lerroa($"- **Aldaketak:** {analisia.Aldaketak}");
            informea.Atala("Gelak", gelak);
            informea.Atala("Oharrak", analisia.Oharrak);
        }

        private static string Egoera(bool aldatu, bool badago)
        {
            if (!aldatu)
            {
                return "aldaketarik gabe";
            }

            return badago ? "eguneratu" : "sortu";
        }
    }
}
