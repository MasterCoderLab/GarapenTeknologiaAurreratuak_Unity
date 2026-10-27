# Horror Builder v9 — Bunkerra, basoko linterna eta FPS kontrolak

## Abiapuntua eta segurtasuna

- Unity 6000.6.0f1; proiektua: `Laberintoa3D_Final`; eszena: `Laberintoa_Horror`.
- Publikoa: `feature/laberintoa-horror`, `c5370157`; pribatua: `main`, `b2691f0`.
- Aurretik: V7/V8 instalatuta, **26 aldaketa lokal**; ez dira commiteatu.
- **Ez du Git add/commit/push egiten.** Ez du V6, V7 edo V8ko materialik aldatzen.
- `SampleScene`, NavMesh, etsaiak, eskopeta/tiroa, jauzia eta HUD berdin geratzen dira.

## V9ren hiru aldaketak

1. `HorrorPlayerMovement.cs`: `Horizontal` ardatza ez da gehiago biraketa: ezker/eskuin desplazamendua da (`transform.right`). `Mouse X` bakarrik biratzen du Rigidbody-a, lehendik bezala, `HorrorMouseLook` erabilita. Mugimendu diagonala mugatzen da abiadura bera izateko.
2. `HorrorAtmosphereV9.cs`: Main Camera-ko script berria. Bunkerreko **lau teilatuen azpian** bazaude, ilargi direkzionala eta argi orokorra moteltzen ditu, laino iluna gogortzen du (`.15`) eta urrutiko etsaiak gutxiago ikustea lortzen du. Kanpoan jatorrizko V8ko ilargia/giroa/lainoa itzultzen dira. Trantsizio leuna (gutxi gorabehera segundo bat baino gehiago).
3. Basoan bakarrik, linternaren argiak `range: 20 -> 28` eta `intensity: 10 -> 14` dauzka. Barruan jatorrizko V8 balioak mantentzen dira. `F` tekla eta `HorrorFlashlight` script-a ez dira aldatzen.

## Exekutatu

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v9.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v9/horror-builder-v9.sh" --audit
# Eszena gorde eta Unity guztiz itxi.
bash "$HOME/Downloads/HorrorBuilder_v9/horror-builder-v9.sh" --apply
```

`--audit` irakurketa hutsa da; `--apply` proiektu lokalari eragiten dio soilik. Babes-kopia:
`Garapen teknologia aurreratuak/Baliabideak/Backups/Laberintoa_Horror/Snapshots/HorrorBuilderV9_XXXXXXXX`.

V9ko iturburua, mugimenduaren aurreko kopia, eszenaren aurreko kopia, hashak eta Unity log-a gordetzen dira. Akatsean V8ko mugimendua eta eszena leheneratu, eta **V9k berak sortutako fitxategiak bakarrik** kentzen dira.

## Unity Play probak

- **Bunkerra:** linterna itzalita monstruoa korridore luzean askoz zailago ikusten da; linterna piztuta pasabide hurbila argitzen du.
- **Basoan:** linterna urrunago iristen da; ilargiaren V8ko argitasun leuna mantentzen da.
- **Mugimendua:** `A/D` albora, `W/S` aurrera/atzera, sagu X/Y kamera, diagonalaren abiadura mugatua; Space eta tiroa berdin.
- **Kontsola:** `HORROR_V9_READY`, `HORROR_NAV_READY: 3/3`.

Oharra: lainoak objektu opako batzuekin efektu desberdina izan dezake material/shaderaren arabera; Unity Play bidez egiaztatzea beharrezkoa da. Itxura gehiegi iluntzen bada, `Main Camera > HorrorAtmosphereV9` Inspector-eko balioak egokitu, eta Builderra berriro ez exekutatu.
