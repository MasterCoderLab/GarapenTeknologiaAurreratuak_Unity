# A3 — Commit-proposamena (2026-11-14)

**Agenteak ez du `git add` ezta `commit` egin.** Ion-ek berrikusi eta egiten ditu, ordena honetan. Komandoak `ECLIPSE/` karpetatik.
`git add -A` saihestu. Aurretik: `git add --dry-run <bideak>` zer sartuko den ikusteko.

## 1. Konfigurazioa (bereizita, AGENTS §3)

Saio honen aurretik zeuden aldaketak (grafikoak eta .gitignore). Ion-ek berrikusi `git diff`-ekin.

```bash
git add .gitignore Assets/Settings/DefaultVolumeProfile.asset Assets/Settings/PC_RPAsset.asset \
  Assets/Settings/UniversalRenderPipelineGlobalSettings.asset ProjectSettings/GraphicsSettings.asset
git commit -m "ECLIPSE: URP eta grafiko-ezarpenak (A2 osteko konfigurazioa)"
```

`Assets/Settings/Build Profiles/` **ez** gehitu (A7 arte bertsiorik gabe, erabakita).

## 2. A2-ren itxiera (txostenak)

```bash
git add docs/qa/tresnak/EszenaEraikitzailea_2026-11-14_03*.md docs/qa/tresnak/JokalariBisuala_2026-11-14_032244.md \
  docs/qa/tresnak/JokalariProba_2026-11-14_03*.md
git commit -m "ECLIPSE A2: itxierako QA txostenak"
```

## 3. A3 — borroka (lehen blokea + saio luzeko hobekuntzak)

```bash
git add Assets/_Eclipse/Scriptak/Borroka.meta Assets/_Eclipse/Scriptak/Borroka/ \
  Assets/_Eclipse/Scriptak/Proba/BorrokaProbaAbiarazlea.cs* Assets/_Eclipse/Scriptak/Proba/EsploratzaileProba.cs \
  Assets/_Eclipse/Scriptak/Jokalaria/JokalariBisuala.cs \
  Assets/_Eclipse/Editor/Eclipse.Editor.asmdef Assets/_Eclipse/Editor/Tresnak/{AnimatorEraikitzailea,BorrokaTresna,NabigazioTresna,QaKapturak}.cs* \
  Assets/_Eclipse/Editor/Tresnak/EszenaEraikitzailea.{BorrokaProba,BorrokaDemoProba}.cs* \
  Assets/_Eclipse/Editor/Tresnak/{EbidentziaKapturak,EszenaEraikitzailea.JokalariProba,EszenaEraikitzailea.MailaProba,JokalariBisualTresna}.cs \
  Assets/_Eclipse/Tests/EditMode/BorrokaLogikaTests.cs* \
  Assets/_Eclipse/Datuak/Borroka.meta Assets/_Eclipse/Datuak/Borroka/ Assets/_Eclipse/Datuak/Jokalaria/ \
  Assets/Resources/Borroka.meta Assets/Resources/Borroka/ Assets/Resources/Nabigazioa.meta Assets/Resources/Nabigazioa/ \
  Assets/_Eclipse/Audioa.meta Assets/_Eclipse/Audioa/Borroka.meta Assets/_Eclipse/Audioa/Borroka/ \
  Assets/_Eclipse/Eszenak/Proba/04_BorrokaProba.unity* \
  docs/qa/kapturak/A3_Borroka/ docs/qa/tresnak/A3_Borroka_LehenBlokea_2026-11-14.md
```

Oharra: `Scriptak/Borroka/` barruan `Desmenbratzea.cs` eta `DesmenbratzeLogika.cs` daude (E blokea); `Etsaia.cs`-k faktoreak ditu. Commit bakarrean edo 5. puntuarekin batera.

## 4. A3 — sektoreen atrezzoa (C)

```bash
git add Assets/_Eclipse/Scriptak/Atrezzo.meta Assets/_Eclipse/Scriptak/Atrezzo/ \
  Assets/_Eclipse/Editor/Tresnak/{AtrezzoPiezak,AtrezzoTresna}.cs* \
  Assets/_Eclipse/Datuak/Atrezzo.meta Assets/_Eclipse/Datuak/Atrezzo/ Assets/Resources/Atrezzo.meta Assets/Resources/Atrezzo/ \
  Assets/_Eclipse/Eszenak/Proba/05_AtrezzoErakusketa.unity*
```

## 5. A3 — NPC-ak eta desmenbratzea (E)

```bash
git add Assets/_Eclipse/Scriptak/Npc.meta Assets/_Eclipse/Scriptak/Npc/ Assets/_Eclipse/Scriptak/Proba/EtsaiJomugaProba.cs* \
  Assets/_Eclipse/Editor/Tresnak/NpcTresna.cs* Assets/_Eclipse/Editor/Tresnak/EszenaEraikitzailea.NpcProba.cs* \
  Assets/_Eclipse/Tests/EditMode/NpcLogikaTests.cs* \
  Assets/_Eclipse/Datuak/Npc.meta Assets/_Eclipse/Datuak/Npc/ Assets/Resources/Npc.meta Assets/Resources/Npc/ \
  Assets/_Eclipse/Eszenak/Proba/06_NpcProba.unity*
```

## 6. A3 — amaierako zinematika (F) eta arma-ikerketa (D)

```bash
git add Assets/_Eclipse/Scriptak/Zinematika.meta Assets/_Eclipse/Scriptak/Zinematika/ \
  Assets/_Eclipse/Editor/Tresnak/ZinematikaTresna.cs* Assets/_Eclipse/Editor/Tresnak/EszenaEraikitzailea.ZinematikaProba.cs* \
  Assets/_Eclipse/Datuak/Zinematika.meta Assets/_Eclipse/Datuak/Zinematika/ \
  Assets/_Eclipse/Audioa/Zinematika.meta Assets/_Eclipse/Audioa/Zinematika/ \
  Assets/_Eclipse/Eszenak/Proba/07_AmaieraZinematika.unity* \
  docs/qa/tresnak/A3_D_ArmaEtaOntziAktiboak_2026-11-14.md
```

## 7. Dokumentazioa

```bash
git add LICENSES/ThirdParty.md docs/estado.md docs/qa/tresnak/A3_CommitProposamena_2026-11-14.md docs/qa/tresnak/A3_SaioLuzea_Txostena_2026-11-14.md
```

## Ez gehitzeko (Ion-ek erabaki)

- Tarteko txostenak `docs/qa/tresnak/{AtrezzoTresna,BorrokaDemoProba,BorrokaProba,BorrokaTresna,EszenaEraikitzailea,JokalariBisuala,JokalariProba,NabigazioTresna,NpcProba,NpcTresna,ZinematikaProba,ZinematikaTresna}_2026-11-14_1[89]*.md` / `_20*.md`: iterazioak dira. Proposamena: azken bertsioa bakarrik gorde eta besteak babeskopiara eraman (agenteak ez ditu mugitu).
- `Recordings/` (gitignore): QA kapturak exekuzio bakoitzeko karpetetan.
