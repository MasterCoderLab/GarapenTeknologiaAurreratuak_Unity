# Horror Builder v11 — faroa/argi naturala eta etsai-barrak

## Zergatik

V10ek linterna piztean **pantaila osoaren gaineko beltza** kentzen zuen.
Horrela linterna benetako Spot Light-aren gainetik pantailako esposizio aldaketa
artifizial bat sumatzen zen. Barren raycast-ak etsaiaren **buruaren gaineko**
puntura apuntatzen zuen, eta bunkerreko teilatuek barra ezkutatzen zuten.

## Aldaketa mugatuak

- `HorrorBunkerDarknessV10.cs`: pantaila osoko beltza kendu; bineta arina soilik,
  linterna piztean aldatzen ez dena.
- `HorrorAtmosphereV9.cs`: bunkerrean fog aktibatua, ExponentialSquared,
  giro-argi laua eta ilargi-argi ahulagoa; basoan aurreko balioak berreskuratu.
- `HorrorEnemyHealth.cs`: ikusgarritasuna **toraxetik** egiaztatu, barra goiko
  gorputzera egokitu, hormen atzeko etsaiei ez erakutsi. Linterna itzalita
  urruneko barrek ez dute etsaia agerian uzten. Tiroaren/3HPren kodea berdin.

## Segurtasuna

- V10eko 63 Git aldaketak eta bi biltegien HEAD-ak egiaztatzen ditu.
- Hiru script-ak V10en erreferentzia-bertsioarekin konparatzen ditu.
- Ez du `.unity` eszena, linterna fisikoa, HUD, eskopeta, NavMesh edo materialik aldatzen.
- Aurreko hiru script-ak eta eszena Snapshots-en gorde; errorean berreskuratu.
- GitHub-eko commit/push bat ere ez.

## Erabilera

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v11.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v11/horror-builder-v11.sh" --audit
# HBB11_AUDIT_OK baldin bada; Unity itxi:
bash "$HOME/Downloads/HorrorBuilder_v11/horror-builder-v11.sh" --apply
```

## Play probak

1. Bunkerrean F piztu/itzali: fokua soilik argitu; ez pantaila osoa.
2. Ilunpean urruneko korridorea desagertu, ez linternaren zirkulua.
3. Bi etsai aurrean eta alde bietatik: barrak ikusgai argiztatuta daudenean.
4. Hiru gorputz-tiro, headshot bakarra, kartutxoak eta HUD berdin.

**Kontuan:** lainoaren dentsitatea (`v11BunkerLainoDentsitatea`) editatzeko
atributua `HorrorAtmosphereV9` inspector-ean agertuko da. Ez aldatu irakaslearen SampleScene.

Linternaren `Light.innerSpotAngle` bunkerrean murrizten da beharrezkoa
bada, fokuaren ertzaren trantsizioa leuntzeko (Unity 6000.6 URP API).
