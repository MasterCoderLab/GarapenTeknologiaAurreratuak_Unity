# Horror Builder v6 — kamera eta hormen azken ukituak

## Helburua

Unity 6000.6.0f1 · `Laberintoa_Horror` · DAM 2AG3.

1. **Sagua X/Y**: ezker-eskuin (`Rigidbody.MoveRotation`) eta gora-behera (`Main Camera.localRotation`, ±72°). `WASD`, `ESPAZIOA`, `F`, `E`, `R`, tiroa eta jokoaren HUDa ez dira aldatzen.
2. **Kamera hormaren kontra**: `nearClipPlane=0.045`, kamera jokalariaren kapsularen barruan 0.10 m atzera. Horrela murrizten da hormen ikusizko zeharkatze txikia. Horrek ez du bermatzen fisikako edozein arazo konpontzea, Play probak behar dira.
3. **PBR hormen itxura leundu**: V5 hormigoi-materialen hiru **kopia berri** sortzen dira (`HB6_HormaLaburra/Ertaina/Luzea`); erliebe-intentsitatea eta distira txikiagoak. 45 horma eta sarrerako lau horma-zati eguneratzen dira, baita lau teilatu ere. Jatorrizko V5 materialak ez dira aldatzen.

## Egiaztapenak

- `--audit` irakurketa hutsa. V5 egoera lokala eta bi repoetako aurreko commitak egiaztatzen ditu.
- `--apply`: aurreko eszena eta `HorrorPlayerMovement.cs` gordetzen ditu; soilik fitxategi horiek, script berria eta V6 materialak aldatzen ditu.
- **Ez dago** `git add`, `git commit`, `git push`, `git reset`, `git clean`.
- `SampleScene`, NavMesh, IA, eskopeta eta beste jokagarritasun-scriptak hash bidez egiaztatzen dira.
- Errorean rollback-ak V6 baliabideak kentzen ditu eta eszena/mugimendua leheneratzen ditu. Loga eta kopia mantentzen dira.

## Exekutatu

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v6.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v6/horror-builder-v6.sh" --audit
# Unity itxi ondoren:
bash "$HOME/Downloads/HorrorBuilder_v6/horror-builder-v6.sh" --apply
```

## Probak Unityn

1. `HORROR_NAV_READY: 3/3 agents on NavMesh`.
2. Saguarekin ezker-eskuin eta gora-behera begiratu; `ESC` sagua askatzeko eta jokoan klik sagua berriz blokeatzeko.
3. Eskopetaren tiroak kameraren norabidean doazela egiaztatu. Linterna ikusgai eta funtzional (`F`).
4. Paretara hurbildu eta kamera mugitu: ez da azalera atzean agertu behar. Hormako josturen distira txikiagoa behatu.
5. Saltoa, hiru bizitzak, hiru etsaiak, irteera, garaipena eta porrota probatu.
6. **Ez egin checkpointik** visual/Play probak gainditu arte.

*Oharra:* hormen elkarguneetan benetako z-fighting geometrikoa badago, material-leuntzeak ez du hori automatikoki konpontzen; log eta irudi zehatzak bidali.
