# A3 — Saio luzeko txostena (2026-11-14)

Egilea: Claude (agentea). **Ez da commit-ik egin.** Ion-en Play proba falta da: "probatuta" = batch-en, sarrera simulatuarekin.

## 1. Benetan inplementatua

| Blokea | Emaitza | Non ikusi |
| --- | --- | --- |
| A Egonkortasuna | Babeskopiak egiaztatuta; 46 kaptura bertsionatu leheneratuta; QA kapturak `Recordings/qa/<data>_<pid>/`-n | `QaKapturak.cs` |
| B Borroka | Apuntatzea 1./3. pertsonan, hormek/kantoiek tiroa gelditzen dute, EMP efektuak, enborreko animazioak, IA (trabak, kolpe-altuera, lerro librea), erretikula, HUD-a (bizitza + munizioa) | `04_BorrokaProba`; menua `ECLIPSE/Borroka/Demoa` |
| C Sektoreen identitatea | M1/M2/M3-rako 26 pieza propio, misio bakoitzean 67/75/56; ateak, terminalak, helburuak eta robotaren ibilbidea libre | `05_AtrezzoErakusketa`; menua `ECLIPSE/Atrezzo/Demoa` |
| D Armak | Ikerketa eta proposamena (lizentziekin) | `A3_D_ArmaEtaOntziAktiboak_2026-11-14.md` |
| E NPC-ak | Zientzialaria (lanean, begiratu, ihes), Soldadua (patruila, begiratu, munstroari tiro), Munstroa = Etsaia. Desmenbratzea: zatia bereizita, ebakidura, odola, herren/narrastaka, ahulago, gorpu aldi baterakoak | `06_NpcProba` |
| F Zinematika | Basamortu arrotza, estazioaren atea, ontzia, Timeline 28 s, soinuak (SoX), «JARRAITUKO DU…» | `07_AmaieraZinematika` |

## 2. Probak

EditMode 89/89 · BorrokaProba 47/47 · NpcProba 36/36 · ZinematikaProba 18/18 · JokalariProba M0 54, M1–M3 21 · HUD 66/91/69 · Interakzioa 25×3 · Robota 211/395/311 m (atrezzorik gabe eta atrezzoarekin) · Borroka-demoa M1/M2/M3 · Jokalariaren konpilazioa (Release, AutoProbaQA gabe) · A2 Release + QA build-ak, target-a leheneratuta · 373 fitxategi babestu hash berdinarekin.

## 3. Babestuta (ez da ukitu)

Misioetako eszenak, argiztapena eta lightmap-ak, `ProjectSettings/`, `Packages/`, `Assets/Settings/` (saioaren aurreko aldaketak bakarrik), `Assets/ThirdParty/`, beste proiektuak. Kaptura bertsionatuak: 0 aldaketa.

## 4. Arazo ezagunak

- Zinematikako ontziak "dirigible" itxura du (hegalak ertzetik ia ez dira ikusten); zerua horizontean berdexka. Prototipoa.
- NPC-ek ez dute kalte-zonarik: jokalariaren tiroek zeharkatzen dituzte (fuego amigo-a erabakitzeko).
- Munstroek jokalaria bakarrik dute helburu (ez NPC-ak).
- M2-ko 01 markatzailea: bide partziala (hodiaren azpitik makurtuta bakarrik), dokumentatuta.
- HypJo zinematikan AC_Npc-rekin: oinez/korrika abiadura gailurrak 1,8 / 7,3 m/s (kurben leuntzea); begiz egiaztatu.

## 5. Ion-ek probatu behar duena

1. `04_BorrokaProba`: sagua, R, V, apuntatzea, HUD-a.
2. `06_NpcProba`: atetik pasatu munstroa erakartzeko; jomuga geldian hankak/besoak tirokatu.
3. `07_AmaieraZinematika`: Play → zinematika osoa (soinua barne).
4. Demoak misioetan: `ECLIPSE/Borroka/Demoa` eta `ECLIPSE/Atrezzo/Demoa` aktibatu, M1→M3 jokatu, gero desaktibatu.
