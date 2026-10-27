# Horror Builder v10 — HUD, bunkerreko iluntasuna, headshot

## Helburua

V9 proiektu funtzionalaren gainean: bunkerraren iluntasuna areagotu linterna itzalita; jokoaren HUD zahar testuala kendu eta bizitza gorria / kartutxoak jarri; hiru etsaiei 3 HP eta headshot (tiro bakarra buruan) gehitu.

**Ez dira aldatzen:** NavMesh, jazarpeneko IA, saltoak, FPS mugimendua, V6/V8 hormak, V7 viewmodel-a, irakaslearen `SampleScene` eta GitHub-en historia.

## Erabilera

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v10.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v10/horror-builder-v10.sh" --audit
# AUDIT_OK ikusiz gero, Unity itxi ondoren:
bash "$HOME/Downloads/HorrorBuilder_v10/horror-builder-v10.sh" --apply
```

Aurreko egoera: publiko `c5370157`, pribatu `b2691f0`, V7-V9ko 36 aldaketa lokal. Auditoria desberdina bada, gelditu eta irteera aztertu.

## Eskuzko proba

- Bunkerrean F linterna itzali: ia ezin da pasilloa ikusi; F piztuta ikusgai.
- Basora irten: V9ko 28 m/14 argia ez da ukitzen.
- Pantailako behe-ezkerrean bizitza gorria 3/3; behe-eskuinean kartutxo ikonoak, x6 eta erreserba.
- Etsai bakoitzaren gainean 3/3 barra, ikusgai daudenean soilik (ez hormaren atzean).
- Buruan tiroa = 0 HP; gorputzean 3 tiro = 0 HP.
- Etsaiek, kolisioek, NavMesh-ek, eskopetaren lehen talkako Raycast-ak eta garaipenak berdin jarraitzen dute.

### Zehaztasunaren muga

Buruaren gunea `Renderer.bounds` eta tiroaren inpaktuaren altueraren bidez kalkulatzen da. Ez da benetako buru-hezurraren collider baten baliokidea. Buruko tiroa ez bada zuzen hautematen modeloarekin, egokitu behar da egiazko modeloa ikusita; ez baieztatu doitasun osoa Play probatu gabe.

## Segurtasuna

`--apply` aurretik `--audit`; Unity itxita; backing `.unity` eta 4 script zahar. GitHub-era ez du commit/push egiten. Unityk errorea emanez gero V10 fitxategi berriak soilik ezabatu eta aurreko 5 fitxategiak berreskuratzen dira.
