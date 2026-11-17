using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Eclipse.EditorTresnak
{
    /// <summary>Animator Controller baten definizioa, datu gisa: herramientak sinadura alderatu eta, behar bada, berreraikitzen dute (idenpotentea).</summary>
    internal sealed class AnimKontrolDef
    {
        public (string Izena, AnimatorControllerParameterType Mota)[] Parametroak;
        public AnimGeruzaDef[] Geruzak;
    }

    internal sealed class AnimGeruzaDef
    {
        public string Izena;
        public AvatarMask Maskara;
        public float Pisua = 1f;
        public AnimEgoeraDef[] Egoerak;
        public AnimTrantsizioDef[] Trantsizioak;
        public string Lehenetsia;
    }

    internal sealed class AnimUmeDef
    {
        public string Klipa;
        public float Atalasea;
        public float Denbora = 1f;
    }

    internal sealed class AnimEgoeraDef
    {
        public string Izena;

        /// <summary>Blend tree-aren parametroa; null bada, klip bakarra edo egoera hutsa.</summary>
        public string Parametroa;
        public AnimUmeDef[] Umeak;

        /// <summary>Klip bakarra; null eta Umeak null: egoera hutsa.</summary>
        public string Klipa;
        public bool OinIK;
        public Vector3 Kokapena;
    }

    internal sealed class AnimTrantsizioDef
    {
        /// <summary>"Any" = AnyState.</summary>
        public string Nondik;
        public string Nora;
        public (string Param, AnimatorConditionMode Modua, float Balioa)[] Baldintzak = Array.Empty<(string, AnimatorConditionMode, float)>();

        /// <summary>Balioa badu: irteera-denbora (normalizatua).</summary>
        public float? Irteera;
        public float Iraupena = 0.15f;
    }

    /// <summary>Animator Controller-ak definiziotik eraiki eta egiaztatu (sinadura testual ordenatua).</summary>
    internal static class AnimatorEraikitzailea
    {
        public const string Any = "Any";

        private static string Baldintzak(IEnumerable<(string Param, AnimatorConditionMode Modua, float Balioa)> b) =>
            string.Join(",", b.Select(x => $"{x.Param}{x.Modua}{x.Balioa:0.###}").OrderBy(x => x, StringComparer.Ordinal));

        public static string Sinadura(AnimKontrolDef def)
        {
            var l = new List<string>();
            l.AddRange(def.Parametroak.Select(p => $"P {p.Izena} {p.Mota}"));
            foreach (AnimGeruzaDef g in def.Geruzak)
            {
                l.Add($"L {g.Izena} w={g.Pisua:0.##} m={(g.Maskara != null ? g.Maskara.name : "-")} d={g.Lehenetsia}");
                foreach (AnimEgoeraDef e in g.Egoerak)
                {
                    string m = e.Umeak != null
                        ? $"BT {e.Parametroa} " + string.Join(";", e.Umeak.Select(u => $"{u.Klipa}@{u.Atalasea:0.###}x{u.Denbora:0.###}"))
                        : e.Klipa != null ? $"K {e.Klipa}" : "E";
                    l.Add($"E {g.Izena}/{e.Izena} ik={e.OinIK} {m}");
                }

                foreach (AnimTrantsizioDef t in g.Trantsizioak)
                {
                    l.Add($"T {g.Izena}/{t.Nondik}>{t.Nora} {t.Iraupena:0.###} x={(t.Irteera.HasValue ? t.Irteera.Value.ToString("0.###") : "-")} {Baldintzak(t.Baldintzak)}");
                }
            }

            return string.Join("\n", l.OrderBy(x => x, StringComparer.Ordinal));
        }

        public static string Sinadura(AnimatorController ac)
        {
            var l = new List<string>();
            l.AddRange(ac.parameters.Select(p => $"P {p.name} {p.type}"));
            AnimatorControllerLayer[] geruzak = ac.layers;
            for (int i = 0; i < geruzak.Length; i++)
            {
                AnimatorControllerLayer g = geruzak[i];
                AnimatorStateMachine sm = g.stateMachine;
                float pisua = i == 0 ? 1f : g.defaultWeight;
                l.Add($"L {g.name} w={pisua:0.##} m={(g.avatarMask != null ? g.avatarMask.name : "-")} d={sm.defaultState?.name}");
                foreach (ChildAnimatorState c in sm.states)
                {
                    string m = c.state.motion is BlendTree bt
                        ? $"BT {bt.blendParameter} " + string.Join(";", bt.children.Select(u => $"{(u.motion != null ? u.motion.name : "null")}@{u.threshold:0.###}x{u.timeScale:0.###}"))
                        : c.state.motion != null ? $"K {c.state.motion.name}" : "E";
                    l.Add($"E {g.name}/{c.state.name} ik={c.state.iKOnFeet} {m}");
                }

                IEnumerable<(string Nondik, AnimatorStateTransition T)> trantsizioak =
                    sm.states.SelectMany(c => c.state.transitions.Select(t => (c.state.name, t)))
                        .Concat(sm.anyStateTransitions.Select(t => (Any, t)));
                foreach ((string nondik, AnimatorStateTransition t) in trantsizioak)
                {
                    string irteera = t.hasExitTime ? t.exitTime.ToString("0.###") : "-";
                    l.Add($"T {g.name}/{nondik}>{t.destinationState?.name} {t.duration:0.###} x={irteera} {Baldintzak(t.conditions.Select(c => (c.parameter, c.mode, c.threshold)))}");
                }
            }

            return string.Join("\n", l.OrderBy(x => x, StringComparer.Ordinal));
        }

        /// <summary>Kontrolagailua definizioarekin bat datorren; ez bada (eta aplikatu), berreraiki. Itzultzen du: aldaketa egon den.</summary>
        public static bool Egiaztatu(string bidea, AnimKontrolDef def, Func<string, AnimationClip> klipa, bool aplikatu, out AnimatorController ac)
        {
            ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(bidea);
            if (ac != null && Sinadura(ac) == Sinadura(def))
            {
                return false;
            }

            if (!aplikatu)
            {
                return true;
            }

            if (ac != null)
            {
                AssetDatabase.DeleteAsset(bidea);
            }

            ac = AnimatorController.CreateAnimatorControllerAtPath(bidea);
            foreach ((string izena, AnimatorControllerParameterType mota) in def.Parametroak)
            {
                ac.AddParameter(izena, mota);
            }

            for (int gi = 0; gi < def.Geruzak.Length; gi++)
            {
                AnimGeruzaDef g = def.Geruzak[gi];
                if (gi > 0)
                {
                    ac.AddLayer(g.Izena);
                }

                AnimatorControllerLayer[] geruzak = ac.layers;
                geruzak[gi].name = g.Izena;
                geruzak[gi].avatarMask = g.Maskara;
                geruzak[gi].defaultWeight = g.Pisua;
                ac.layers = geruzak;

                AnimatorStateMachine sm = ac.layers[gi].stateMachine;
                var egoerak = new Dictionary<string, AnimatorState>();
                foreach (AnimEgoeraDef e in g.Egoerak)
                {
                    AnimatorState egoera;
                    if (e.Umeak != null)
                    {
                        egoera = ac.CreateBlendTreeInController(e.Izena, out BlendTree zuhaitza, gi);
                        zuhaitza.name = e.Izena;
                        zuhaitza.blendType = BlendTreeType.Simple1D;
                        zuhaitza.blendParameter = e.Parametroa;
                        zuhaitza.useAutomaticThresholds = false;
                        foreach (AnimUmeDef u in e.Umeak)
                        {
                            zuhaitza.AddChild(Klipa(klipa, u.Klipa), u.Atalasea);
                        }

                        ChildMotion[] umeak = zuhaitza.children;
                        for (int i = 0; i < umeak.Length; i++)
                        {
                            umeak[i].timeScale = e.Umeak[i].Denbora;
                        }

                        zuhaitza.children = umeak;
                    }
                    else
                    {
                        egoera = sm.AddState(e.Izena);
                        egoera.motion = e.Klipa != null ? Klipa(klipa, e.Klipa) : null;
                    }

                    egoera.iKOnFeet = e.OinIK;
                    egoera.writeDefaultValues = true;
                    egoerak[e.Izena] = egoera;
                }

                ChildAnimatorState[] umeEgoerak = sm.states;
                for (int i = 0; i < umeEgoerak.Length; i++)
                {
                    string izena = umeEgoerak[i].state.name;
                    umeEgoerak[i].position = g.Egoerak.First(e => e.Izena == izena).Kokapena;
                }

                sm.states = umeEgoerak;
                sm.defaultState = egoerak[g.Lehenetsia];
                foreach (AnimTrantsizioDef t in g.Trantsizioak)
                {
                    AnimatorStateTransition tr = t.Nondik == Any ? sm.AddAnyStateTransition(egoerak[t.Nora]) : egoerak[t.Nondik].AddTransition(egoerak[t.Nora]);
                    tr.canTransitionToSelf = false;
                    tr.hasExitTime = t.Irteera.HasValue;
                    tr.exitTime = t.Irteera ?? 0f;
                    tr.hasFixedDuration = true;
                    tr.duration = t.Iraupena;
                    foreach ((string param, AnimatorConditionMode modua, float balioa) in t.Baldintzak)
                    {
                        tr.AddCondition(modua, balioa, param);
                    }
                }
            }

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            string benetakoa = Sinadura(ac);
            if (benetakoa != Sinadura(def))
            {
                throw new InvalidOperationException($"{bidea}: ez dator bat definizioarekin.\n--- benetakoa\n{benetakoa}\n--- definizioa\n{Sinadura(def)}");
            }

            return true;
        }

        private static AnimationClip Klipa(Func<string, AnimationClip> klipa, string izena) =>
            klipa(izena) ?? throw new InvalidOperationException($"Klipa falta da: {izena}");

        /// <summary>Gorputzaren goiko aldeko maskara (bizkarra, burua, besoak): armaren geruzarako.</summary>
        public static AvatarMask GoikoMaskara(string bidea, bool aplikatu, List<string> aldaketak)
        {
            var behar = new Dictionary<AvatarMaskBodyPart, bool>
            {
                [AvatarMaskBodyPart.Root] = false,
                [AvatarMaskBodyPart.Body] = true,
                [AvatarMaskBodyPart.Head] = true,
                [AvatarMaskBodyPart.LeftLeg] = false,
                [AvatarMaskBodyPart.RightLeg] = false,
                [AvatarMaskBodyPart.LeftArm] = true,
                [AvatarMaskBodyPart.RightArm] = true,
                [AvatarMaskBodyPart.LeftFingers] = true,
                [AvatarMaskBodyPart.RightFingers] = true,
                [AvatarMaskBodyPart.LeftFootIK] = false,
                [AvatarMaskBodyPart.RightFootIK] = false,
                [AvatarMaskBodyPart.LeftHandIK] = true,
                [AvatarMaskBodyPart.RightHandIK] = true,
            };

            var maskara = AssetDatabase.LoadAssetAtPath<AvatarMask>(bidea);
            if (maskara != null && behar.All(kv => maskara.GetHumanoidBodyPartActive(kv.Key) == kv.Value))
            {
                return maskara;
            }

            aldaketak.Add($"Avatar maskara {(maskara == null ? "berria" : "eguneratua")}: {bidea} (gorputzaren goiko aldea)");
            if (!aplikatu)
            {
                return maskara;
            }

            bool berria = maskara == null;
            if (berria)
            {
                maskara = new AvatarMask { name = System.IO.Path.GetFileNameWithoutExtension(bidea) };
            }

            foreach (KeyValuePair<AvatarMaskBodyPart, bool> kv in behar)
            {
                maskara.SetHumanoidBodyPartActive(kv.Key, kv.Value);
            }

            if (berria)
            {
                AssetDatabase.CreateAsset(maskara, bidea);
            }
            else
            {
                EditorUtility.SetDirty(maskara);
            }

            return maskara;
        }
    }
}
