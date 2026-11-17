# A3, primer bloque: núcleo de combate en escena aislada (14/11/2026)

**Estado:**
- **Comprobado en batch:** todo lo que figura en las tablas.
- **Pendiente:** tu revisión jugando (ratón, R y V con entrada real).
- **Sin tocar:** las tres misiones; el combate solo existe en `04_BorrokaProba`.

**Git:** sin commits ni operaciones de Git; rama `feature/eclipse-hud` sobre `09d2c73a`.
**Copia de seguridad y logs:** `~/ECLIPSE_babeskopia/a3_lehen_blokea_20261114_183802/`.

## 1. Qué hay

| Pieza | Qué hace |
|---|---|
| **Lógica pura** (`Scriptak/Borroka/`, 19 tests EditMode) | `ArmaLogika`: cargador de 8, reserva 24 (máximo 64), cadencia 0,3 s, recarga 1,5 s. `BizitzaLogika`: vida, daño, invulnerabilidad breve, curación y muerte. `GorputzKalteLogika`: zonas de daño (cabeza ×2, torso ×1, brazos ×0,6, piernas ×0,75); cada extremidad se rompe al acumular 40 de daño. `EtsaiEgoeraMakina`: Zain → Jarraitu → Eraso → Txundituta (EMP) → Bilatu → Hilda |
| **Pistola EMP** (`JokalariArma`, `EmpPistolaBisuala`) | Clic derecho apunta: el modelo mira a la cámara, que se acerca a (0,6; 0,05; −1,3) y se activa la capa "Arma". Clic izquierdo dispara desde la retícula; R recarga. **Comprueba pecho → boca → impacto**, así que nunca atraviesa paredes, tampoco con la mano metida en una. Rayo, destello, chispas y sonidos SoX. El disparo alerta a los enemigos en 25 m |
| **Jugador** (`JokalariBizitza`, `BorrokaHud`) | 100 de vida, flash rojo, animación de impacto, muerte y, en la escena de pruebas, reaparición a los 2 s. HUD con retícula, munición y vida |
| **Enemigo** (`Etsaia`, `EtsaiSortzailea`) | Componente común a cualquier modelo: vista (18 m, ±75°, línea de visión), oído, `NavMeshAgent`, ataque (20 de daño), aturdimiento EMP de 0,5 s y muerte. **Aparece en runtime** sobre los marcadores `MailaMarka` (Etsaia), igual que en las misiones |
| **Enemigo provisional** (`PF_Etsaia_Proba`) | Maniquí CC0 de Quaternius UAL con material propio y 10 zonas de impacto (cabeza, torso y dos por cada extremidad) |
| **Animator** | `AC_Jokalaria`: capa **"Arma"** (máscara de torso, peso 0 por defecto) con Eutsi, Apuntatu (blend de pitch −45/0/45), Tiro, Birkarga y Kaltea. En las misiones, sin arma, todo queda igual que en A2. `AC_Etsaia_Proba`: Mugimendua, Eraso, Kaltea y Hil |
| **Herramientas** | `BorrokaTresna` (vista previa, aplicar e idempotente), `AnimatorEraikitzailea` (controladores comparados por firma, usado por el jugador y el enemigo), `BatchBorrokaProba` |
| **Escena** | `Eszenak/Proba/04_BorrokaProba.unity`: primitivas, muro, esquina, cajas, 3 marcadores de enemigo y NavMesh generado a partir de los colliders físicos (`NM_04_BorrokaProba.asset`) |

**Cambios en código que ya existía** (aditivos):
- `EsploratzaileKontrola`: `KameraDesplazamendua` (por defecto el del hombro), `BegiradaPitch` y `Kamera`.
- `JokalariBisuala`: refresca sus renderers de malla al añadir el arma, para que en primera persona quede solo la sombra.
- `Eclipse.Editor.asmdef`: referencia a `Unity.AI.Navigation`, que ya estaba instalado.

## 2. Preparado para las fases siguientes (sin implementar)

- **Criaturas definitivas y variantes:** prefabs con `Etsaia` + `NavMeshAgent` + `KalteZonaKolisioa`; se cambia la ruta en `EtsaiSortzailea`. Los parámetros de la máquina de estados (distancia, daño, velocidad) están en el prefab.
- **Desmembramiento:**
  - `GorputzKalteLogika` ya calcula qué parte se rompe y `Etsaia.ZatiaHautsita` emite el evento (comprobado en el test: brazo izquierdo).
  - El trozo visual (`BakeMesh` + ocultar el renderer del brazo + muñón) se colgará de ese evento.
  - La IA podrá cambiar: sin pierna, más lenta; sin brazo, sin ese ataque.
- **NPC (científicos y soldados):** pueden implementar `IKalteHartzailea` si hace falta, y no usan `Etsaia`: no son hostiles. Aparecerán sobre marcadores como los enemigos. Su comportamiento no está implementado.
- **Apuntar con desplazamiento lateral:** `JokalariAnimLogika` ya publica `AbiaduraX` y `AbiaduraZ`. Los clips de Mixamo se añadirían al blend tree.
- **Misiones:** `JokalariArma.Gehitu(kontrola, false)` desde `LevelManager` y un `EtsaiSortzailea` por sector. Se hará **tras tu revisión**.

## 3. Pruebas

| Prueba | Resultado |
|---|---|
| `BorrokaTresna` y `JokalariBisuala` | Vista previa → aplicar → **segunda vista previa: 0 cambios** en las dos |
| EditMode | **74/74** (55 anteriores + 19 de combate) |
| `BatchBorrokaProba` (Play, `04_BorrokaProba`) | **42/42 y 0 errores en el log.** Detalle debajo |
| HypJo (escena 03 y M1/M2/M3) | 54 / 21 / 21 / 21, como en A2 |
| HUD, mapa e inventario | 66 / 89 / 69 |
| Interacción y cámara | 25 × 3; 13 056 vistas sin atravesar paredes |
| Robot | 211 / 395 / 311 m |
| Compilación sin Development | El jugador está incluido y `AutoProbaQA` no |
| Build QA y Release | 57 líneas, 0 fallos; target devuelto a Windows |
| Integridad | 0 cambios en las 5 escenas, lightmaps, `Assets/Settings`, `ProjectSettings` y `Packages` |

**Detalle de `BatchBorrokaProba`:**
- **Configuración:** 3 enemigos creados en runtime en los marcadores, sobre el NavMesh y con 10 zonas cada uno. Pistola en la mano (0,075 m), capa "Arma" activa, 8/24.
- **Apuntado:** cañón alineado con la retícula (0,0°).
- **Daño por zonas:** torso 25 (100 → 75); brazo 3 × 15, que se rompe en el tercer disparo (un solo evento); cabeza ×2, muerte.
- **Al morir:** colliders y agente desactivados.
- **Muro:** el disparo no atraviesa a `Babesa`, y su ruido alerta al enemigo de detrás.
- **Borde del muro:** la cámara ve el objetivo pero la boca está detrás (x 8,70 frente a 8,12), y el impacto queda en el muro.
- **Munición:** vaciado, "hutsik", recarga de 1,5 s (8 en el cargador, reserva −8) y HUD.
- **IA:** persigue por NavMesh hasta 1,03 m, 2 golpes (100 → 60) y flash rojo. El EMP la aturde y muere con 4 disparos al torso.
- **Jugador:** muere y reaparece en el inicio con vida completa. En primera persona, arma y modelo solo proyectan sombra.

## 4. Incidencias corregidas

**Defectos reales:**
- **Prefab del enemigo sin zonas de impacto:** `KalteZonaKolisioa` estaba en un archivo con otro nombre, y Unity no serializa así un `MonoBehaviour`. Ahora tiene su propio archivo. La comprobación idempotente lo detectó.
- **Posible disparo a través de la pared:** si la mano extendida queda dentro de una pared, un raycast que nace dentro de un collider no lo detecta. Ahora se comprueba también el tramo pecho → boca.
- **Sangre y chispas cuadradas:** ahora usan la textura de partícula de Unity. El rayo no, para que no se desvanezca en los extremos.

**Errores de mis tests:**
- Un test comparaba floats exactos; ahora usa tolerancia.
- El primer caso de la esquina colocaba al jugador dentro del muro; se rehízo con el borde de `Babesa`.

## 5. Capturas (escena neutra, revisadas)

**Carpeta:** `docs/qa/kapturak/A3_Borroka/`.
- `neutro_01` empuñando; `02` apuntando; `03` impacto; `04` sangre;
- `05` muerte; `06` muro; `07` borde del muro; `08` recarga;
- `09` ataque; `10` primera persona;
- vídeo `neutro_bideoa_erasoa.mp4` (H.264).

## 6. Limitaciones conocidas

- Sin clips laterales ni de marcha atrás: al apuntar y moverse, las piernas usan los clips de avanzar.
- La sangre es discreta (partículas pequeñas). La retícula es pequeña.
- Enemigo provisional: un maniquí sin aspecto de horror.
- Faltan en este bloque: botiquín y munición recogibles, integración en el HUD de las misiones, muerte en las misiones (reinicio por `LevelManager`) y el encuentro de M2.
- **Efecto secundario de las regresiones:** reescriben capturas versionadas (`HUD_*`, `M*_uholdea`, `A2_Jokalaria/*`).
  - Están respaldadas en `…/regresio_kapturak/` y **no** se han restaurado; lo decides tú.
  - Propuesta para el siguiente bloque: que las pruebas guarden sus capturas en una carpeta por ejecución.
