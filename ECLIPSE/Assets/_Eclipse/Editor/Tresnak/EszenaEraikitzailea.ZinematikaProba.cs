using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Zinematika;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3 (F blokea): amaierako zinematikaren proba Play moduan (07_AmaieraZinematika, batch). Eszenarik ez da gordetzen.
    /// Une giltzarrietan egoera neurtzen du (atea, HypJo, ontzia, iluntzea, testua) eta zinematikaren kameratik kapturak ateratzen ditu.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string ZpMarka = "ECL_ZINEMATIKAPROBA";
        private static double _zpHasiera;
        private static readonly List<string> ZpErroreak = new List<string>();

        public static void BatchZinematikaProba()
        {
            try
            {
                EditorSceneManager.OpenScene(ZinematikaTresna.Eszena, OpenSceneMode.Single);
                ZpErroreak.Clear();
                Application.logMessageReceived += ZpLog;
                _zpHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += ZpEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ZpMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void ZpLog(string mezua, string pila, LogType mota)
        {
            if ((mota == LogType.Error || mota == LogType.Exception) && !mezua.StartsWith(ZpMarka, StringComparison.Ordinal) && ZpErroreak.Count < 20)
            {
                ZpErroreak.Add($"{mota}: {mezua.Split('\n')[0]}");
            }
        }

        private static void ZpEguneratu()
        {
            ZinematikaProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<ZinematikaProbaKontrola>();
            if (EditorApplication.timeSinceStartup - _zpHasiera > 240.0)
            {
                var l = kontrola != null ? kontrola.Lerroak : new List<string>();
                l.Add("HUTS: denbora agortuta");
                ZpAmaitu(l);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (kontrola == null)
            {
                if (UnityEngine.Object.FindAnyObjectByType<PlayableDirector>() != null)
                {
                    kontrola = new GameObject("ZinematikaProbaKontrola").AddComponent<ZinematikaProbaKontrola>();
                    kontrola.Hasi();
                }

                return;
            }

            if (kontrola.Amaituta)
            {
                ZpAmaitu(kontrola.Lerroak);
            }
        }

        private static void ZpAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= ZpEguneratu;
            Application.logMessageReceived -= ZpLog;
            lerroak.Add(ZpErroreak.Count == 0 ? "ONDO: Play-n 0 errore/salbuespen log-ean" : $"HUTS: Play-n {ZpErroreak.Count} errore: {string.Join(" | ", ZpErroreak)}");
            var informea = new Informea("ZinematikaProba", ZpMarka, "A3 F: amaierako zinematika Play-n (07_AmaieraZinematika)", ZinematikaTresna.Eszena, "Zinematika");
            informea.Atala("Emaitzak", lerroak);
            int hutsak = lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal));
            if (hutsak == 0)
            {
                informea.AmaituOndo($"{lerroak.Count(l => l.StartsWith("ONDO", StringComparison.Ordinal))} egiaztapen, 0 huts");
            }
            else
            {
                informea.AmaituErrorearekin($"{hutsak} huts");
            }

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(hutsak == 0 ? 0 : 1);
        }

        private sealed class ZinematikaProbaKontrola : MonoBehaviour
        {
            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;
            private PlayableDirector _zuzendaria;
            private Camera _kamera;
            private string _kapturak;

            private void Oharra(string izena, bool ondo, string x = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena}{(string.IsNullOrEmpty(x) ? string.Empty : $" ({x})")}");

            public void Hasi()
            {
                StartCoroutine(Exekutatu());
            }

            private IEnumerator Arte(double t)
            {
                while (_zuzendaria.time < t && _zuzendaria.state == PlayState.Playing)
                {
                    yield return null;
                }
            }

            private IEnumerator Argazkia(string izena, Canvas oihala = null)
            {
                yield return null;
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderMode aurrekoa = oihala != null ? oihala.renderMode : RenderMode.ScreenSpaceOverlay;
                if (oihala != null)
                {
                    oihala.renderMode = RenderMode.ScreenSpaceCamera;
                    oihala.worldCamera = _kamera;
                    oihala.planeDistance = 0.5f;
                    Canvas.ForceUpdateCanvases();
                }

                _kamera.targetTexture = rt;
                _kamera.Render();
                RenderTexture.active = rt;
                var irudia = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                irudia.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                irudia.Apply();
                RenderTexture.active = null;
                _kamera.targetTexture = null;
                if (oihala != null)
                {
                    oihala.worldCamera = null;
                    oihala.renderMode = aurrekoa;
                }

                QaKapturak.Gorde(irudia.EncodeToPNG(), _kapturak, $"neutro_{izena}.png");
                Destroy(irudia);
                rt.Release();
                Destroy(rt);
            }

            private IEnumerator Exekutatu()
            {
                _zuzendaria = FindAnyObjectByType<PlayableDirector>();
                _kamera = Camera.main;
                _kapturak = QaKapturak.Azpikarpeta("A3_Zinematika");
                Transform ezker = GameObject.Find("Panela_Ezkerra").transform;
                Transform eskuin = GameObject.Find("Panela_Eskuina").transform;
                Light barnekoa = GameObject.Find("Barneko_Argia").GetComponent<Light>();
                GameObject hypjoBidea = GameObject.Find("HypJo_Bidea");
                ZinematikaAbiadura abiadura = hypjoBidea.GetComponentInChildren<ZinematikaAbiadura>(true);
                Transform hypjo = abiadura.transform;
                Transform ontzia = GameObject.Find("Ontzia").transform;
                Canvas oihala = GameObject.Find("Amaiera_Pantaila").GetComponent<Canvas>();
                CanvasGroup beltza = oihala.GetComponent<CanvasGroup>();
                CanvasGroup testuTaldea = oihala.transform.Find("Testua").GetComponent<CanvasGroup>();
                Text testua = testuTaldea.GetComponent<Text>();

                Oharra("Zuzendaria Timeline-arekin martxan", _zuzendaria.playableAsset != null && _zuzendaria.state == PlayState.Playing,
                    $"{_zuzendaria.duration:0.0} s");
                Oharra($"Iraupena {ZinematikaTresna.Iraupena:0} s", Mathf.Abs((float)_zuzendaria.duration - ZinematikaTresna.Iraupena) < 0.5f);
                Oharra("Basamortua: Terrain, zeru prozedurala eta lainoa", Terrain.activeTerrain != null && RenderSettings.skybox != null
                    && RenderSettings.skybox.shader.name == "Skybox/Procedural" && RenderSettings.fog);
                Oharra("Kamera nagusi bakarra (zinematikarena)", _kamera != null && FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Length == 1);

                yield return Arte(0.3);
                Oharra("Hasieran iluntzetik irteten da", beltza.alpha > 0.4f, $"alfa {beltza.alpha:0.00}");
                yield return Arte(2.0);
                yield return Argazkia("01_atea_irekitzen");
                yield return Arte(3.0);
                Oharra("Atea irekita (panelak alboetara) eta barneko argia piztuta", ezker.localPosition.x <= -2.1f && eskuin.localPosition.x >= 2.1f && barnekoa.intensity > 3f,
                    $"x {ezker.localPosition.x:0.00}/{eskuin.localPosition.x:0.00}, argia {barnekoa.intensity:0.0}");
                yield return Arte(4.4);
                Oharra("HypJo oinez irteten da (abiadura 0,6–2 m/s)", hypjoBidea.activeInHierarchy && abiadura.Abiadura > 0.6f && abiadura.Abiadura < 2f, $"{abiadura.Abiadura:0.00} m/s");
                Oharra("HypJo-ren eredua ez da gurasotik desplazatzen (root motion-ik ez)", hypjo.localPosition.magnitude < 0.05f, $"{hypjo.localPosition.magnitude:0.000} m");
                yield return Arte(5.5);
                yield return Argazkia("02_hypjo_kanpora");
                yield return Arte(6.45);
                Oharra("HypJo kanpoan, atetik ≥ 4 m", hypjoBidea.transform.position.z >= 3.8f, $"z {hypjoBidea.transform.position.z:0.0}");
                yield return Arte(8.5);
                yield return Argazkia("03_ontzia_hurbiltzen");
                yield return Arte(11.0);
                yield return Argazkia("04_lurreratzea_hautsa");
                bool hautsa = GameObject.Find("Hautsa_Lurreratzea") != null;
                Oharra("Lurreratzean hautsa aktibo", hautsa);
                yield return Arte(12.4);
                Vector3 lur = ZinematikaTresna.Lurreratzea;
                Oharra("Ontzia lurreratuta (lekuan, y ≈ 0)", Vector3.Distance(ontzia.position, lur) < 0.1f, $"{ontzia.position}");
                yield return Arte(14.0);
                Oharra("HypJo korrika ontzira (> 3 m/s)", abiadura.Abiadura > 3f, $"{abiadura.Abiadura:0.0} m/s");
                yield return Argazkia("05_hypjo_ontzira");
                yield return Arte(15.8);
                Oharra("HypJo ontzian sartuta (desaktibatuta)", !hypjoBidea.activeInHierarchy);
                yield return Arte(17.6);
                yield return Argazkia("06_aireratzea");
                yield return Arte(19.5);
                yield return Argazkia("07_ihesa");
                yield return Arte(21.4);
                Oharra("Ontzia urrun eta goian", ontzia.position.y > 80f, $"y {ontzia.position.y:0}");
                yield return Arte(25.0);
                Oharra("Iluntzea osoa eta testua ikusgai", beltza.alpha > 0.98f && testuTaldea.alpha > 0.95f, $"{beltza.alpha:0.00}/{testuTaldea.alpha:0.00}");
                Oharra($"Testua: «{ZinematikaTresna.AmaieraTestua}»", testua.text == ZinematikaTresna.AmaieraTestua);
                yield return Argazkia("08_jarraituko_du", oihala);
                ZinematikaAmaiera amaiera = _zuzendaria.GetComponent<ZinematikaAmaiera>();
                float muga = Time.time + 6f;
                while (!amaiera.Amaituta && Time.time < muga)
                {
                    yield return null;
                }

                Oharra("Zinematika amaituta (ECL_ZINEMATIKA_AMAITUTA)", amaiera.Amaituta);
                Amaituta = true;
            }
        }
    }
}
