using System;
using System.Collections.Generic;
using Eclipse.Npc;
using UnityEngine;

namespace Eclipse.Misioak
{
    /// <summary>
    /// Misio baten eduki dinamikoa (A4): atrezzoa, borroka (arma, NavMesh, etsaiak), NPC-ak eta amaierako eszena.
    /// Resources/Misioak/ME_&lt;eszena&gt;.asset; MisioEdukiKargatzailea-k runtime-n aplikatzen du, Editorean zein build-etan.
    /// Eszenetan ez da ezer gordetzen: misioen .unity fitxategiak eta argiztapena ez dira aldatzen. MisioEdukiTresna-k sortzen du.
    /// </summary>
    public sealed class MisioEdukia : ScriptableObject
    {
        public const string Karpeta = "Misioak";

        [Serializable]
        public sealed class NpcKokapena
        {
            public NpcRola Rola;
            public Vector3 Lekua;
            public float Yaw;

            /// <summary>Lan-lekua (zientzialariak): hutsik bada, patruila.</summary>
            public bool LanLekuaDu;
            public Vector3 LanLekua;
            public float LanYaw;
            public Vector3[] Patruila = Array.Empty<Vector3>();
        }

        [SerializeField] private string _eszena;
        [SerializeField] private bool _atrezzoAktibo = true;
        [SerializeField] private bool _borrokaAktibo;
        [SerializeField] private int _etsaiKopurua = -1;
        [SerializeField] private string _etsaiAurrefab = "Borroka/PF_Etsaia_Proba";
        [SerializeField] private List<NpcKokapena> _npcak = new List<NpcKokapena>();
        [SerializeField] private string _amaieraEszena;

        public string Eszena => _eszena;

        /// <summary>Sektorearen atrezzo propioa (Resources/Atrezzo/AD_&lt;eszena&gt;).</summary>
        public bool AtrezzoAktibo => _atrezzoAktibo;

        /// <summary>Jokalariari EMP pistola eta bizitza; NavMesh-a; etsaiak markatzaileetan.</summary>
        public bool BorrokaAktibo => _borrokaAktibo;

        /// <summary>Etsai-kopurua (jokalaritik urrunenak lehenbizi); -1 = markatzaile guztiak.</summary>
        public int EtsaiKopurua => _etsaiKopurua;

        public string EtsaiAurrefab => _etsaiAurrefab;

        public IReadOnlyList<NpcKokapena> Npcak => _npcak;

        /// <summary>Misioa amaitu eta hurrengo misiorik ez dagoenean kargatzeko eszena (M3 → 07_AmaieraZinematika). Hutsik: ezer ez.</summary>
        public string AmaieraEszena => _amaieraEszena;

        public static string Bidea(string eszena) => $"{Karpeta}/ME_{eszena}";

        public static MisioEdukia Kargatu(string eszena) => Resources.Load<MisioEdukia>(Bidea(eszena));

        public void Ezarri(string eszena, bool atrezzoa, bool borroka, int etsaiKopurua, string etsaiAurrefab, List<NpcKokapena> npcak, string amaieraEszena)
        {
            _eszena = eszena;
            _atrezzoAktibo = atrezzoa;
            _borrokaAktibo = borroka;
            _etsaiKopurua = etsaiKopurua;
            _etsaiAurrefab = etsaiAurrefab;
            _npcak = npcak ?? new List<NpcKokapena>();
            _amaieraEszena = amaieraEszena;
        }

        /// <summary>Konparaziorako sinadura (tresnaren idenpotentzia).</summary>
        public string Sinadura()
        {
            var l = new List<string> { _eszena, _atrezzoAktibo.ToString(), _borrokaAktibo.ToString(), _etsaiKopurua.ToString(), _etsaiAurrefab, _amaieraEszena ?? string.Empty };
            foreach (NpcKokapena n in _npcak)
            {
                l.Add($"{n.Rola}|{V(n.Lekua)}|{n.Yaw:0.#}|{n.LanLekuaDu}|{V(n.LanLekua)}|{n.LanYaw:0.#}|{string.Join(";", Array.ConvertAll(n.Patruila, V))}");
            }

            return string.Join("\n", l);
        }

        private static string V(Vector3 v) => $"{v.x:0.##},{v.y:0.##},{v.z:0.##}";
    }
}
