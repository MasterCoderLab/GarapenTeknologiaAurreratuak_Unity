using System;

namespace Eclipse.Npc
{
    /// <summary>
    /// NPC-en rolak (A3, E blokea). Narratiba: M1 zientzialariak eta soldaduak (giroa, ez etsaiak), M2 soldaduak eta lehen munstroa,
    /// M3 munstroen inbasioa. Munstroa da etsai bakarra: haren adimena EtsaiEgoeraMakina da (Etsaia osagaia), ez hau.
    /// </summary>
    public enum NpcRola
    {
        Zientzialaria,
        Soldadua,
        Munstroa,
    }

    public enum NpcEgoera
    {
        /// <summary>Lan-lekuan (kontsola, mahaia) lanean.</summary>
        Lanean,

        /// <summary>Patruila-puntuen artean oinez.</summary>
        Patruila,

        /// <summary>Jokalaria hurbil: gelditu eta begiratu.</summary>
        Begiratu,

        /// <summary>Zientzialaria: munstroa edo tiroak, korrika ihes.</summary>
        Ihes,

        /// <summary>Soldadua: munstroari aurre egin (begiratu eta tiro).</summary>
        Zaindu,
    }

    public enum NpcAbiadura
    {
        Geldi,
        Oinez,
        Korrika,
    }

    /// <summary>Fotograma bateko pertzepzioa (egokitzaileak kalkulatzen du: distantziak, ikusmena, tiro-hotsak).</summary>
    public readonly struct NpcPertzepzioa
    {
        public NpcPertzepzioa(float jokalariDistantzia, bool jokalariaIkusten, float mehatxuDistantzia, bool mehatxuaIkusten, bool alarma)
        {
            JokalariDistantzia = jokalariDistantzia;
            JokalariaIkusten = jokalariaIkusten;
            MehatxuDistantzia = mehatxuDistantzia;
            MehatxuaIkusten = mehatxuaIkusten;
            Alarma = alarma;
        }

        public float JokalariDistantzia { get; }

        public bool JokalariaIkusten { get; }

        /// <summary>Munstro bizirik hurbilenaren distantzia (float.PositiveInfinity, ez badago).</summary>
        public float MehatxuDistantzia { get; }

        public bool MehatxuaIkusten { get; }

        /// <summary>Fotograma honetan tiro-hotsa entzun da.</summary>
        public bool Alarma { get; }

        public static NpcPertzepzioa Lasaia(float jokalariDistantzia = float.PositiveInfinity, bool jokalariaIkusten = false) =>
            new NpcPertzepzioa(jokalariDistantzia, jokalariaIkusten, float.PositiveInfinity, false, false);
    }

    public readonly struct NpcErabakia
    {
        public NpcErabakia(NpcEgoera egoera, bool egoeraAldatu, NpcAbiadura abiadura, bool tiro)
        {
            Egoera = egoera;
            EgoeraAldatu = egoeraAldatu;
            Abiadura = abiadura;
            Tiro = tiro;
        }

        public NpcEgoera Egoera { get; }

        public bool EgoeraAldatu { get; }

        public NpcAbiadura Abiadura { get; }

        /// <summary>Soldadua bakarrik, eta beti munstroaren aurka: inoiz ez jokalariaren aurka.</summary>
        public bool Tiro { get; }

        public bool JokalariaBegiratu => Egoera == NpcEgoera.Begiratu;

        public bool MehatxuaBegiratu => Egoera == NpcEgoera.Zaindu;
    }

    /// <summary>
    /// Zientzialarien eta soldaduen egoera-makina (logika hutsa, MonoBehaviour-ik gabe). Ez dira inoiz jokalariaren etsaiak:
    /// erabakietan ez dago jokalariaren aurkako ekintzarik. Egokitzailea: Npc osagaia (NavMeshAgent, Animator, burua).
    /// </summary>
    public sealed class NpcEgoeraMakina
    {
        public const float BegiratuDistantzia = 3.5f;
        public const float BegiratuIraupena = 1.5f;
        public const float IkaraDistantzia = 12f;
        public const float IkaraIraupena = 6f;
        public const float ZainduDistantzia = 16f;
        public const float ZainduIraupena = 4f;
        public const float TiroTartea = 0.9f;

        private readonly bool _lanLekuaDu;
        private float _begiratuDenbora;
        private float _ikaraDenbora;
        private float _zainduDenbora;
        private float _tiroDenbora;

        public NpcEgoeraMakina(NpcRola rola, bool lanLekuaDu)
        {
            if (Etsaia(rola))
            {
                throw new ArgumentException("Munstroaren adimena EtsaiEgoeraMakina da, ez NpcEgoeraMakina.", nameof(rola));
            }

            Rola = rola;
            _lanLekuaDu = lanLekuaDu;
            Egoera = Lasaia();
        }

        public NpcRola Rola { get; }

        public NpcEgoera Egoera { get; private set; }

        /// <summary>Jokalariaren etsaia den rola: Munstroa bakarrik (zientzialariak eta soldaduak ez, lehenetsita).</summary>
        public static bool Etsaia(NpcRola rola) => rola == NpcRola.Munstroa;

        public NpcErabakia Eguneratu(float dt, in NpcPertzepzioa p)
        {
            dt = Math.Max(0f, dt);
            _begiratuDenbora = Math.Max(0f, _begiratuDenbora - dt);
            _ikaraDenbora = Math.Max(0f, _ikaraDenbora - dt);
            _zainduDenbora = Math.Max(0f, _zainduDenbora - dt);
            _tiroDenbora = Math.Max(0f, _tiroDenbora - dt);

            NpcEgoera aurrekoa = Egoera;
            bool tiro = false;
            NpcAbiadura abiadura;
            if (Rola == NpcRola.Zientzialaria)
            {
                if (p.Alarma || (p.MehatxuaIkusten && p.MehatxuDistantzia <= IkaraDistantzia))
                {
                    _ikaraDenbora = IkaraIraupena;
                }

                if (_ikaraDenbora > 0f)
                {
                    Egoera = NpcEgoera.Ihes;
                    abiadura = NpcAbiadura.Korrika;
                }
                else
                {
                    abiadura = Lasaia(p);
                }
            }
            else
            {
                bool mehatxua = p.MehatxuaIkusten && p.MehatxuDistantzia <= ZainduDistantzia;
                if (mehatxua || p.Alarma)
                {
                    _zainduDenbora = ZainduIraupena;
                }

                if (_zainduDenbora > 0f)
                {
                    Egoera = NpcEgoera.Zaindu;
                    abiadura = NpcAbiadura.Geldi;
                    if (mehatxua && _tiroDenbora <= 0f)
                    {
                        tiro = true;
                        _tiroDenbora = TiroTartea;
                    }
                }
                else
                {
                    abiadura = Lasaia(p);
                }
            }

            return new NpcErabakia(Egoera, Egoera != aurrekoa, abiadura, tiro);
        }

        /// <summary>Arriskurik gabe: jokalaria hurbil badago begiratu; bestela, lanean edo patruilan.</summary>
        private NpcAbiadura Lasaia(in NpcPertzepzioa p)
        {
            if (p.JokalariaIkusten && p.JokalariDistantzia <= BegiratuDistantzia)
            {
                _begiratuDenbora = BegiratuIraupena;
            }

            if (_begiratuDenbora > 0f)
            {
                Egoera = NpcEgoera.Begiratu;
                return NpcAbiadura.Geldi;
            }

            Egoera = Lasaia();
            return Egoera == NpcEgoera.Lanean ? NpcAbiadura.Geldi : NpcAbiadura.Oinez;
        }

        private NpcEgoera Lasaia() => _lanLekuaDu ? NpcEgoera.Lanean : NpcEgoera.Patruila;
    }
}
