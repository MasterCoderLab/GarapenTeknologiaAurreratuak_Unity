# Horror Builder v8 — leheneratze bisuala eta gaueko argiztapena

## Abiapuntua

V7 aplikatuta (20 tokiko aldaketa; ez dago GitHubera igota); publikoan `c5370157`, pribatuan `b2691f0`.

## Aldaketa bakarrak

- 45 bunker-horma + 8 hormigoi-zati/teilatu: V7ko material-erreferentziaren ordez V6ko `HB6_Horma*` jatorrizko materiala. Ez da ezabatu V7ko materialik edo aldatu V6ko fitxategirik.
- Bunkerreko 4 larrialdi-argi: intentsitatea **0.60 → 0.27**; irteerako argi gorria: **1.80 → 1.25**.
- Kanpoko ilargi-argi direkzionala: **0.13 → 0.29**. Basoko 2 argi urdin: **0.35 → 0.58**, irismena **12 → 16**.
- Laino, argi orokor, linterna, kamera Overlay, eskopeta, NavMesh, jokalari, etsaien IA, interfazea eta geometria **bere horretan**.

## Exekuzioa

```
unzip -o "$HOME/Downloads/HorrorBuilder_v8.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v8/horror-builder-v8.sh" --audit
# Unityn eszena gorde eta Unity itxi
bash "$HOME/Downloads/HorrorBuilder_v8/horror-builder-v8.sh" --apply
```

Auditoriak ez du ezer aldatzen; `--apply`-k eszena eta V8-ko Builder fitxategi propioak soilik aldatzen ditu. Ez da commit/push egiten. Akatsen kasuan aurreko eszena leheneratzen da, eta V8-ren fitxategiak soilik erretiratzen dira; V7 osoa mantentzen da.

## Play probak

1. Bunkerra ilunagoa; linternak protagonismo handiagoa eta larrialdi-seinaleak oraindik agerian.
2. Kanpoan ilargi-argi urdin leuna: basoko bidea eta txabola hobeto ikusten dira, baina gaua izaten jarraitzen du.
3. Hormak V6-ren itxura berarekin; horma/lur kolisioak eta kamera V7 bezala ongi.
4. `HORROR_NAV_READY: 3/3`, saltoa eta eskopeta ongi.

**Kontuz:** materiala leheneratzeak ez du geometria gainjarriak sortutako z-fighting-a berez konpontzen. Distortsio pixka bat geratzen bada, kokapen zehatza identifikatu behar da.

Ez jarraitu v8 `--apply` errepikatzen akatsen ondoren; bidali log-a. Checkpoint pribatua eta publikoa, biak bereizita, Play probak gainditu ondoren.
