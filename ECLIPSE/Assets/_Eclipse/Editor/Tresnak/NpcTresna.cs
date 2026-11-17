using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Borroka;
using Eclipse.Misioak;
using Eclipse.Npc;
using Eclipse.Proba;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3 (E blokea): NPC-en eta desmenbratzearen prototipoa. Aurrebista (ezer aldatu gabe) eta Aplikatu; idenpotentea.
    /// - AC_Npc eta PF_Npc_Zientzialaria / PF_Npc_Soldadua: Quaternius UAL manikia (CC0) koloreekin eta primitiboekin (kaskoa, fusila...).
    /// - PF_Etsaia_Desmenbragarria: PF_Etsaia_Proba-ren aldaera, Desmenbratzea osagaiarekin eta "Narrastaka" egoerarekin.
    /// - 06_NpcProba: laborategia (atrezzo propioa) eta biltegia, NPC-ak lanean eta patruilan, munstro bat eta jomuga geldi bat.
    /// Misioetako eszenak eta PF_Etsaia_Proba ez dira ukitzen (misio-demoak desmenbratzerik gabe jarraitzen du).
    /// </summary>
    public static class NpcTresna
    {
        private const string Tresna = "NpcTresna";
        private const string Marka = "ECL_NPCTRESNA";
        private const string DatuKarpeta = "Assets/_Eclipse/Datuak/Npc";
        private const string ResKarpeta = "Assets/Resources/Npc";
        internal const string ProbaEszena = "Assets/_Eclipse/Eszenak/Proba/06_NpcProba.unity";
        private const string NavMeshBidea = DatuKarpeta + "/NM_06_NpcProba.asset";
        internal const string NpcKontrolagailua = DatuKarpeta + "/AC_Npc.controller";
        private const string DesmenKontrolagailua = "Assets/_Eclipse/Datuak/Borroka/AC_Etsaia_Desmenbragarria.controller";
        internal const string DesmenAurrefab = "Assets/Resources/Borroka/PF_Etsaia_Desmenbragarria.prefab";
        private const string OrbanAurrefab = "Assets/Resources/Atrezzo/Piezak/PF_At_OdolOrbana.prefab";
        private const string PiezaKarpeta = "Assets/Resources/Atrezzo/Piezak";
        private const string TiroSoinua = "Assets/_Eclipse/Audioa/Borroka/SFX_Emp_Tiroa.wav";

        /// <summary>Bertsio-markak: geometria edo konfigurazioa aldatzean igo, tresnak berreraiki dezan.</summary>
        private const string NpcBertsioa = "NpcBertsioa_2";
        private const string DesmenBertsioa = "DesmenBertsioa_1";
        internal const float DesmenBizitza = 150f;

        internal static string Aurrefab(NpcRola rola) => $"{ResKarpeta}/PF_Npc_{rola}.prefab";

        private static (string, AnimatorConditionMode, float)[] B(string param, bool balioa) => BorrokaTresna.B(param, balioa);

        private static AnimKontrolDef NpcDefinizioa() => new AnimKontrolDef
        {
            Parametroak = new[]
            {
                ("Abiadura", AnimatorControllerParameterType.Float),
                ("Lanean", AnimatorControllerParameterType.Bool),
                ("Zaindu", AnimatorControllerParameterType.Bool),
            },
            Geruzak = new[]
            {
                new AnimGeruzaDef
                {
                    Izena = "Oinarria", Lehenetsia = "Mugimendua",
                    Egoerak = new[]
                    {
                        new AnimEgoeraDef
                        {
                            Izena = "Mugimendua", Parametroa = "Abiadura", OinIK = true, Kokapena = new Vector3(300f, 0f),
                            Umeak = new[]
                            {
                                new AnimUmeDef { Klipa = "Idle_Loop", Atalasea = 0f },
                                new AnimUmeDef { Klipa = "Walk_Loop", Atalasea = 0.975f },
                                new AnimUmeDef { Klipa = "Jog_Fwd_Loop", Atalasea = 5.357f },
                            },
                        },
                        new AnimEgoeraDef { Izena = "Lanean", Klipa = "Idle_Talking_Loop", Kokapena = new Vector3(560f, 0f) },
                        new AnimEgoeraDef { Izena = "Zaindu", Klipa = "Pistol_Aim_Neutral", Kokapena = new Vector3(560f, 120f) },
                    },
                    Trantsizioak = new[]
                    {
                        new AnimTrantsizioDef { Nondik = "Mugimendua", Nora = "Lanean", Baldintzak = B("Lanean", true), Iraupena = 0.3f },
                        new AnimTrantsizioDef { Nondik = "Lanean", Nora = "Mugimendua", Baldintzak = B("Lanean", false), Iraupena = 0.3f },
                        new AnimTrantsizioDef { Nondik = "Mugimendua", Nora = "Zaindu", Baldintzak = B("Zaindu", true), Iraupena = 0.2f },
                        new AnimTrantsizioDef { Nondik = "Zaindu", Nora = "Mugimendua", Baldintzak = B("Zaindu", false), Iraupena = 0.3f },
                    },
                },
            },
        };

        /// <summary>Etsaiaren definizioa + "Narrastaka" (bi hankak galduta: makurtuta aurrera).</summary>
        private static AnimKontrolDef DesmenDefinizioa()
        {
            AnimKontrolDef d = BorrokaTresna.EtsaiDefinizioa();
            d.Parametroak = d.Parametroak.Append(("Narrastaka", AnimatorControllerParameterType.Bool)).ToArray();
            AnimGeruzaDef g = d.Geruzak[0];
            g.Egoerak = g.Egoerak.Append(new AnimEgoeraDef
            {
                Izena = "Narrastaka", Parametroa = "Abiadura", Kokapena = new Vector3(40f, 120f),
                Umeak = new[]
                {
                    new AnimUmeDef { Klipa = "Crouch_Idle_Loop", Atalasea = 0f },
                    new AnimUmeDef { Klipa = "Crouch_Fwd_Loop", Atalasea = 1.2f },
                },
            }).ToArray();
            g.Trantsizioak = g.Trantsizioak.Concat(new[]
            {
                new AnimTrantsizioDef { Nondik = "Mugimendua", Nora = "Narrastaka", Baldintzak = B("Narrastaka", true), Iraupena = 0.25f },
                new AnimTrantsizioDef { Nondik = "Narrastaka", Nora = "Mugimendua", Baldintzak = B("Narrastaka", false), Iraupena = 0.25f },
            }).ToArray();
            return d;
        }

        [MenuItem("ECLIPSE/NPC/Aktiboak: aurrebista")]
        private static void MenuAurrebista() => Exekutatu(false, false);

        [MenuItem("ECLIPSE/NPC/Aktiboak: aplikatu")]
        private static void MenuAplikatu()
        {
            if (EditorUtility.DisplayDialog(Tresna, "NPC-ak, etsai desmenbragarria eta 06_NpcProba sortu edo eguneratu? (Misioak ez dira ukitzen)", "Aplikatu", "Utzi"))
            {
                Exekutatu(true, false);
            }
        }

        /// <summary>Batchmode: aurrebista; -eclipseAplikatu-rekin, aplikatu; -eclipseEszenaBerria-rekin, 06 proba-eszena berreraiki.</summary>
        public static void BatchNpcTresna()
        {
            string[] args = Environment.GetCommandLineArgs();
            EditorApplication.Exit(Exekutatu(args.Contains("-eclipseAplikatu"), args.Contains("-eclipseEszenaBerria")) ? 0 : 1);
        }

        private static bool Exekutatu(bool aplikatu, bool eszenaBerria)
        {
            var aldaketak = new List<string>();
            var egiaztapenak = new List<string>();
            var informea = new Informea(Tresna, Marka, aplikatu ? "Aplikatu" : "Aurrebista", ProbaEszena, "NPC eta desmenbratzea (A3, E)");
            try
            {
                foreach (string karpeta in new[] { DatuKarpeta, ResKarpeta })
                {
                    if (!AssetDatabase.IsValidFolder(karpeta))
                    {
                        aldaketak.Add($"Karpeta berria: {karpeta}");
                        if (aplikatu)
                        {
                            AssetDatabase.CreateFolder(Path.GetDirectoryName(karpeta).Replace('\\', '/'), Path.GetFileName(karpeta));
                        }
                    }
                }

                var materialak = new Dictionary<string, Material>
                {
                    ["zientzialaria"] = BorrokaTresna.LitMateriala("M_Npc_Zientzialaria", new Color(0.85f, 0.87f, 0.9f), 0f, 0.35f, null, aplikatu, aldaketak, DatuKarpeta),
                    ["soldadua"] = BorrokaTresna.LitMateriala("M_Npc_Soldadua", new Color(0.17f, 0.2f, 0.17f), 0.1f, 0.3f, null, aplikatu, aldaketak, DatuKarpeta),
                    ["plaka"] = BorrokaTresna.LitMateriala("M_Npc_Plaka", new Color(0.09f, 0.1f, 0.11f), 0.75f, 0.5f, null, aplikatu, aldaketak, DatuKarpeta),
                    ["bisera"] = BorrokaTresna.LitMateriala("M_Npc_Bisera", new Color(0.05f, 0.03f, 0.02f), 0f, 0.8f, new Color(1.6f, 0.65f, 0.15f), aplikatu, aldaketak, DatuKarpeta),
                    ["pantaila"] = BorrokaTresna.LitMateriala("M_Npc_Pantaila", new Color(0.02f, 0.05f, 0.06f), 0f, 0.7f, new Color(0.3f, 1.4f, 1.7f), aplikatu, aldaketak, DatuKarpeta),
                    ["tiroa"] = BorrokaTresna.PartikulaMateriala("M_Npc_Tiroa", new Color(1f, 0.75f, 0.35f, 1f), true, aplikatu, aldaketak, testurarekin: false, karpeta: DatuKarpeta),
                };

                AudioClip soinua = AssetDatabase.LoadAssetAtPath<AudioClip>(TiroSoinua) ?? throw new InvalidOperationException($"Soinua falta da: {TiroSoinua}");
                Dictionary<string, AnimationClip> klipak = JokalariBisualTresna.UalKlipak();
                AnimationClip Klipa(string k) => klipak.TryGetValue(k, out AnimationClip c) ? c : null;
                if (AnimatorEraikitzailea.Egiaztatu(NpcKontrolagailua, NpcDefinizioa(), Klipa, aplikatu, out AnimatorController npcAc))
                {
                    aldaketak.Add($"Animator Controller: {NpcKontrolagailua} (Mugimendua, Lanean, Zaindu)");
                }

                if (AnimatorEraikitzailea.Egiaztatu(DesmenKontrolagailua, DesmenDefinizioa(), Klipa, aplikatu, out AnimatorController desmenAc))
                {
                    aldaketak.Add($"Animator Controller: {DesmenKontrolagailua} (etsaia + Narrastaka)");
                }

                foreach (NpcRola rola in new[] { NpcRola.Zientzialaria, NpcRola.Soldadua })
                {
                    NpcAurrefabrikatua(rola, npcAc, materialak, soinua, aplikatu, aldaketak, egiaztapenak);
                }

                DesmenAurrefabrikatua(desmenAc, aplikatu, aldaketak, egiaztapenak);
                ProbaEszenaEgiaztatu(aplikatu, eszenaBerria, aldaketak, egiaztapenak);

                if (aplikatu)
                {
                    AssetDatabase.SaveAssets();
                }

                informea.Atala(aplikatu ? "Aplikatutako aldaketak" : "Aplikatuko liratekeen aldaketak", aldaketak);
                informea.Atala("Egiaztapenak", egiaztapenak);
                informea.AmaituOndo($"{aldaketak.Count} aldaketa");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.Atala("Aldaketak (errorearen aurretik)", aldaketak);
                informea.Atala("Egiaztapenak", egiaztapenak);
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
        }

        // ---------- NPC-ak ----------

        private static List<string> NpcEzberdintasunak(GameObject p, NpcRola rola, AnimatorController ac)
        {
            var l = new List<string>();
            if (p == null)
            {
                l.Add("ez dago");
                return l;
            }

            if (!p.TryGetComponent(out NpcKontrola k) || k.Rola != rola) l.Add("NpcKontrola");
            if (p.GetComponent<NavMeshAgent>() == null) l.Add("NavMeshAgent");
            Animator an = p.GetComponentInChildren<Animator>();
            if (an == null || an.runtimeAnimatorController != ac) l.Add("Animator");
            if (p.transform.Find(NpcBertsioa) == null) l.Add(NpcBertsioa);
            return l;
        }

        /// <summary>Primitibo bat (kolisiorik gabe) mundu-koordenatuetan kokatuta eta hezur bati lotuta.</summary>
        private static Transform Pieza(string izena, PrimitiveType mota, Transform gurasoa, Vector3 lekua, Vector3 tamaina, Material m, bool lokala = false)
        {
            GameObject o = GameObject.CreatePrimitive(mota);
            o.name = izena;
            UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            o.layer = 2;
            o.GetComponent<MeshRenderer>().sharedMaterial = m;
            if (lokala)
            {
                o.transform.SetParent(gurasoa, false);
                o.transform.localPosition = lekua;
                o.transform.localScale = tamaina;
            }
            else
            {
                o.transform.SetPositionAndRotation(lekua, Quaternion.identity);
                o.transform.localScale = tamaina;
                o.transform.SetParent(gurasoa, true);
            }

            return o.transform;
        }

        private static void NpcAurrefabrikatua(NpcRola rola, AnimatorController ac, Dictionary<string, Material> mat, AudioClip soinua, bool aplikatu,
            List<string> aldaketak, List<string> egiaztapenak)
        {
            string bidea = Aurrefab(rola);
            var dagoena = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
            List<string> ezberdinak = NpcEzberdintasunak(dagoena, rola, ac);
            if (ezberdinak.Count == 0)
            {
                egiaztapenak.Add($"NPC zuzena: {bidea}");
                return;
            }

            aldaketak.Add($"Aurrefabrikatua {(dagoena == null ? "berria" : "berreraikia")}: {bidea} ({string.Join(", ", ezberdinak)})");
            if (!aplikatu)
            {
                return;
            }

            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                var erroa = new GameObject($"PF_Npc_{rola}") { layer = 2 };
                SceneManager.MoveGameObjectToScene(erroa, aurrebista);
                NavMeshAgent agentea = erroa.AddComponent<NavMeshAgent>();
                agentea.radius = 0.3f;
                agentea.height = 1.8f;
                agentea.speed = 1f;
                agentea.angularSpeed = 360f;
                agentea.acceleration = 8f;
                agentea.stoppingDistance = 0.1f;
                agentea.avoidancePriority = rola == NpcRola.Soldadua ? 40 : 60;
                var kapsula = erroa.AddComponent<CapsuleCollider>();
                kapsula.center = new Vector3(0f, 0.9f, 0f);
                kapsula.height = 1.8f;
                kapsula.radius = 0.28f;
                // Trigger: jokalariak eta robotak ez dute NPC-ekin talka egiten (korridoreak ez blokeatzeko); lerro-ikusmenek trigger-ak baztertzen dituzte.
                kapsula.isTrigger = true;
                NpcKontrola kontrola = erroa.AddComponent<NpcKontrola>();

                var eredua = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(JokalariBisualTresna.Ual), aurrebista);
                eredua.name = "Eredua";
                eredua.transform.SetParent(erroa.transform, false);
                foreach (Transform t in eredua.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = 2;
                }

                Animator an = eredua.GetComponent<Animator>() ?? eredua.AddComponent<Animator>();
                an.avatar = AssetDatabase.LoadAllAssetsAtPath(JokalariBisualTresna.Ual).OfType<Avatar>().First();
                an.runtimeAnimatorController = ac;
                an.applyRootMotion = false;
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Material gorputza = rola == NpcRola.Zientzialaria ? mat["zientzialaria"] : mat["soldadua"];
                foreach (SkinnedMeshRenderer r in eredua.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.sharedMaterials = Enumerable.Repeat(gorputza, r.sharedMaterials.Length).ToArray();
                }

                Transform Hezurra(HumanBodyBones h) => an.GetBoneTransform(h) ?? throw new InvalidOperationException($"Hezurra falta da: {h}");
                Transform burua = Hezurra(HumanBodyBones.Head);
                Transform bularra = an.GetBoneTransform(HumanBodyBones.UpperChest) ?? Hezurra(HumanBodyBones.Chest);
                Vector3 aurrera = erroa.transform.forward;
                Transform kanoia = null;
                Transform arma = null;
                LineRenderer izpia = null;
                AudioSource audioa = null;
                if (rola == NpcRola.Zientzialaria)
                {
                    // Datu-betaurrekoak (zian distiratsuak) eta identifikazio-txartela: siluetaz soldaduetatik bereizteko.
                    Pieza("Betaurrekoak", PrimitiveType.Cube, burua, burua.position + Vector3.up * 0.075f + aurrera * 0.115f, new Vector3(0.17f, 0.04f, 0.025f), mat["pantaila"]);
                    Pieza("Txartela", PrimitiveType.Cube, bularra, bularra.position + aurrera * 0.13f + Vector3.right * 0.08f, new Vector3(0.06f, 0.08f, 0.01f), mat["pantaila"]);
                }
                else
                {
                    Pieza("Kaskoa", PrimitiveType.Sphere, burua, burua.position + Vector3.up * 0.1f + aurrera * 0.01f, new Vector3(0.27f, 0.25f, 0.3f), mat["plaka"]);
                    Pieza("Bisera", PrimitiveType.Cube, burua, burua.position + Vector3.up * 0.08f + aurrera * 0.13f, new Vector3(0.2f, 0.055f, 0.04f), mat["bisera"]);
                    Pieza("BularPlaka", PrimitiveType.Cube, bularra, bularra.position + aurrera * 0.11f, new Vector3(0.36f, 0.3f, 0.08f), mat["plaka"]);
                    Pieza("Motxila", PrimitiveType.Cube, bularra, bularra.position - aurrera * 0.16f, new Vector3(0.3f, 0.36f, 0.14f), mat["plaka"]);

                    // Fusila: eskuineko eskuan; orientazioa NpcKontrola-k ezartzen du (mundu-koordenatuetan).
                    Transform eskua = Hezurra(HumanBodyBones.RightHand);
                    var fusila = new GameObject("Fusila") { layer = 2 };
                    fusila.transform.SetPositionAndRotation(eskua.position, Quaternion.LookRotation(aurrera));
                    fusila.transform.SetParent(eskua, true);
                    arma = fusila.transform;
                    Pieza("Gorputza", PrimitiveType.Cube, arma, new Vector3(0f, 0.02f, 0.14f), new Vector3(0.06f, 0.1f, 0.56f), mat["plaka"], lokala: true);
                    Pieza("Kargadorea", PrimitiveType.Cube, arma, new Vector3(0f, -0.08f, 0.12f), new Vector3(0.04f, 0.13f, 0.07f), mat["plaka"], lokala: true);
                    Transform kanoiHodia = Pieza("KanoiHodia", PrimitiveType.Cylinder, arma, new Vector3(0f, 0.03f, 0.5f), new Vector3(0.025f, 0.09f, 0.025f), mat["plaka"], lokala: true);
                    kanoiHodia.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Pieza("Argia", PrimitiveType.Cube, arma, new Vector3(0f, 0.075f, 0.1f), new Vector3(0.02f, 0.012f, 0.18f), mat["bisera"], lokala: true);
                    kanoia = new GameObject("Kanoia").transform;
                    kanoia.SetParent(arma, false);
                    kanoia.localPosition = new Vector3(0f, 0.03f, 0.6f);

                    var izpiObj = new GameObject("TiroIzpia") { layer = 2 };
                    izpiObj.transform.SetParent(erroa.transform, false);
                    izpia = izpiObj.AddComponent<LineRenderer>();
                    izpia.useWorldSpace = true;
                    izpia.positionCount = 2;
                    izpia.widthMultiplier = 0.03f;
                    izpia.sharedMaterial = mat["tiroa"];
                    izpia.shadowCastingMode = ShadowCastingMode.Off;
                    izpia.enabled = false;
                    audioa = erroa.AddComponent<AudioSource>();
                    audioa.clip = soinua;
                    audioa.playOnAwake = false;
                    audioa.spatialBlend = 1f;
                    audioa.minDistance = 2f;
                    audioa.maxDistance = 30f;
                }

                kontrola.Konfiguratu(rola, an, null, null, kanoia, izpia, audioa);
                var so = new SerializedObject(kontrola);
                so.FindProperty("_fusila").objectReferenceValue = arma;
                so.FindProperty("_oinezAbiadura").floatValue = 1f;
                so.ApplyModifiedPropertiesWithoutUndo();
                new GameObject(NpcBertsioa).transform.SetParent(erroa.transform, false);
                PrefabUtility.SaveAsPrefabAsset(erroa, bidea, out bool gordeta);
                if (!gordeta)
                {
                    throw new InvalidOperationException($"Ezin izan da gorde {bidea}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            egiaztapenak.Add($"NPC gordeta: {bidea}");
        }

        // ---------- Etsai desmenbragarria (aldaera) ----------

        private static void DesmenAurrefabrikatua(AnimatorController ac, bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            var oinarria = AssetDatabase.LoadAssetAtPath<GameObject>(BorrokaTresna.EtsaiAurrefab)
                           ?? throw new InvalidOperationException($"Oinarrizko etsaia falta da: {BorrokaTresna.EtsaiAurrefab} (BorrokaTresna aplikatu)");
            var orbana = AssetDatabase.LoadAssetAtPath<GameObject>(OrbanAurrefab)
                         ?? throw new InvalidOperationException($"Odol-orbana falta da: {OrbanAurrefab} (AtrezzoTresna aplikatu)");
            var dagoena = AssetDatabase.LoadAssetAtPath<GameObject>(DesmenAurrefab);
            var ezberdinak = new List<string>();
            if (dagoena == null)
            {
                ezberdinak.Add("ez dago");
            }
            else
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(dagoena) != oinarria) ezberdinak.Add("ez da PF_Etsaia_Proba-ren aldaera");
                if (dagoena.GetComponent<Desmenbratzea>() == null) ezberdinak.Add("Desmenbratzea");
                Animator an = dagoena.GetComponentInChildren<Animator>();
                if (an == null || an.runtimeAnimatorController != ac) ezberdinak.Add("Animator");
                Etsaia e = dagoena.GetComponent<Etsaia>();
                if (e == null || !Mathf.Approximately(new SerializedObject(e).FindProperty("_bizitza").floatValue, DesmenBizitza)) ezberdinak.Add("bizitza");
                if (dagoena.transform.Find(DesmenBertsioa) == null) ezberdinak.Add(DesmenBertsioa);
            }

            if (ezberdinak.Count == 0)
            {
                egiaztapenak.Add($"Etsai desmenbragarria zuzena: {DesmenAurrefab} (PF_Etsaia_Proba-ren aldaera)");
                return;
            }

            aldaketak.Add($"Aldaera {(dagoena == null ? "berria" : "berreraikia")}: {DesmenAurrefab} ({string.Join(", ", ezberdinak)})");
            if (!aplikatu)
            {
                return;
            }

            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                var instantzia = (GameObject)PrefabUtility.InstantiatePrefab(oinarria, aurrebista);
                instantzia.name = "PF_Etsaia_Desmenbragarria";
                instantzia.AddComponent<Desmenbratzea>().Konfiguratu(orbana, 20f, 15f);
                instantzia.GetComponentInChildren<Animator>().runtimeAnimatorController = ac;
                var so = new SerializedObject(instantzia.GetComponent<Etsaia>());
                so.FindProperty("_bizitza").floatValue = DesmenBizitza;
                so.ApplyModifiedPropertiesWithoutUndo();
                new GameObject(DesmenBertsioa).transform.SetParent(instantzia.transform, false);
                PrefabUtility.SaveAsPrefabAsset(instantzia, DesmenAurrefab, out bool gordeta);
                if (!gordeta)
                {
                    throw new InvalidOperationException($"Ezin izan da gorde {DesmenAurrefab}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            egiaztapenak.Add($"Aldaera gordeta: {DesmenAurrefab}");
        }

        // ---------- 06_NpcProba ----------

        private static void ProbaEszenaEgiaztatu(bool aplikatu, bool berria, List<string> aldaketak, List<string> egiaztapenak)
        {
            if (!berria && File.Exists(ProbaEszena) && AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshBidea) != null)
            {
                egiaztapenak.Add($"Proba-eszena badago: {ProbaEszena}");
                return;
            }

            aldaketak.Add($"Proba-eszena {(File.Exists(ProbaEszena) ? "berreraikia" : "berria")}: {ProbaEszena} (laborategia + biltegia, 3 zientzialari, 2 soldadu, 2 etsai-markatzaile, NavMesh)");
            if (!aplikatu)
            {
                return;
            }

            string aurrekoa = SceneManager.GetActiveScene().path;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException("Proba-eszena ez da sortu: irekitako eszena gorde gabe dago.");
            }

            Scene eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string navMesh = EszenaEraiki();
            if (!EditorSceneManager.SaveScene(eszena, ProbaEszena))
            {
                throw new InvalidOperationException($"Ezin izan da gorde {ProbaEszena}");
            }

            egiaztapenak.Add(navMesh);
            if (!string.IsNullOrEmpty(aurrekoa) && aurrekoa != ProbaEszena)
            {
                EditorSceneManager.OpenScene(aurrekoa, OpenSceneMode.Single);
            }
        }

        private static Transform Puntua(string izena, Transform gurasoa, Vector3 lekua, float yaw)
        {
            var t = new GameObject(izena).transform;
            t.SetParent(gurasoa, false);
            t.SetLocalPositionAndRotation(lekua, Quaternion.Euler(0f, yaw, 0f));
            return t;
        }

        private static void AtrezzoPieza(string pieza, Transform gurasoa, Vector3 lekua, float yaw)
        {
            var aurrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PiezaKarpeta}/{pieza}.prefab")
                           ?? throw new InvalidOperationException($"Atrezzo-pieza falta da: {pieza} (AtrezzoTresna aplikatu)");
            var o = (GameObject)PrefabUtility.InstantiatePrefab(aurrefab, gurasoa.gameObject.scene);
            o.transform.SetParent(gurasoa, false);
            o.transform.SetLocalPositionAndRotation(lekua, Quaternion.Euler(0f, yaw, 0f));
        }

        private static void NpcJarri(NpcRola rola, string izena, Transform gurasoa, Transform lanLekua, Transform[] patruila)
        {
            var o = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Aurrefab(rola)), gurasoa.gameObject.scene);
            o.name = izena;
            o.transform.SetParent(gurasoa, false);
            Transform hasiera = lanLekua != null ? lanLekua : patruila[0];
            o.transform.SetPositionAndRotation(hasiera.position, hasiera.rotation);
            var so = new SerializedObject(o.GetComponent<NpcKontrola>());
            so.FindProperty("_lanLekua").objectReferenceValue = lanLekua;
            SerializedProperty p = so.FindProperty("_patruila");
            p.arraySize = patruila?.Length ?? 0;
            for (int i = 0; i < p.arraySize; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = patruila[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Laborategia (z &lt; 1) eta biltegia (z &gt; 1), 3 m-ko ate batekin. Atrezzo propioa (kontsolak, mahaia, apalategia).
        /// Etsai aktiboa biltegian, jokalariaren spawn-etik eta soldaduen patruiletatik ikusten ez den lekuan; jomuga geldia mendebaldean.
        /// </summary>
        private static string EszenaEraiki()
        {
            Material zorua = JokalariBisualTresna.ProbaMateriala("M_Proba_Zorua", Color.white, JokalariBisualTresna.Laukiak(), 20f);
            Material horma = JokalariBisualTresna.ProbaMateriala("M_Proba_Horma", new Color(0.55f, 0.57f, 0.6f));
            Material oztopoa = JokalariBisualTresna.ProbaMateriala("M_Proba_Oztopoa", new Color(0.95f, 0.45f, 0.1f));

            var erroa = new GameObject("NpcProba").transform;
            JokalariBisualTresna.ArgiNeutroak(erroa);
            Transform geometria = Puntua("Geometria", erroa, Vector3.zero, 0f);
            JokalariBisualTresna.Kubo("Zorua", geometria, new Vector3(0f, -0.1f, 0f), new Vector3(37f, 0.2f, 27f), zorua);
            JokalariBisualTresna.Kubo("Horma_Iparra", geometria, new Vector3(0f, 1.5f, 13.25f), new Vector3(37f, 3f, 0.5f), horma);
            JokalariBisualTresna.Kubo("Horma_Hegoa", geometria, new Vector3(0f, 1.5f, -13.25f), new Vector3(37f, 3f, 0.5f), horma);
            JokalariBisualTresna.Kubo("Horma_Ekialdea", geometria, new Vector3(18.25f, 1.5f, 0f), new Vector3(0.5f, 3f, 27f), horma);
            JokalariBisualTresna.Kubo("Horma_Mendebaldea", geometria, new Vector3(-18.25f, 1.5f, 0f), new Vector3(0.5f, 3f, 27f), horma);
            JokalariBisualTresna.Kubo("Banaketa_Mendebaldea", geometria, new Vector3(-9.75f, 1.5f, 1f), new Vector3(16.5f, 3f, 0.4f), horma);
            JokalariBisualTresna.Kubo("Banaketa_Ekialdea", geometria, new Vector3(9.75f, 1.5f, 1f), new Vector3(16.5f, 3f, 0.4f), horma);
            JokalariBisualTresna.Kubo("Atalburua", geometria, new Vector3(0f, 2.6f, 1f), new Vector3(3f, 0.8f, 0.4f), horma);
            JokalariBisualTresna.Kubo("Kutxa_1", geometria, new Vector3(3f, 0.5f, 6f), new Vector3(1.2f, 1f, 1.2f), oztopoa);
            JokalariBisualTresna.Kubo("Kutxa_2", geometria, new Vector3(-4f, 0.5f, 10f), new Vector3(1.2f, 1f, 1.2f), oztopoa);
            JokalariBisualTresna.Kubo("Kutxa_3", geometria, new Vector3(12f, 0.75f, 4f), new Vector3(1.5f, 1.5f, 1.5f), oztopoa);

            // Laborategiko atrezzoa (A3 C blokeko pieza propioak): hegoaldeko hormaren kontra, aurrealdea gelara begira.
            Transform atrezzoa = Puntua("Atrezzoa", erroa, Vector3.zero, 0f);
            AtrezzoPieza("PF_At_Kontsola", atrezzoa, new Vector3(-8f, 0f, -12.52f), 0f);
            AtrezzoPieza("PF_At_Kontsola", atrezzoa, new Vector3(-4f, 0f, -12.52f), 0f);
            AtrezzoPieza("PF_At_LabMahaia", atrezzoa, new Vector3(8f, 0f, -12.47f), 0f);
            AtrezzoPieza("PF_At_Apalategia", atrezzoa, new Vector3(13.5f, 0f, -12.7f), 0f);

            Transform puntuak = Puntua("NpcPuntuak", erroa, Vector3.zero, 0f);
            Transform lan1 = Puntua("LanLekua_1", puntuak, new Vector3(-8f, 0f, -11.4f), 180f);
            Transform lan2 = Puntua("LanLekua_2", puntuak, new Vector3(-4f, 0f, -11.4f), 180f);
            Transform lan3 = Puntua("LanLekua_3", puntuak, new Vector3(8f, 0f, -11.4f), 180f);
            Transform[] s1 = { Puntua("Patruila_S1_A", puntuak, new Vector3(-14f, 0f, -6f), 90f), Puntua("Patruila_S1_B", puntuak, new Vector3(4f, 0f, -6f), 270f) };
            Transform[] s2 = { Puntua("Patruila_S2_A", puntuak, new Vector3(12f, 0f, -9f), 0f), Puntua("Patruila_S2_B", puntuak, new Vector3(12f, 0f, -3f), 180f) };

            Transform npcak = Puntua("Npcak", erroa, Vector3.zero, 0f);
            NpcJarri(NpcRola.Zientzialaria, "Zientzialaria_1", npcak, lan1, null);
            NpcJarri(NpcRola.Zientzialaria, "Zientzialaria_2", npcak, lan2, null);
            NpcJarri(NpcRola.Zientzialaria, "Zientzialaria_3", npcak, lan3, null);
            NpcJarri(NpcRola.Soldadua, "Soldadua_1", npcak, null, s1);
            NpcJarri(NpcRola.Soldadua, "Soldadua_2", npcak, null, s2);

            // Etsaiak runtime-n (misioetan bezala): markatzaileak + EtsaiSortzailea, aldaera desmenbragarriarekin.
            Transform etsaiak = Puntua("Etsaiak", erroa, Vector3.zero, 0f);
            etsaiak.gameObject.AddComponent<EtsaiSortzailea>().Konfiguratu("Borroka/PF_Etsaia_Desmenbragarria", true);
            Transform aktiboa = Puntua("Din_Marka_Etsaia_Biltegia", etsaiak, new Vector3(10f, 0f, 10f), 225f);
            aktiboa.gameObject.AddComponent<MailaMarka>().Konfiguratu(MarkaMota.Etsaia);
            Transform jomuga = Puntua("Din_Marka_Etsaia_Jomuga", etsaiak, new Vector3(-10f, 0f, 8f), 160f);
            jomuga.gameObject.AddComponent<MailaMarka>().Konfiguratu(MarkaMota.Etsaia);
            jomuga.gameObject.AddComponent<EtsaiJomugaProba>();

            Transform spawn = Puntua("Spawn_Jokalaria", erroa, new Vector3(0f, 0f, -9f), 0f);
            var kamera = new GameObject("Main Camera") { tag = "MainCamera" };
            kamera.transform.SetParent(erroa, false);
            kamera.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.65f, -9f), Quaternion.identity);
            Camera c = kamera.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
            kamera.AddComponent<AudioListener>();

            var abiarazlea = new GameObject("Abiarazlea").AddComponent<BorrokaProbaAbiarazlea>();
            abiarazlea.transform.SetParent(erroa, false);
            var serializatua = new SerializedObject(abiarazlea);
            serializatua.FindProperty("_spawn").objectReferenceValue = spawn;
            serializatua.ApplyModifiedPropertiesWithoutUndo();

            // NavMesh-a kolisio fisikoetatik (Ignore Raycast geruza kanpo: NPC-ak eta etsaiak), aktibo gisa gordeta.
            var nabigazioa = new GameObject("Nabigazioa");
            nabigazioa.transform.SetParent(erroa, false);
            var gainazala = nabigazioa.AddComponent<NavMeshSurface>();
            gainazala.collectObjects = CollectObjects.All;
            gainazala.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            gainazala.layerMask = ~(1 << 2);
            Physics.SyncTransforms();
            gainazala.BuildNavMesh();
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshBidea) != null)
            {
                AssetDatabase.DeleteAsset(NavMeshBidea);
            }

            AssetDatabase.CreateAsset(gainazala.navMeshData, NavMeshBidea);
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            return $"NavMesh: {tri.indices.Length / 3} triangelu, {NavMeshBidea}";
        }
    }
}
