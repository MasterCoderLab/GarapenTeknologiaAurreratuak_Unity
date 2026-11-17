# A4 — Auditoría de recursos, assets necesarios y plan de integración real (15/11/2026)

Autor: Claude (agente). Para Ion. **Estado: propuesta.** Nada de lo que aquí se plantea sobre escenas se ha ejecutado; lo implementado hoy (sección 4) no toca ninguna `.unity` de misión.

Restricción que condiciona la auditoría: por la cláusula de IA del EULA de la Asset Store, **no he abierto ni mirado** los kits `Sci Fi Modular Pack` (MagixBox) ni `Sci-Fi Old rusty props PBR` (KMProd), ni sus capturas (`ion_*`). Su calidad visual la juzgas tú; yo solo aporto datos de uso (nombres y recuentos).

---

## 1. Recursos actuales: qué sirve y qué no

| Recurso | Licencia | Calidad | Veredicto |
|---|---|---|---|
| **HypJo v2** (protagonista, rig Mixamo, clips UAL) | CC BY 4.0 + CC0 | Aprobado por ti; referencia mínima | **Definitivo** |
| **Kit modular MagixBox** (paredes `Wall12`, suelo `Floor1`, secciones `Profile1`/`ProfileDoor`, puertas, marcos) | EULA Asset Store | No evaluable por mí | **Se conserva** como base de las tres misiones (ya está horneado y validado) |
| **Props KMProd** (11 tipos: `Rack` ×14, `Container_small` ×14, `Container_Big` ×14, `Old_computer` ×11, `Container_BigOpen` ×7, `Box1` ×6, `Supply_box` ×4, `Vents`, `Tubes`, `Oxygen_sint`, `Geo_base`) | EULA Asset Store | No evaluable por mí | **Insuficiente por variedad**: 79 colocaciones de solo 11 modelos, repetidos en las tres misiones. Es una de las causas objetivas de que se parezcan |
| **Atrezzo propio** (26 piezas por sector: consolas, tanques, generadores, transformadores, barreras, señales, vísceras…) | Propio | Primitivas con texturas procedurales. Correcto como relleno de fondo; no como pieza protagonista | **Provisional/aceptable** en segundo plano. Las "hero pieces" (generador grande, reactor, cápsulas) deben ser modelos reales |
| **Pistola EMP** (`SM_EmpPistola`, kitbash) | Propio | Funcional, silueta simple | **Provisional** → sustituir (sección 2) |
| **Enemigo** (maniquí Quaternius UAL) | CC0 | Maniquí liso | **Provisional** → sustituir |
| **NPC** (maniquí + gafas/placa/casco/fusil kitbash) | CC0 + propio | Reconocibles por silueta, no por calidad | **Provisional** → sustituir |
| **Nave y exterior** (kitbash + Terrain procedural) | Propio | Nave poco convincente ("dirigible"); desierto aceptable como base | Nave → sustituir; terreno → mejorar con texturas/rocas CC0 |
| **Sonidos** (SoX) | Propio | Correctos como placeholders | Aceptable para la entrega; mejorable |
| **Sistemas** (arma, daño localizado, IA, NavMesh, HUD, NPC, desmembramiento, Timeline) | Propio | Probados en batch (ver sección 4) | **Definitivos**; se reutilizan tal cual |
| `Creep` (criatura) | Sin verificar | No importado | **Descartado** hasta tener licencia |

### Por qué las tres misiones se parecen (datos, no impresiones)

Las misiones no están hechas a mano: las genera `EszenaEraikitzailea` desde `EszenaEraikitzailea.MailaDatuak.cs` (rejilla de 3,75 m). Eso es una ventaja enorme: **rediseñar es editar datos y reconstruir con la herramienta**, no mover objetos a mano.

- **Misma gramática en las tres:** sala de inicio 3×3 → cruces `J1…J6` (1×1) → **laberinto `Mantentzea`** (existe en M1, M2 y M3, con el mismo truco de tabiques) → `Kontrola` → puerta bloqueada → `Irteera`.
- **Misma altura:** casi todas las salas usan 2 filas de panel; solo `LabNagusia`/`Borroka` (3), `Sorgailuak` (3), `Erreaktorea` (4) y `AzkenBorroka` (3) destacan.
- **Mismo pasillo:** todas las conexiones son secciones `Profile1` idénticas; cambian el color de la luz y el tubo.
- **Mismos 11 props** en todas las salas (tabla anterior).
- **Diferenciación actual:** solo color de luz, tubos y el atrezzo propio (A3).

---

## 2. Assets que necesitamos descargar (tú decides; yo no descargo nada)

Toda la información sale de búsquedas web del 14–15/11/2026. **Antes de usar cualquiera, hay que abrir la página, confirmar la licencia y guardar el texto de licencia junto al archivo** (`LICENSES/ThirdParty.md`). Marco con ⚠ lo que no he podido confirmar. Nada procede de Dead Space ni lo imita.

### 2.1 Imprescindibles

| # | Asset | Autor / enlace | Precio | Licencia | Formato / rig | Para | Por qué | Falta confirmar |
|---|---|---|---|---|---|---|---|---|
| 1 | **Criatura humanoide de Mixamo** (p. ej. las que Mixamo llama "Mutant", "Zombie", "Parasite", "Warrok") | Adobe Mixamo — https://www.mixamo.com (tu cuenta) | Gratis | Términos de Adobe: uso en juegos permitido, sin redistribuir el asset suelto (foros de Adobe; **leer el EULA**) | FBX, rig humanoide **ya compatible con nuestras hitboxes, UAL y desmembramiento**; cientos de animaciones (ataque, muerte, arrastrarse) | M2 (1), M3 (variantes) | Es la vía más rápida a una criatura con calidad y animaciones propias; el pipeline ya lo hicimos con HypJo | ⚠ Nombres exactos del catálogo: los compruebas tú al entrar |
| 2 | **Soldado futurista rigged** (opción A: Mixamo, personajes acorazados tipo "Vanguard"/"Exo"; opción B: soldado sci-fi CC0 de Grab3D, armadura blanca con visor cian) | A: https://www.mixamo.com · B: https://grab3d.com/free-rigged-3d-models/ | Gratis | A: Adobe · B: CC0 1.0 (según la página del índice) | FBX/GLB humanoide | M1, M2 | Sustituye al maniquí con casco; funciona con `NpcKontrola` sin cambios | ⚠ B: confirmar en la ficha del modelo concreto; A: nombres del catálogo |
| 3 | **Científico con bata** | **No he encontrado ninguno gratuito, rigged y con calidad suficiente.** Lo más cercano: "Low-Poly Rigged Scientist" (Shadow The Fox, CC BY, 386 triángulos: demasiado pobre) y batas sueltas de muy alta densidad (IA, 946k tris) | — | — | — | M1 | — | **Propuesta:** personaje humano de Mixamo (ropa civil) + **bata modelada por mí en Blender** sobre su cuerpo (malla simple, skinning copiado). Es trabajo propio y controlable. Alternativa de pago: ninguna verificada |
| 4 | **Props de laboratorio e industria en volumen** — Quaternius *Modular Sci-Fi MEGAKIT* (270+ piezas: paredes, puertas, columnas, props) y *Sci-Fi Essentials Kit* (pantallas animadas, cajas, robots, armas) | https://quaternius.itch.io/modular-sci-fi-megakit · https://quaternius.itch.io/sci-fi-essentials-kit | Gratis (Standard); Pro 9,99 $ con URP y shaders | **CC0** (itch.io y OpenGameArt) | FBX/OBJ/glTF; Pro con prefabs URP | M1, M2, M3 | Rompe la repetición de los 11 props; CC0 permite **publicarlos en el repo** (a diferencia de los kits de pago) | Estilo low-poly limpio: hay que oscurecer materiales para que casen con HypJo (lo hago yo) |
| 5 | **Piezas protagonistas industriales** — "Sci-Fi Power Reactor" (Just-Peeps, ~8,6k tris, game-ready) · "SciFi Style Generator" (Sousinho, 4K PBR, etiqueta *noAI*) | https://sketchfab.com/3d-models/sci-fi-power-reactor-442645aaebe74e648ec1460f429ee1ba · https://sketchfab.com/3d-models/scifi-style-generator-c218cb5146eb47539fee5ff8801508f9 | Gratis | **CC BY** (atribución) | glTF/FBX PBR | M2 (`Sorgailuak`, `Bobinak`), M3 | Un generador real de 3–4 m cambia la lectura de la sala más que 20 cajas | ⚠ El *noAI* de Sousinho es una etiqueta contra entrenamiento de IA; por prudencia, ese lo revisas tú |

### 2.2 Recomendables

| # | Asset | Enlace | Precio | Licencia | Para | Nota |
|---|---|---|---|---|---|---|
| 6 | **Pistola sci-fi** "Sci-Fi Pistol (Low Poly)" (Ace 3D, ~4,9k tris, PBR 2K/4K, Unity-ready) · alternativa "Sci-fi pistol" (Eugen Vahrushin, 4,4k) | https://sketchfab.com/3d-models/sci-fi-pistol-low-poly-1f7bfd56c78c4b51bf93833bedfd60d9 · https://sketchfab.com/3d-models/sci-fi-pistol-c23394d709d14772adf355da84408edc | Gratis | CC BY | EMP (M2–M3) | Sustituye el kitbash; añado emisivo cian y cañón EMP en Blender |
| 7 | **Fusil M14-like**: base CC0 de *Low Poly Guns Pack* (Quaternius, 50+ armas) adaptada en Blender con detalles del *Sci-Fi Essentials Kit* | https://quaternius.itch.io/50-lowpoly-guns | Gratis | CC0 (OpenGameArt; ⚠ confirmar en el zip) | Soldados; arma futura del jugador | No existe un "M14 futurista" libre: se construye |
| 8 | **Nave de rescate**: "Sci-fi Dropship" (Pascal T. Monette, 36,6k tris) · "Void Voyager" (UNREAL JOELAB, PBR, patas articuladas; ⚠ polígonos sin confirmar) | https://sketchfab.com/3d-models/sci-fi-dropship-709a1916c0cc4cbebe197e7ef26964cb · https://sketchfab.com/3d-models/void-voyager-sci-fi-dropship-military-vtol-06f44332a98d41008d683449464d077b | Gratis | CC BY | 07 cinemática | Alternativa CC0 estilizada: Quaternius *LowPoly Spaceships* https://quaternius.itch.io/lowpoly-spaceships |
| 9 | **Infestación**: "Living Flesh Blob" (Yury Misiyuk, game-ready Unity/UE5) · "The Flesh" (CameronProteau, 9,7k) · "Tentacle" (leunamanuelgc, PBR 2K) | https://sketchfab.com/3d-models/living-flesh-blob-free-organic-horror-asset-6357bbc094d54cf5ba150ef2951b3750 · https://sketchfab.com/3d-models/the-flesh-47fb5fb4ea0640819e5a2b1a5696a120 · https://sketchfab.com/3d-models/tentacle-007847ced64b4a3dabcbab6c40da6097 | Gratis | CC BY | M3 | Sustituyen a mis "zainak" procedurales |
| 10 | **Desierto**: Poly Haven, colección *Namaqualand* (10 rocas escaneadas, 5 materiales de suelo, 10 HDRI de desierto) + texturas de arena | https://polyhaven.com/collections/namaqualand | Gratis | **CC0** | 07 cinemática | Rocas y cielo reales en vez de mis primitivas |
| 11 | **Escombros, tuberías y suciedad realistas**: Quixel Megascans en Fab | https://www.fab.com | Gratis (según anuncio de Quixel del 5/11/2026) | Fab Standard License, válida para cualquier motor (⚠ **leer el EULA**; cambió hace días) | M3 derrumbes, M2 daños, 07 | Calidad máxima; ⚠ confirmar términos antes de descargar |

### 2.3 Opcionales

- **Kenney Blaster Kit** (CC0, https://kenney.nl/assets/blaster-kit): estilo demasiado colorido para survival horror.
- **Synty Sidekick Starter Pack** (gratis, EULA Synty, https://syntystore.com/products/sidekick-modular-characters-starter-pack): civiles modulares rigged, estilo estilizado; útil solo si aceptas ese estilo para los científicos.
- **Unity Asset Store "Sci-Fi Soldier" (Jellymobile, gratis)**: EULA de la Asset Store → yo no podría revisarlo; solo tú.

### Descartados y por qué

- Sketchfab "Low poly M16": **CC BY-NC** (no comercial) y no futurista.
- Paragon/MetaHuman (Epic): solo Unreal.
- Packs "free download" de assets de pago (unityassetcollection y similares): piratería.
- CGTrader/Fab de pago sin licencia abierta: fuera por presupuesto y porque el repo es público.

---

## 3. Mejoras arquitectónicas propuestas (datos de `MailaDatuak` + reconstrucción con la herramienta)

Reglas que respeto: incremental, una misión cada vez, respaldo verificable antes, APIs de Editor, y después de cada misión: `ArgiztapenPreseta` → bake (`BatchMailaBake`, ~10 min en la GPU) → oclusión → NavMesh (`NabigazioTresna`) → atrezzo → `MisioEdukia` → regresión completa. **Riesgo conocido:** el defecto de parches en lightmaps con pasillos (documentado en `estado.md`), por eso los pasillos siguen fuera del lightmap.

### M1 Laborategia — "investigación y misterio"

| Cambio | Cómo (datos) | Efecto |
|---|---|---|
| **Gran laboratorio** fusionando `Lab1` (16,0,3×3) y `Biologia` (20,0,3×3) en una sala 7×3 con **tabique de cristal** interior (`Leihoak`) que separa la celda de biología | `Gelak` + `BarneHormak` + `Leihoak` | Primera sala amplia con zona de observación |
| `LabNagusia` 3×4 → **4×5 con entreplanta** (`Plataformak` + `Eskailerak`, ya existen) sobre tanques de muestras | `Gelak`, `Plataformak`, `Barandak` | Verticalidad; galería de observación |
| **Quitar el laberinto `Mantentzea`** de M1 y convertirlo en almacén frío abierto con estanterías | `BarneHormak` fuera, altura 2 filas | El laberinto queda solo en M3 |
| Ventanas desde el pasillo `J3`→`LabNagusia` | `Leihoak` | Ver el laboratorio antes de entrar |
| Cápsulas/vitrinas y monitores a lo largo de `Harrera` | atrezzo (ya) + MEGAKIT (nuevo) | Identidad |

### M2 Energia — "central industrial"

| Cambio | Cómo | Efecto |
|---|---|---|
| `Sorgailuak` 5×5: subir a **4 filas** y añadir **segunda pasarela** a 8,5 m sobre la actual | `Gelak.Ilarak`, `Plataformak`, `Eskailerak` | Sala monumental con el generador protagonista (asset #5) |
| `Bobinak` 4×5 → **nave alta 5×6** con torres de bobinas y pasarela central | `Gelak`, `Plataformak`, atrezzo `Transformadorea` | Zona abierta industrial |
| **Quitar el laberinto** de M2 → sala de turbinas abierta con tuberías caídas como cobertura | `Apaingarriak` `HodiX/Z` con colisión, `MailaKolisio` | Combate con coberturas; primer monstruo escenificado en `Bobinak` |
| Luces de emergencia pulsantes en `Fusibleak1/2` y señales de evacuación | `Pultsua`, atrezzo señales | Emergencia |

### M3 Erreaktorea — "monumental, infestado"

| Cambio | Cómo | Efecto |
|---|---|---|
| `Erreaktorea` 6×6 → **8×8 con dos niveles de pasarela** (Pl y 2·Pl) y cuatro escaleras | `Gelak`, `Plataformak`, `Barandak`, `Eskailerak` | Reactor monumental de verdad |
| `AzkenBorroka` 7×5: barricadas, derrumbe parcial del techo (cajas de colisión + escombros) | `KolisioGehigarriak`, `Apaingarriak`, Megascans | Terror y supervivencia |
| `Deskontaminazioa` → **esclusa infestada** (crecimientos orgánicos, luz roja pulsante) | atrezzo + asset #9 | Primer susto |
| Mantener el laberinto solo aquí, oscuro e infestado | — | Identidad |
| `Irteera` → **compuerta al exterior** con señal `IHESA` (la misma puerta que abre la cinemática) | `Gelak`, señal | Conecta con 07 |

Las terminales, bloqueos, spawn y salidas se mantienen en las mismas salas lógicas; las rutas del robot y las pruebas HUD/Interakzio se regeneran con las herramientas.

---

## 4. Qué he implementado ya (hoy, sin modelos nuevos ni cambios de escena)

**Sistema de contenido de misión en runtime** — el jugador encuentra todo dentro de M1–M3, en Editor y en build, **sin menús especiales**:

- `MisioEdukia` (ScriptableObject en `Resources/Misioak/ME_<misión>.asset`): atrezzo, combate (arma, NavMesh, nº de enemigos y prefab), lista de NPC (rol, posición, puesto de trabajo o patrulla) y escena final.
- `MisioEdukiKargatzailea` (runtime): al cargar una misión aplica la configuración bajo un objeto `MisioEdukia`; al acabar M3 carga `07_AmaieraZinematika`.
- `MisioEdukiTresna` (menú `ECLIPSE/Misioak/Edukia`): genera los tres assets; **calcula las posiciones de los NPC desde la escena** (científicos frente a consolas/estanterías, soldados patrullando entre salas) y las valida con el NavMesh de la misión (caminos completos, lejos de terminales, puertas, bloqueos y spawn). Idempotente (segunda pasada: 0 cambios).
- Narrativa aplicada: **M1** atrezzo + 3 científicos + 2 soldados, sin arma; **M2** atrezzo + arma + 2 enemigos + 2 soldados; **M3** atrezzo + arma + 6 enemigos + cinemática final.
- Las demos del Editor siguen existiendo, pero ya no hacen falta y no duplican nada.
- Cambios pequeños asociados: `LevelManager.HurrengoEszena` (propiedad), `NpcKontrola.KokapenaEzarri`, `BorrokaDemoa` refactorizada en métodos estáticos reutilizables, colisión de los NPC como trigger (no bloquean pasillos ni al robot).

**Pruebas:** `BatchMisioEdukiProba` M1 14/14, M2 13/13, M3 13/13 (incluye: M3 completada programáticamente → se carga 07 y la Timeline arranca). La regresión completa se está ejecutando al cierre de este documento (resultado en `docs/estado.md`).

**Limitación honesta:** los personajes que aparecen son los provisionales (maniquíes). El sistema está listo para cambiar un nombre de prefab por el definitivo.

---

## 5. Qué depende de que me des modelos

| Dependencia | Bloquea |
|---|---|
| Criatura (asset #1) | Aspecto del monstruo en M2/M3; animaciones de arrastre reales; gore convincente |
| Soldado (asset #2) | Aspecto de soldados en M1/M2 |
| Científico (asset #3, vía Mixamo + bata propia) | Aspecto de científicos en M1 |
| Props CC0 (asset #4) y hero pieces (#5) | Rediseño visual real de las salas (sección 3); sin ellos solo cambiaría la geometría |
| Pistola/fusil (#6, #7) | Arma en mano con calidad |
| Nave, desierto, escombros (#8, #10, #11) | Cinemática creíble |

---

## 6. Plan de integración real (orden propuesto)

| Fase | Contenido | Requiere | Pruebas |
|---|---|---|---|
| **A4.1 (hecho)** | Contenido runtime en M1–M3 + enlace M3→07 | — | MisioEdukiProba ×3, regresión |
| **A4.2** | Enemigos definidos en `MisioEdukia` (posiciones extra sin tocar escenas); M3 con el prefab desmembrable; M2 encuentro escenificado (primer monstruo tras `Bobinak`) | Tu OK | MisioEdukiProba, BorrokaDemo |
| **A4.3** | Piloto arquitectónico en **M1** (sección 3) con respaldo, rebake y regresión | Tu aprobación del plan + props CC0 (#4) | Robot/HUD/Interakzio M1, hashes |
| **A4.4** | Personajes definitivos: criatura, soldado, científico (pipeline HypJo: Humanoid, materiales URP, prefabs por herramienta) | Assets #1–#3 | NpcProba, BorrokaProba, capturas |
| **A4.5** | M2 y M3 arquitectura + hero props + infestación | #5, #9, #11 | Regresión completa |
| **A4.6** | Armas (#6, #7), nave y desierto (#8, #10), 07 en Build Profile (necesita tu permiso en `ProjectSettings`) | Assets | ZinematikaProba, build Windows |
| **A7** | Build final sin Development, créditos y licencias | — | Build + `grep AutoProbaQA` |

---

## 7. Riesgos, pruebas y próximos pasos

**Riesgos**
- Rebake por misión (~10 min GPU) y el defecto de parches en lightmaps: cada rediseño exige revisar capturas (tú).
- Mixamo: términos de Adobe, no CC; válido para el proyecto académico, sin redistribuir FBX (ya lo hacemos así con HypJo).
- Estilo: mezclar low-poly CC0 (Quaternius) con HypJo exige retrabajo de materiales; lo asumo yo en Blender/URP.
- Rendimiento: más props y NPC; medir FPS en Play (solo tú).
- Fecha: 25/11. Prioridad realista: A4.2 → A4.4 (personajes) → A4.3 solo en M1 → A4.6 mínimo (nave/pistola). M2/M3 arquitectura solo si llegan los assets antes del 20/11.

**Decisiones que necesito de ti**
1. Aprobar o recortar la lista de descargas (2.1 primero). Dime cuáles bajas y dónde las dejas (`~/ECLIPSE_babeskopia/a2_blender/assets_candidatos/`).
2. Aprobar el plan arquitectónico de M1 como piloto (sección 3).
3. Científicos: ¿Mixamo + bata propia en Blender, o estilo Synty?
4. Mantener los maniquíes en las misiones mientras tanto (están activos ahora) o desactivar NPC/enemigos hasta tener modelos (`MisioEdukiTresna` lo permite).
5. Permiso para añadir `07_AmaieraZinematika` a Build Settings cuando toque (A4.6).
