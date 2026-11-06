using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Misioak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Mailen proba automatikoak: kapsularen uholdea (jokalaria irits daitekeen puntu guztiak), zuloak eta ihesak,
    /// misioaren faseak blokeoekin, gela guztiak eskuragarri, eta estatistikak.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const float UholdeUrratsa = 0.5f;
        private const float KapsulaErradioa = 0.3f;
        private const float MakurAltuera = 1.1f;
        private const float MakurIkuspegia = 1.5f;
        private const float MailaIgoera = 0.3f;
        private const int UholdeGehienez = 600000;
        private const int UholdeAzpiurratsak = 5;
        private const float OinErradioa = 0.25f;

        private static readonly (int X, int Z)[] Auzokoak =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        private sealed class UholdeNodo
        {
            public int X;
            public int Z;
            public float Y;
            public UholdeNodo Gurasoa;
            public Vector3 Posizioa => new Vector3(X * UholdeUrratsa, Y, Z * UholdeUrratsa);
        }

        private sealed class Uholdea
        {
            public readonly List<UholdeNodo> Nodoak = new List<UholdeNodo>();
            public readonly HashSet<string> Zuloak = new HashSet<string>();
            public readonly HashSet<string> Ihesak = new HashSet<string>();
        }

        private static bool KapsulaLibre(Vector3 oinak)
        {
            // Jokalaria makur daiteke: oztopo baxuen azpitik pasatzen dena eskuragarri dago.
            // Oinen tartea (0,3 m) libre: CharacterController-ak maila hori igotzen du, eta mailadietan hurrengo mailak ez du trabatzen.
            // Kapsularen behealdea biribila da eta oztopo baxuetara gehiegi hurbilduko litzateke: xafla laua gehitzen da oinen mailan.
            return !Physics.CheckCapsule(
                    oinak + Vector3.up * (MailaIgoera + KapsulaErradioa + 0.02f),
                    oinak + Vector3.up * (MakurAltuera - KapsulaErradioa + 0.05f),
                    KapsulaErradioa, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !Physics.CheckBox(
                    oinak + Vector3.up * (MailaIgoera + 0.06f),
                    new Vector3(0.3f, 0.04f, 0.3f),
                    Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Aurrean zutik kabitzen ez bada, robotak makurtu behar du. Mailadietan ere makurtzen da batzuetan: ez du ibilbidea aldatzen.</summary>
        internal static bool RobotakMakurtuBehar(Vector3 oinak, Vector3 norabidea)
        {
            for (float d = 0f; d <= MakurIkuspegia; d += 0.25f)
            {
                if (!Proba.EsploratzaileKontrola.ZutikLibre(oinak + norabidea * d))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Jokalariak hasieratik irits daitekeen guztia: 0,5 m-ko sarea, 0,3 m-ko maila igo, erortzeak onartuta (norabide bakarrean).</summary>
        private static Uholdea UholdeaEgin(Vector3 hasiera, List<Bounds> aztarna)
        {
            var uholdea = new Uholdea();
            var ikusiak = new HashSet<(int, int, int)>();
            var ilara = new Queue<UholdeNodo>();

            if (!Physics.Raycast(hasiera + Vector3.up * 0.5f, Vector3.down, out RaycastHit lehena, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return uholdea;
            }

            var erroa = new UholdeNodo { X = Mathf.RoundToInt(hasiera.x / UholdeUrratsa), Z = Mathf.RoundToInt(hasiera.z / UholdeUrratsa), Y = lehena.point.y };
            ikusiak.Add((erroa.X, erroa.Z, Mathf.RoundToInt(erroa.Y * 10f)));
            ilara.Enqueue(erroa);

            while (ilara.Count > 0 && uholdea.Nodoak.Count < UholdeGehienez)
            {
                UholdeNodo u = ilara.Dequeue();
                uholdea.Nodoak.Add(u);
                foreach ((int dx, int dz) in Auzokoak)
                {
                    int x = u.X + dx;
                    int z = u.Z + dz;
                    var goian = new Vector3(x * UholdeUrratsa, u.Y, z * UholdeUrratsa);

                    // Azpiurratsetan ibili: mailadi bateko maila bakoitza banan-banan igo behar da, ez sarearen jauzi osoan.
                    float y = u.Y;
                    bool ondo = true;
                    for (int k = 1; k <= UholdeAzpiurratsak && ondo; k++)
                    {
                        Vector3 p = Vector3.Lerp(u.Posizioa, goian, (float)k / UholdeAzpiurratsak);
                        p.y = y;
                        // CharacterController-a kapsularen azpian dagoen azalik altuenean dago, ez erdiko puntuaren azpikoan.
                        if (!Physics.SphereCast(p + Vector3.up * (MailaIgoera + 0.05f + OinErradioa), OinErradioa, Vector3.down, out RaycastHit jo, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            if (uholdea.Zuloak.Count < 200)
                            {
                                uholdea.Zuloak.Add(V(p));
                            }

                            ondo = false;
                            break;
                        }

                        if (jo.point.y - y > MailaIgoera)
                        {
                            ondo = false;
                            break;
                        }

                        y = jo.point.y;
                        if (k % 2 == 1 && !KapsulaLibre(new Vector3(p.x, y, p.z)))
                        {
                            ondo = false;
                        }
                    }

                    if (!ondo)
                    {
                        continue;
                    }

                    var berria = new Vector3(goian.x, y, goian.z);
                    if (!KapsulaLibre(berria))
                    {
                        continue;
                    }

                    if (!ikusiak.Add((x, z, Mathf.RoundToInt(y * 10f))))
                    {
                        continue;
                    }

                    if (aztarna != null && !aztarna.Any(b => Barruan(b, berria)) && uholdea.Ihesak.Count < 200)
                    {
                        uholdea.Ihesak.Add(V(berria));
                    }

                    ilara.Enqueue(new UholdeNodo { X = x, Z = z, Y = y, Gurasoa = u });
                }
            }

            return uholdea;
        }

        private static bool Barruan(Bounds b, Vector3 p)
        {
            return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
        }

        private static UholdeNodo Hurbilena(Uholdea uholdea, Vector3 puntua, float gehienez)
        {
            UholdeNodo onena = null;
            float distantzia = gehienez * gehienez;
            foreach (UholdeNodo u in uholdea.Nodoak)
            {
                float d = (u.Posizioa - puntua).sqrMagnitude;
                if (d < distantzia)
                {
                    distantzia = d;
                    onena = u;
                }
            }

            return onena;
        }

        /// <summary>Misioaren ibilbidea, faseka: helburu bakoitzera uholde berri batekin, faseari dagozkion blokeoak bakarrik itxita.</summary>
        private static List<RobotZatia> MisioIbilbidea(MailaDef d, List<Bounds> aztarna, List<string> hutsak, List<string> lerroak)
        {
            var zatiak = new List<RobotZatia>();
            GameObject erroa = SceneManager.GetActiveScene().GetRootGameObjects().First(o => o.name == d.Erroa);
            Transform misioa = SemeaBilatu(erroa.transform, MisioaIzena);
            MisioTerminala[] terminalak = misioa.GetComponentsInChildren<MisioTerminala>(true);
            BlokeoAtea[] blokeoak = misioa.GetComponentsInChildren<BlokeoAtea>(true);
            Vector3 uneko = SemeaBilatu(misioa, "Spawn_Jokalaria").position;
            Vector3 irteera = misioa.GetComponentInChildren<MisioIrteera>(true).transform.position;

            void BlokeoakEzarri(int fasea)
            {
                foreach (BlokeoAtea b in blokeoak)
                {
                    b.gameObject.SetActive(b.Fasea > fasea);
                }

                Physics.SyncTransforms();
            }

            try
            {
                for (int f = 0; f < d.Misioa.Faseak.Length; f++)
                {
                    BlokeoakEzarri(f);
                    foreach (string id in d.Misioa.Faseak[f].Helburuak)
                    {
                        MisioTerminala terminala = terminalak.First(t => t.Helburua == id);
                        Uholdea uholdea = UholdeaEgin(uneko, aztarna);
                        if (uholdea.Zuloak.Count + uholdea.Ihesak.Count > 0)
                        {
                            hutsak.Add($"Fasea {f}: {uholdea.Zuloak.Count} zulo eta {uholdea.Ihesak.Count} ihes. Adibidea: {uholdea.Zuloak.Concat(uholdea.Ihesak).First()}");
                        }

                        BoxCollider trigger = terminala.GetComponent<BoxCollider>();
                        Bounds kaxa = trigger.bounds;
                        kaxa.Expand(-0.5f);
                        UholdeNodo helmuga = null;
                        float onena = float.MaxValue;
                        foreach (UholdeNodo u in uholdea.Nodoak)
                        {
                            float dist = (u.Posizioa - terminala.transform.position).sqrMagnitude;
                            if (dist < onena && kaxa.Contains(u.Posizioa + Vector3.up))
                            {
                                onena = dist;
                                helmuga = u;
                            }
                        }

                        if (helmuga == null)
                        {
                            hutsak.Add($"Fasea {f}: ezin da {id} terminalera iritsi ({uholdea.Nodoak.Count} nodotik). Azken abiapuntua {V(uneko)}");
                            return zatiak;
                        }

                        List<Vector3> bidea = BideaLortu(helmuga);
                        zatiak.Add(new RobotZatia { Puntuak = bidea, Helburua = id });
                        lerroak.Add($"Fasea {f}, {id}: {Luzera(bidea):0} m, {uholdea.Nodoak.Count} nodo eskuragarri");
                        uneko = helmuga.Posizioa;
                    }

                    // Blokeoak benetan blokeatzen duela: azken fasea amaitu arte irteerara ezin da iritsi.
                    if (blokeoak.Any(b => b.Fasea > f))
                    {
                        Uholdea itxita = UholdeaEgin(uneko, aztarna);
                        if (Hurbilena(itxita, irteera, 2f) != null)
                        {
                            hutsak.Add($"Fasea {f}: irteerara iristen da blokeoa ireki gabe.");
                        }
                    }
                }

                BlokeoakEzarri(int.MaxValue);
                Uholdea azkena = UholdeaEgin(uneko, aztarna);
                UholdeNodo amaiera = Hurbilena(azkena, irteera, 2f);
                if (amaiera == null)
                {
                    hutsak.Add("Misioa amaituta ere ezin da irteerara iritsi.");
                    return zatiak;
                }

                List<Vector3> irteeraBidea = BideaLortu(amaiera);
                zatiak.Add(new RobotZatia { Puntuak = irteeraBidea });
                lerroak.Add($"Irteera: {Luzera(irteeraBidea):0} m");
            }
            finally
            {
                foreach (BlokeoAtea b in blokeoak)
                {
                    b.gameObject.SetActive(true);
                }

                Physics.SyncTransforms();
            }

            return zatiak;
        }

        private static List<Vector3> BideaLortu(UholdeNodo helmuga)
        {
            var bidea = new List<Vector3>();
            for (UholdeNodo u = helmuga; u != null; u = u.Gurasoa)
            {
                bidea.Add(u.Posizioa);
            }

            bidea.Reverse();
            return bidea;
        }

        private static float Luzera(List<Vector3> puntuak)
        {
            float guztira = 0f;
            for (int i = 1; i < puntuak.Count; i++)
            {
                guztira += Vector3.Distance(puntuak[i - 1], puntuak[i]);
            }

            return guztira;
        }

        private static bool MailaProbatu(MailaDef d)
        {
            Physics.SyncTransforms();
            var informea = new Informea(Tresna, Marka, $"{d.Izena}: Probak", d.Bidea, d.Erroa);
            var hutsak = new List<string>();
            var ibilbidea = new List<string>();
            Scene eszena = SceneManager.GetActiveScene();
            GameObject erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
            Transform misioa = SemeaBilatu(erroa.transform, MisioaIzena);

            // Prefab eta material erreferentziak.
            int prefabGalduak = 0;
            int materialGalduak = 0;
            int scriptGalduak = 0;
            foreach (Transform t in erroa.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsPrefabAssetMissing(t.gameObject))
                {
                    prefabGalduak++;
                }

                scriptGalduak += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                Renderer r = t.GetComponent<Renderer>();
                if (r != null && r.sharedMaterials.Any(mat => mat == null))
                {
                    materialGalduak++;
                }
            }

            if (prefabGalduak + materialGalduak + scriptGalduak > 0)
            {
                hutsak.Add($"Erreferentzia galduak: {prefabGalduak} prefab, {materialGalduak} material, {scriptGalduak} script.");
            }

            // Gela guztiak, aztarna eta sabaia.
            BoxCollider[] gelak = erroa.GetComponentsInChildren<BoxCollider>(true).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)).ToArray();
            List<Bounds> aztarna = gelak.Select(k =>
            {
                Bounds b = k.bounds;
                b.Expand(new Vector3(1.6f, 0f, 1.6f));
                return b;
            }).ToList();

            foreach (BoxCollider gela in gelak)
            {
                Vector3 p = gela.bounds.center;
                if (!Physics.Raycast(new Vector3(p.x, 1f, p.z), Vector3.up, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && !Physics.Raycast(new Vector3(p.x + 0.4f, 1f, p.z + 0.4f), Vector3.up, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    hutsak.Add($"{gela.name}: ez du sabairik.");
                }
            }

            // Misioaren ibilbidea, blokeoekin.
            List<RobotZatia> zatiak = MisioIbilbidea(d, aztarna, hutsak, ibilbidea);
            _robotBidea = zatiak;

            // Uholde osoa, blokeorik gabe: gela eta puntu guztiak.
            BlokeoAtea[] blokeoak = misioa.GetComponentsInChildren<BlokeoAtea>(true);
            foreach (BlokeoAtea b in blokeoak)
            {
                b.gameObject.SetActive(false);
            }

            Physics.SyncTransforms();
            Uholdea osoa = UholdeaEgin(SemeaBilatu(misioa, "Spawn_Jokalaria").position, aztarna);
            foreach (BlokeoAtea b in blokeoak)
            {
                b.gameObject.SetActive(true);
            }

            Physics.SyncTransforms();
            hutsak.AddRange(osoa.Zuloak.Take(10).Select(z => $"Zuloa: {z}"));
            hutsak.AddRange(osoa.Ihesak.Take(10).Select(z => $"Ihesa aztarnatik kanpo: {z}"));

            var gelaLerroak = new List<string>();
            foreach (BoxCollider gela in gelak)
            {
                Bounds b = gela.bounds;
                int nodoak = osoa.Nodoak.Count(u => Barruan(b, u.Posizioa));
                int gehienez = Mathf.Max(1, Mathf.RoundToInt(b.size.x * b.size.z / (UholdeUrratsa * UholdeUrratsa)));
                gelaLerroak.Add($"{gela.name}: {nodoak} nodo (~{100 * nodoak / gehienez} %)");
                if (nodoak == 0)
                {
                    hutsak.Add($"{gela.name}: ezin da iritsi.");
                }
            }

            int bildumaGalduak = 0;
            foreach (MisioBilduma bilduma in misioa.GetComponentsInChildren<MisioBilduma>(true))
            {
                if (Hurbilena(osoa, bilduma.transform.position, 2f) == null)
                {
                    hutsak.Add($"{bilduma.name}: ezin da iritsi.");
                    bildumaGalduak++;
                }
            }

            foreach (MailaMarka marka in misioa.GetComponentsInChildren<MailaMarka>(true))
            {
                if (Hurbilena(osoa, marka.transform.position, 1.5f) == null)
                {
                    hutsak.Add($"{marka.name}: ez dago oinez eskuragarri ({V(marka.transform.position)}).");
                }
            }

            // Prop bakoitzak talka egin behar du, izena Din_ ez bada.
            int propak = 0;
            foreach (Transform kontainerra in erroa.GetComponentsInChildren<Transform>(true).Where(t => t.name == Propak))
            {
                foreach (Transform prop in kontainerra)
                {
                    Renderer[] propRendererrak = prop.GetComponentsInChildren<Renderer>();
                    if (propRendererrak.Length == 0)
                    {
                        continue;
                    }

                    propak++;
                }
            }

            UholdeMapa(osoa, $"{d.Aurrizkia}_uholdea.png", zatiak);

            // Estatistikak.
            Renderer[] rendererrak = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            long triangeluak = rendererrak.Sum(EbidentziaKapturak.Triangeluak);
            Light[] argiak = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            int piztuta = argiak.Count(a => a.enabled);
            int itzalak = argiak.Count(a => a.enabled && a.shadows != LightShadows.None);
            int objektuak = erroa.GetComponentsInChildren<Transform>(true).Length;
            int materialak = rendererrak.SelectMany(r => r.sharedMaterials).Distinct().Count();
            List<string> prefabKostua = rendererrak
                .GroupBy(r =>
                {
                    GameObject instantzia = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
                    return instantzia != null ? PiezaIzena.Replace(instantzia.name, string.Empty) : r.name;
                })
                .Select(t => (Izena: t.Key, Tri: t.Sum(EbidentziaKapturak.Triangeluak), Kop: t.Count()))
                .OrderByDescending(t => t.Tri)
                .Take(10)
                .Select(t => $"{t.Izena}: {t.Tri} triangelu, {t.Kop} renderer")
                .ToList();

            string laburpena = $"{d.Gelak.Length} sala, {d.Loturak.Length} pasillo, {osoa.Nodoak.Count} nodo eskuragarri, {zatiak.Count} ibilbide-zati; {objektuak} objektu, {rendererrak.Length} renderer, {triangeluak} triangelu, {materialak} material, {piztuta} argi piztuta, {itzalak} itzalekin";
            informea.Lerroa($"- **Laburpena:** {laburpena}");
            informea.Atala("Misioaren ibilbidea", ibilbidea);
            informea.Atala("Gelen estaldura", gelaLerroak);
            informea.Atala("Triangeluak prefab-en arabera", prefabKostua);
            informea.Atala("HUTSAK", hutsak);
            if (hutsak.Count > 0)
            {
                informea.AmaituErrorearekin($"{hutsak.Count} huts; {laburpena}");
                return false;
            }

            informea.AmaituOndo($"0 huts; {laburpena}");
            return true;
        }

        private static void UholdeMapa(Uholdea uholdea, string fitxategia, List<RobotZatia> zatiak)
        {
            if (uholdea.Nodoak.Count == 0)
            {
                return;
            }

            int xMin = uholdea.Nodoak.Min(u => u.X) - 2;
            int zMin = uholdea.Nodoak.Min(u => u.Z) - 2;
            int zabalera = uholdea.Nodoak.Max(u => u.X) - xMin + 3;
            int altuera = uholdea.Nodoak.Max(u => u.Z) - zMin + 3;
            const int Px = 3;
            var irudia = new Texture2D(zabalera * Px, altuera * Px, TextureFormat.RGB24, false);
            irudia.SetPixels(Enumerable.Repeat(new Color(0.08f, 0.1f, 0.14f), irudia.width * irudia.height).ToArray());

            void Margotu(int x, int z, Color kolorea)
            {
                for (int a = 0; a < Px; a++)
                {
                    for (int b = 0; b < Px; b++)
                    {
                        irudia.SetPixel((x - xMin) * Px + a, (z - zMin) * Px + b, kolorea);
                    }
                }
            }

            foreach (UholdeNodo u in uholdea.Nodoak)
            {
                Margotu(u.X, u.Z, u.Y > 0.5f ? new Color(1f, 0.75f, 0.2f) : new Color(0.25f, 0.55f, 0.75f));
            }

            foreach (RobotZatia zatia in zatiak)
            {
                foreach (Vector3 p in zatia.Puntuak)
                {
                    Margotu(Mathf.RoundToInt(p.x / UholdeUrratsa), Mathf.RoundToInt(p.z / UholdeUrratsa), Color.white);
                }
            }

            string karpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "qa", "kapturak"));
            Directory.CreateDirectory(karpeta);
            File.WriteAllBytes(Path.Combine(karpeta, fitxategia), irudia.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(irudia);
        }

        // ---------- Robota: Play moduan, benetako CharacterController-arekin ----------

        private static List<RobotZatia> _robotBidea;
        private static MailaDef _robotMaila;
        private static double _robotHasiera;
        private static bool _robotMisioaAmaituta;
        private static string _robotLaburpena;

        /// <summary>Batchmode: Play-n sartu, esploratzailea misioaren ibilbidetik eraman eta irteera egiaztatu.</summary>
        public static void BatchMailaRobota()
        {
            try
            {
                _robotMaila = BatchMailaLortu();
                EditorSceneManager.OpenScene(_robotMaila.Bidea, OpenSceneMode.Single);
                if (!MailaProbatu(_robotMaila) || _robotBidea == null || _robotBidea.Count == 0)
                {
                    Debug.LogError($"{RobotaMarka}_ERROR Proba estatikoak huts egin du: ez da Play-n sartu.");
                    EditorApplication.Exit(1);
                    return;
                }

                _robotHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += RobotaEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{RobotaMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private const string RobotaMarka = "ECL_ROBOTA";
        private const double RobotaDenbora = 300.0;

        private static void RobotaEguneratu()
        {
            if (EditorApplication.timeSinceStartup - _robotHasiera > RobotaDenbora)
            {
                RobotaAmaitu(false, "Denbora agortuta.");
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            CharacterController kontrolagailua = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            if (_robotMisioaAmaituta)
            {
                // Hurrengo maila benetan kargatzen dela eta esploratzailea sortzen duela egiaztatu.
                string hurrengoa = _robotMaila.Misioa.Hurrengoa;
                if (string.IsNullOrEmpty(hurrengoa) || !File.Exists($"{EszenaKarpeta}{hurrengoa}.unity"))
                {
                    RobotaAmaitu(true, $"{_robotLaburpena}. Misioa amaituta; ez dago hurrengo eszenarik kargatzeko.");
                }
                else if (SceneManager.GetActiveScene().name == hurrengoa && kontrolagailua != null && UnityEngine.Object.FindAnyObjectByType<LevelManager>() != null)
                {
                    RobotaAmaitu(true, $"{_robotLaburpena}. Misioa amaituta; {hurrengoa} kargatuta eta esploratzailea sortuta {kontrolagailua.transform.position}.");
                }

                return;
            }

            RobotKontrola robota = UnityEngine.Object.FindAnyObjectByType<RobotKontrola>();
            if (robota != null)
            {
                _robotLaburpena = robota.Laburpena;
            }

            if (robota == null)
            {
                if (kontrolagailua != null && SceneManager.GetActiveScene().name == _robotMaila.Izena)
                {
                    Jokoa.GameEvents.SektoreaAmaitu += _ => _robotMisioaAmaituta = true;
                    robota = kontrolagailua.gameObject.AddComponent<RobotKontrola>();
                    robota.Hasi(_robotBidea, UnityEngine.Object.FindAnyObjectByType<LevelManager>());
                    Debug.Log($"{RobotaMarka}_HASI {_robotMaila.Izena}: esploratzailea {kontrolagailua.transform.position}");
                }

                return;
            }

            if (robota.Hutsegitea != null)
            {
                RobotaAmaitu(false, robota.Hutsegitea);
            }
        }

        private static void RobotaAmaitu(bool ondo, string testua)
        {
            EditorApplication.update -= RobotaEguneratu;
            var informea = new Informea(Tresna, RobotaMarka, $"{_robotMaila.Izena}: Robota Play moduan", _robotMaila.Bidea, _robotMaila.Erroa);
            if (ondo)
            {
                informea.AmaituOndo(testua);
            }
            else
            {
                informea.AmaituErrorearekin(testua);
            }

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(ondo ? 0 : 1);
        }

        private sealed class RobotZatia
        {
            public List<Vector3> Puntuak;
            public string Helburua;
        }

        /// <summary>Esploratzailearen CharacterController-a ibilbidetik eramaten du eta terminalak erabiltzen ditu, benetako trigger-ak itxaronda.</summary>
        private sealed class RobotKontrola : MonoBehaviour
        {
            private const float Abiadura = 5f;
            private const float Geldirik = 6f;

            private CharacterController _kontrolagailua;
            private Proba.EsploratzaileKontrola _esploratzailea;
            private LevelManager _kudeatzailea;
            private List<RobotZatia> _zatiak;
            private int _zatia;
            private int _puntua;
            private float _hurbilena = float.MaxValue;
            private float _aurrerapena;
            private float _ibilia;
            private int _terminalak;
            private float _denbora;

            public string Hutsegitea { get; private set; }

            public string Laburpena => $"{_ibilia:0} m, {_denbora:0} s, {_terminalak} terminal";

            public void Hasi(List<RobotZatia> zatiak, LevelManager kudeatzailea)
            {
                _kontrolagailua = GetComponent<CharacterController>();
                _esploratzailea = GetComponent<Proba.EsploratzaileKontrola>();
                _kudeatzailea = kudeatzailea;
                _zatiak = zatiak;
                _aurrerapena = Time.time;
                if (kudeatzailea == null)
                {
                    Hutsegitea = "Ez dago LevelManager-ik.";
                }
            }

            private void Update()
            {
                if (Hutsegitea != null || _zatiak == null)
                {
                    return;
                }

                _denbora += Time.deltaTime;
                if (_zatia >= _zatiak.Count)
                {
                    // Irteerako trigger-ak misioa ixten du; GameEvents-ek jakinarazten du.
                    if (!_kudeatzailea.MisioaAmaituta && Time.time - _aurrerapena > Geldirik)
                    {
                        Hutsegitea = $"Irteeran dago ({transform.position}) baina misioa ez da amaitu.";
                    }

                    return;
                }

                RobotZatia zatia = _zatiak[_zatia];
                if (_puntua >= zatia.Puntuak.Count)
                {
                    ZatiaAmaitu(zatia);
                    return;
                }

                Vector3 helburua = zatia.Puntuak[_puntua];
                Vector3 alde = helburua - transform.position;
                alde.y = 0f;
                float distantzia = alde.magnitude;
                if (distantzia < 0.3f)
                {
                    _puntua++;
                    _hurbilena = float.MaxValue;
                    _aurrerapena = Time.time;
                    return;
                }

                if (distantzia < _hurbilena - 0.02f)
                {
                    _hurbilena = distantzia;
                    _aurrerapena = Time.time;
                }
                else if (Time.time - _aurrerapena > Geldirik)
                {
                    Hutsegitea = $"Trabatuta {transform.position} puntuan, {helburua} helburuarekin (zatia {_zatia}, puntua {_puntua}/{zatia.Puntuak.Count}).";
                    return;
                }

                _esploratzailea.MakurtuEzarri(RobotakMakurtuBehar(transform.position, alde.normalized));
                Vector3 aurretik = transform.position;
                Vector3 mugimendua = alde.normalized * Mathf.Min(Abiadura * Time.deltaTime, distantzia) + Vector3.down * (9.8f * Time.deltaTime);
                _kontrolagailua.Move(mugimendua);
                _ibilia += Vector3.Distance(aurretik, transform.position);
            }

            private void ZatiaAmaitu(RobotZatia zatia)
            {
                if (zatia.Helburua != null)
                {
                    MisioTerminala terminala = FindObjectsByType<MisioTerminala>(FindObjectsInactive.Exclude).First(t => t.Helburua == zatia.Helburua);
                    if (!terminala.JokalariaBarruan)
                    {
                        if (Time.time - _aurrerapena > Geldirik)
                        {
                            Hutsegitea = $"{zatia.Helburua}: trigger-a ez da piztu {transform.position} puntuan.";
                        }

                        return;
                    }

                    BeteEmaitza emaitza = terminala.Erabili();
                    if (emaitza != BeteEmaitza.Onartua)
                    {
                        Hutsegitea = $"{zatia.Helburua}: {emaitza}.";
                        return;
                    }

                    _terminalak++;
                }

                _zatia++;
                _puntua = 0;
                _hurbilena = float.MaxValue;
                _aurrerapena = Time.time;
            }
        }
    }
}
