# ECLIPSE — Estrategia de Claude v2.1 (shooter táctico + inventario ligero)

3 de noviembre de 2026 · Ion Beristain · **Estado:** aprobada como base definitiva (revisión de GPT: 9,5/10), con 4 correcciones técnicas incorporadas en esta revisión 2 (sección 1.1). Es la fuente de verdad.

**Base:** v2.0 completa + encargo `01_PROMPT_ENCARGO_V2_1.md`. Prioridad en conflictos: rúbrica y capturas del profesor > decisiones de Ion y del encargo > v2.0 > metodología histórica > guía transversal.

**Alcance de esta versión:** solo documentación y diseño. No se ha tocado ningún proyecto Unity, repositorio ni videojuego anterior.

---

## 0. Resumen ejecutivo

**ECLIPSE** es un *shooter* táctico de ciencia ficción en tercera persona: exploración, sigilo opcional, combate contra robots, inventario ligero y una única arma, la pistola EMP, con disparo normal y disparo cargado. Dura unos 8–12 minutos.

| Aspecto | Decisión |
| --- | --- |
| Historia | Un técnico queda atrapado en una instalación científica tras un incidente. Recupera una tarjeta, restablece la energía, activa el **Zero Protokoloa** (el protocolo de contención y evacuación de la instalación) y escapa en el ihes-ontzia (nave de escape). Se cuenta con terminales, señales y ambiente, sin cinemáticas largas |
| Bucle | Explorar → encontrar recursos y objetos de misión → evitar o neutralizar amenazas → activar sistemas → abrir acceso → completar el nivel → estadísticas → siguiente nivel |
| Dos formas de jugar | Sigilo y desactivación selectiva, o combate con la EMP. La puntuación premia ambas y nunca obliga a eliminar enemigos |
| Niveles | 3 sectores: **Laborategia** (laboratorio), **Energia** (energía) y **Erreaktorea** (reactor). Cada uno con objetivo, transición, dificultad y una herramienta nueva |
| EMP | Normal: 25 de energía, preciso, aturde. Cargado (mantener 0,9 s y soltar): 60 de energía, destruye drones, aturde torretas el doble de tiempo, empuja objetos con más fuerza y apaga láseres 5 s |
| Inventario | 4 ranuras de consumibles apilables (Botikina, EMP bateria) + sección aparte de objetos de misión (Sarbide-txartela, Fusiblea). Persistencia coherente entre niveles, muertes y reintentos |
| Arquitectura | Unity 6.6 URP limpio. Un único dueño del estado de partida (`GameSession`); reglas en C# puro testeable (`Inbentarioa`, `EMPLogika`, puntuación); MonoBehaviours finos para Unity |
| IA y automatización | Claude Code implementa por módulos con contratos fijados antes de paralelizar; subagentes de QA, revisión, documentación y auditoría; lo repetitivo dentro de Unity, con herramientas de Editor (vista previa, un solo Undo, idempotentes); GPT revisa; Ion decide y prueba en Unity |
| Entregas | **6/11/2026, 14:15**: escenario (sin código de juego). **20/11/2026, 14:15**: juego completo con build de Windows |

## 1. Cambios respecto a v2.0

| Tema | v2.0 | v2.1 | Tipo |
| --- | --- | --- | --- |
| Género | Acción en tercera persona | *Shooter* táctico en tercera persona con sigilo opcional e inventario ligero | Cambia |
| EMP | Un solo disparo | Disparo normal + disparo cargado, con reglas de entrada, coste y efectos distintos | Cambia |
| Inventario | No había ("sin inventario") | 4 ranuras de consumibles + objetos de misión aparte | Se añade |
| Barrera láser | En la tabla de enemigos | Peligro ambiental (`Arriskuak/`), no enemigo | Cambia (pedido por el encargo) |
| Nombres de sector | Laboratorio / Energía / Reactor | Laborategia / Energia / Erreaktorea | Cambia |
| *Protocol Zero* | Nombre alternativo del juego | Nombre del protocolo dentro de la historia (**Zero Protokoloa**); el juego se llama ECLIPSE | Cambia |
| Puntos por neutralizar | +50 cada vez que se aturde una torreta | Puntos de neutralización **una sola vez por enemigo y sector** | Cambia (corrige un exploit de v2.0) |
| Bonus "Fantasma" | Ningún dron te detecta | Ningún enemigo entra en alerta (incluye el aviso de las torretas) | Cambia (en el sector 1 no hay drones) |
| Reintento tras game over | Restaura puntuación y vidas | Restaura **juntos** puntuación, vidas, inventario, objetos de misión y objetos recogidos del sector | Cambia |
| Permisos de Claude | Solo `Assets/_Eclipse/` | Más `docs/` y `LICENSES/`; `Packages/` y `ProjectSettings/` solo con autorización explícita, preflight y diff | Cambia (pedido por el encargo) |
| Copias de seguridad | Git + `git bundle` | 3 capas: remoto con LFS verificado, copia independiente del proyecto y de los paquetes originales, prueba de restauración | Cambia (pedido por el encargo) |
| Estructura de escenas | Una escena; se decide antes del miércoles 4 | Una escena para el 6/11 (se mantiene). Nuevo punto de decisión el 7/11 para el juego, con una recomendación basada en nueva evidencia de Moodle (sección 17) | Cambia el momento; la decisión es de Ion |
| Diseño de encuentros y telemetría | No había | Encuentros por sector y registro local de eventos para equilibrar | Se añade |
| Tabla de controles, diagrama de estados, contratos de snapshot, criterios de aceptación | Parciales | Completos | Se añade |
| Concepto, cámara, proyecto limpio, iluminación híbrida, Compute Light Baker, multiagente ligero, 4 niveles de QA, guía artística, regla de cierre | — | Sin cambios de fondo | Se mantiene |

### 1.1 Correcciones de la revisión 2

| # | Corrección | Secciones |
| --- | --- | --- |
| 1 | Reintentar un sector **recarga la escena** y aplica el snapshot: puertas, paneles, fusibles, terminal, enemigos, checkpoints y cuenta atrás vuelven solos al estado inicial, sin código de reinicio por componente | 5.8, 10.1–10.3, 13.2, 14, 19 |
| 2 | Pasar a 3 escenas no es solo cambiar el `GameManager`: afecta a referencias, iluminación, NavMesh, probes, ajustes de entorno, objetos persistentes y Build Profiles. Reglas de sector autónomo desde el 4/11, comprobadas por el validador | 7, 9, 10.4, 14, 19 |
| 3 | La contingencia del jugador es **Starter Assets: Character Controllers (URP)**, el paquete oficial que recibe las actualizaciones; su compatibilidad con 6000.6 se comprueba en la prueba de kits y el 7/11 | 4, 6, 16, 18 |
| 4 | Automatización de Unity: herramientas de Editor que crean y configuran prefabs, componentes, iluminación y probes en cambios autorizados, con vista previa, un solo Undo, idempotencia e informe | 9.1, 12, 12.1, 13.2, 14, 17, 19 |

## 2. Contexto y restricciones

| Dato | Valor |
| --- | --- |
| Alumno | Ion, 2.º DAM, Tolosaldea LHII, grupo 2AG3. **Trabaja solo**, con Claude Code y ChatGPT |
| Entorno | Fedora 44, Unity 6000.6.0f1 (Unity 6.6), URP, RTX 4060, 32 GB RAM, Git/GitHub |
| Salida | Build Windows x64. Autoextraíble WinRAR SFX desde Fedora solo si hace falta y **después** de probar el build en un Windows real |
| Proyectos protegidos | Space Invaders 2D, Plataformak 2D, Laberintoa Horror 3D. No se tocan |
| Entrega 1 | **Zure eskenatokia osatu**, 6/11/2026 a las 14:15: kit de Internet + escena propia + geometría, materiales, UV, Lightmaps, Reflection Probes y evidencias |
| Entrega 2 | **Eskenatoki barrenean ibiltzen**, 20/11/2026 a las 14:15: videojuego evaluado con la rúbrica de 7 criterios |
| Temario del escenario | Topología, non-manifold, escalas, normales, UV, pivotes, convenciones de nombres, textura única / tileable / trim sheet, luces, skybox, lightmap, reflection probe, light probes |
| Tutoriales de interacción | GameObject y Transform, GUI I y II, Input System y GetAxis, Rigidbody y AddForce, rotar cámara, CharacterController, Instantiate + OnCollisionEnter, puntos con OnTriggerEnter, control del tiempo y fin de juego, animar personaje |
| Idiomas | Juego, interfaz, comentarios y memoria en euskera; explicaciones técnicas a Ion en español |

## 3. La rúbrica y cómo la cubre ECLIPSE

Los 7 criterios son **requisitos confirmados** (captura de Moodle). El inventario, el disparo cargado, el sigilo puntuable y la telemetría son **mejoras voluntarias** de diseño: el profesor no los pide, pero refuerzan "Interakzioa", "Puntuazioa" y "Mailak". La nota depende de que funcionen bien, no de cuántos sistemas haya.

| Criterio (euskera) | 3 – Onargarria | 5 – Bikaina | Cómo lo cubre ECLIPSE |
| --- | --- | --- | --- |
| Hasiera eta amaiera | Ambos existen pero son simples | Excelentes, ligados a la narrativa | Intro del incidente (3 pantallas), activación del Zero Protokoloa, escape en el ihes-ontzia con estadísticas finales; derrota con su propio texto |
| Pertsonaiak eta etsaiak | Aceptables y funcionales | Creativos, variados, interesantes (4: buen diseño y comportamiento claro) | Protagonista animado; torreta y dron con conductas opuestas y avisos legibles; variantes por configuración (torreta giratoria, dron cazador); láser como peligro del escenario |
| Eszenatokia | Funcional y coherente | Creativo, variado, estética y narrativa excelentes (4: detallado, da inmersión) | Composición propia, 3 ambientes de luz, narrativa ambiental con puertas cerradas, paneles de fusibles, terminales y señales |
| Mailak | Dos niveles con algo de progresión (1: un solo nivel) | Buen diseño y gran variedad (4: ≥3 niveles con dificultad creciente) | 3 sectores con objetivo propio, transición, dificultad creciente y una herramienta nueva en cada uno |
| Puntuazioa | Visible pero limitada | Completa, visible, bonus y combos, estadísticas | Datu-nukleoak, neutralizaciones únicas, sigilo, combos, bonus, estadísticas por nivel y récord; sin *farm* |
| Bizitzak | Limitadas pero funcionales (1: no hay) | Completas, con mecánicas adicionales | Salud + escudo regenerable + 3 vidas, invulnerabilidad tras un golpe, checkpoints, vida extra, botiquines y reintento coherente con el inventario |
| Interakzioa | Funcional y básica | Completa, intuitiva, feedback visual y sonoro (4: fluida, acciones variadas) | Apuntar, disparo normal y cargado, inventario, consumibles, recoger, usar objetos de misión, linterna, paneles; cada acción con efecto visual y sonido |

## 4. Decisiones cerradas

| Decisión | Elección | Motivo |
| --- | --- | --- |
| Concepto | Técnico atrapado que escapa por 3 sectores de una instalación científica | Interiores: luz horneada controlada; metal y cristal lucen los probes |
| Género | *Shooter* táctico en tercera persona con sigilo opcional; no RPG, no *survival crafting*, no oleadas | Decisión de Ion (v2.1) |
| Cámara y controlador | Tercera persona; **un solo** controlador validado por prueba: PixelWizards (tiene modo apuntar en 8 direcciones) o, si falla, el paquete oficial Starter Assets: Character Controllers (URP) | No se mezclan dos controladores |
| Arma | Solo la pistola EMP, con modo normal y modo cargado; sin arsenal ni munición balística | Una herramienta profunda mejor que varias superficiales |
| Enemigos | Torreta (`Dorrea`) y dron (`Droia`), con variantes por configuración | Robots: sin animación humanoide |
| Peligros | Barrera láser (`Laserra`) como trampa ambiental | Pedido por el encargo |
| Inventario | 4 ranuras de consumibles + objetos de misión aparte | Decisión de Ion (v2.1) |
| Proyecto | Unity 6000.6 limpio con URP; GATE solo como donante de código si compensa | La entrega del 6/11 no depende de proyectos ajenos |
| Niveles | 6/11: una escena `01_Instalazioa` con 3 sectores autónomos (decisión v2.0). 20/11: punto de decisión el 7/11, ver sección 10.4 | La arquitectura funciona igual con 1 o 3 escenas |
| Iluminación | Híbrida: base horneada en el estado en que se juega cada sector + tiempo real solo para lo que cambia | Una luz horneada no se apaga en juego |
| Bake | Unity Compute Light Baker (a probar el 3/11); Progressive GPU y CPU como reserva | El CPU está deprecado en 6000.6 |
| Persistencia en disco | Solo récord y ajustes en `PlayerPrefs`; sin guardado de partida | Fuera del alcance |
| Fuera del alcance | Jefe final, multijugador, crafting, peso, comercio, equipamiento, guardado de partida | Ninguno aporta a la rúbrica en proporción a su coste |
| Duración | 8–12 minutos | Corto y pulido |

## 5. Diseño jugable (GDD)

Los números son **valores iniciales** que se ajustan con telemetría y pruebas; las reglas y contratos son decisiones.

### 5.1 Sectores: desbloqueos y objetivos

| Sector | Ambiente | Se aprende / desbloquea | Objetivo | Salida |
| --- | --- | --- | --- | --- |
| 1. Laborategia | Blanco y cian, limpio | Controles, apuntado, **EMP normal**, Botikina, Sarbide-txartela, primera torreta, láseres | Encontrar la Sarbide-txartela | Puerta de salida con la tarjeta |
| 2. Energia | Apagón: rojo de emergencia + linterna | **Linterna**, **EMP cargado**, EMP bateria, drones, Fusiblea | Encontrar 3 fusibles y activar los 3 paneles con su etengailua (interruptor) | El sorgailua (generador) vuelve a funcionar; se abre la salida |
| 3. Erreaktorea | Ámbar y naranja, alarmas, humo | Todo combinado + cuenta atrás | Activar el terminal con la Sarbide-txartela (Zero Protokoloa) y llegar al ihes-ontzia | Escape antes de 180 s |

La tarjeta del sector 1 se vuelve a usar en el sector 3: así la persistencia de los objetos de misión entre niveles es visible y tiene sentido narrativo.

### 5.2 Diseño de encuentros

| Encuentro | Qué contiene | Qué enseña o pide | Solución sigilosa | Solución de combate |
| --- | --- | --- | --- | --- |
| L1 Sarrera | Avisos de control, 2 datu-nukleoak | Moverse, mirar, recoger | — | — |
| L2 Lehen dorrea | 1 torreta en un pasillo con cobertura; cajas con Rigidbody | Cobertura, ritmo de la torreta, EMP normal, empujar cajas | Pasar en los huecos de su rastreo | Aturdirla y cruzar |
| L3 Laser korridorea | 2 barreras láser con ciclo, panel de desactivación, Botikina | Leer patrones, usar botiquín | Cruzar en el hueco del ciclo | Desactivar con el panel |
| L4 Txartela | Sala con la tarjeta | Objeto de misión y puerta | — | — |
| E1 Ilunpea | 1 dron patrullando, EMP bateria | Linterna, sigilo, visión del dron | Esquivar su cono y no correr cerca | Dos disparos normales |
| E2 Fusible gelak | 3 salas con fusible, 2 drones entre ellas, aviso de disparo cargado | EMP cargado contra drones, gestión de energía | Rutas alternativas por conductos | Disparo cargado = dron destruido |
| E3 Etengailuak | 3 paneles: fusible + interruptor | Usar objetos de misión | — | — |
| R1 Kontrol gela | Torretas (una giratoria) + láseres | Combinar cobertura y EMP | Rutas por pasarelas | Aturdir torretas |
| R2 Terminala | Terminal con tarjeta | Inicia la cuenta atrás | — | — |
| R3 Ihesa | Torretas + drones (uno cazador) + láseres bajo cuenta atrás | Todo combinado, botiquines y baterías | Correr por la ruta lateral | Abrir paso con cargados |

### 5.3 Controles

Input System con dos *action maps*, **Jokoa** y **UI**. Al abrir el inventario, la pausa o un menú se activa UI y se desactiva Jokoa, así que no hay disparos ni interacciones con la interfaz abierta.

| Acción | Teclado y ratón | Mando | Notas |
| --- | --- | --- | --- |
| Moverse / mirar | WASD / ratón | Stick izquierdo / derecho | — |
| Correr / saltar | Shift / Espacio | L3 / A | No se corre mientras se carga |
| Apuntar | Clic derecho (mantener) | LT | Cámara al hombro, movimiento más lento |
| Disparar / cargar | Clic izquierdo: soltar antes de 0,9 s = normal; mantener ≥ 0,9 s y soltar = cargado | RT | El disparo sale **al soltar**, nunca dos veces |
| Interactuar | E | X | Paneles, puertas, terminal |
| Inventario | Tab | Select | Abre el panel y pausa |
| Uso rápido | 1–4 | Cruceta | Usa la ranura sin abrir el panel, en tiempo real |
| Linterna | F | Y | Desde el sector 2 |
| Pausa | Esc | Start | — |

El remapeo de teclas es opcional y va al final (ejemplo *Rebinding UI* del Input System). Si se quiere enseñar `GetAxis` (tutorial 4), *Active Input Handling* va en **Both**; los controladores elegidos usan el Input System nuevo.

### 5.4 Pistola EMP

Una sola arma con dos modos claramente distintos: el normal es preciso y barato; el cargado es caro y decisivo, y obliga a frenar.

| Regla | Modo normal | Modo cargado |
| --- | --- | --- |
| Entrada | Soltar el clic antes de 0,9 s | Mantener ≥ 0,9 s y soltar |
| Coste | 25 de 100 | 60 de 100 |
| Alcance | 25 m | 25 m |
| Torreta | Aturdida 4 s | Aturdida 8 s (las torretas no se destruyen) |
| Dron | −1 de resistencia (aturdido 2 s) | −2 de resistencia: destruido de un impacto |
| Láser (emisor) | Sin efecto | Apagado 5 s (alternativa temporal al panel) |
| Objeto con Rigidbody | Impulso base (`AddForce`, modo Impulse) | Impulso ×3 |

- **Resistencia del dron:** 2 puntos. Un disparo normal quita 1 y lo aturde; dos normales o un cargado lo destruyen. Se recupera a 2 tras 5 s sin impactos. Es coherente con la v2.0 ("2 impactos: destruido").
- **Carga:** se mueve un 40% más despacio y no se puede correr. Si la energía es menor que 60, la carga se queda en nivel normal (la retícula lo marca con un candado) y al soltar sale un disparo normal. Con menos de 25 no hay disparo: suena un clic vacío.
- **Cancelación:** abrir inventario o pausa, morir o empezar una transición cancela la carga **sin coste**.
- **Cadencia:** 0,25 s mínimo entre disparos.
- **Regeneración:** 20 de energía por segundo tras 1 s sin disparar **ni cargar**.
- **Puntería sin atravesar paredes:** primer raycast desde el centro de la cámara para obtener el punto apuntado; segundo raycast desde la boca de la pistola hasta ese punto. Si algo se interpone, el impacto se produce ahí.
- **Feedback:** anillo de carga en la retícula (cian que pasa a blanco al completarse), zumbido que sube de tono, barra de energía en el HUD con marcas en 25 y 60, rayo fino (normal) o grueso con onda de choque (cargado), retroceso de cámara en el cargado.
- **Física:** solo cajas preparadas (capa `Bultzagarria`, masa fija, interpolación activada) reaccionan al impulso, para no comprometer colisiones ni el CharacterController.

### 5.5 Inventario

**Consumibles: 4 ranuras**

- Cada ranura guarda **una pila de un solo tipo**. Primero se completa la pila existente; si está llena, se usa una ranura libre.
- Pila máxima de 3 para Botikina y para EMP bateria (configurable en `ElementuDatuak`).
- **Inventario lleno:** el objeto no se recoge, sigue en el suelo y aparece el mensaje "Inbentarioa beteta dago".
- **Recogida parcial:** no existe por diseño, porque todo objeto del escenario contiene **1 unidad** (lo comprueba el validador). Si algún día un objeto tuviera más, la regla sería: entra lo que quepa y el resto sigue en el objeto.
- **Uso:** desde el panel (Tab: se elige la ranura y se confirma con E, Enter o clic en "Erabili"), o en tiempo real con las teclas 1–4.
- **Nunca se gasta un objeto sin efecto:** con la salud llena, el botiquín no se usa ("Osasuna beteta dago"); con la energía llena, la batería tampoco ("Energia beteta dago").
- **El panel pausa el juego:** se lee con calma y no se mezclan entradas. El uso táctico en combate son las teclas 1–4, que no pausan. La interfaz del panel usa tiempo sin escalar.

**Objetos de misión: sección aparte, sin ranuras**

- Se registran solos al recogerlos y se ven en una lista con icono y estado.
- Se usan en su dispositivo con E: la puerta del laboratorio y el terminal piden la Sarbide-txartela; cada panel de energía consume un Fusiblea.
- Si falta el objeto, aparece un mensaje claro ("Sarbide-txartela behar da").
- Los **datu-nukleoak** no van al inventario: suman puntos y desaparecen.

### 5.6 Tabla de objetos

| Objeto | Tipo | Efecto | Pila | Dónde aparece |
| --- | --- | --- | --- | --- |
| Botikina | Consumible | +50 de salud sin pasar de 100; no se gasta con la salud llena | 3 | S1 ×1, S2 ×2, S3 ×2 |
| EMP bateria | Consumible | Energía al máximo al instante; no se gasta con la energía llena | 3 | S2 ×2, S3 ×2 |
| Sarbide-txartela | Misión | Abre la salida de Laborategia y activa el terminal de Erreaktorea; no se consume | — | S1 ×1 |
| Fusiblea | Misión | Se consume al activar un panel de energía | — | S2 ×3 |
| Datu-nukleoa | Coleccionable | +100 puntos; no ocupa inventario | — | S1 ×5, S2 ×6, S3 ×6 |

**Por qué la batería no deja sin sentido la regeneración:** pasar de 0 a 60 de energía tarda 1 s + 3 s. La batería da un disparo cargado inmediato en mitad de un encuentro. Fuera de combate no compensa usarla, y hay pocas.

### 5.7 Enemigos y peligros

| Elemento | Estados | Ataque y aviso | Respuesta a la EMP | Puntos | Qué enseña |
| --- | --- | --- | --- | --- | --- |
| Torreta (`Dorrea`) | Inactiva → Rastreo → Aviso → Disparo → Aturdida | Cono de 60°, 15 m; aviso de 0,8 s (luz roja + pitido); proyectil físico cada 1,5 s (Instantiate + Rigidbody + OnCollisionEnter) | Aturdida 4 s (normal) u 8 s (cargado); no se destruye | +50, solo la primera vez | Cobertura y ritmo |
| Torreta giratoria (variante) | Igual, con barrido de 120° | Igual | Igual | +50, solo la primera vez | Leer el barrido |
| Dron (`Droia`) | Patrulla → Sospecha → Persecución → Búsqueda → Patrulla; Aturdido; Destruido | Vista: cono de 90°, 12 m; oído: carrera a menos de 6 m. Sospecha 1 s (luz amarilla); persecución (luz roja); descarga a 2 m (20 de daño). Búsqueda: va a la última posición conocida y busca 5 s | Resistencia 2 (normal −1, cargado −2) | +150 al destruir | Sigilo o enfrentamiento |
| Dron cazador (variante, S3) | Igual | Más rápido (4 m/s) y busca 8 s | Igual | +150 al destruir | Presión bajo la cuenta atrás |
| Barrera láser (`Laserra`, peligro) | Encendida ↔ Apagada (ciclo) → Desactivada | 2 s encendida / 1,5 s apagada; contacto = 25 de daño | Cargado: apagada 5 s | +100 al desactivarla con su panel, una vez | Leer patrones |

**Contratos:**
- `IKaltegarria.JasoKaltea(KalteInfo)`: daño al jugador, con cantidad, tipo (`Proiektila`, `Laserra`, `Deskarga`) y origen. El escudo absorbe primero.
- `IEMPHartzailea.JasoEMP(EMPInfo)`: lo implementan la torreta, el dron, el emisor láser y las cajas (`EMPBultzagarria`). Recibe el modo, el punto de impacto y la dirección.

### 5.8 Jugador: salud, escudo y vidas

| Regla | Valor |
| --- | --- |
| Salud por vida | 100 |
| Escudo | 50; se regenera 10 por segundo tras 4 s sin recibir daño |
| Vidas | 3 al empezar; +1 cada 5.000 puntos (máximo 5) |
| Perder una vida | Reaparece en el último checkpoint con salud, escudo y energía al máximo; invulnerable 2 s con parpadeo; combo a ×1. **Inventario, objetos recogidos y enemigos se quedan como estaban** |
| Checkpoints | Inicio de cada sector + uno intermedio en S2 (tras el primer panel) y en S3 (en el terminal) |
| Muerte durante la cuenta atrás | Reaparece en el checkpoint del terminal y la cuenta atrás vuelve a 180 s; la vida perdida es el coste |
| Game over | Derrota con estadísticas: "Berriz saiatu" (recarga el sector y aplica su snapshot de entrada; reaparece al inicio del sector, sección 10.3) o "Menu nagusia" |

### 5.9 Puntuación

- **Base:** datu-nukleoa +100; neutralizar una torreta +50 (**una vez por torreta y sector**); destruir un dron +150; desactivar un láser con su panel +100 (una vez).
- **Combo:** cada acción que puntúa en menos de 3 s desde la anterior sube el multiplicador (×2, ×3, ×4 como máximo); recibir daño lo reinicia. Como cada enemigo puntúa una sola vez, no hay *farm*.
- **Bonus de fin de sector:** sin daño +500; **Fantasma** (ningún enemigo entra en alerta: ni aviso de torreta ni persecución de dron) ×1,5 sobre los puntos del sector; en S3, segundos restantes ×10.
- **Estadísticas por sector:** tiempo, puntos, datu-nukleoak X/Y, neutralizaciones, detecciones, daño recibido, consumibles usados. Al final: total y récord (`PlayerPrefs`).

### 5.10 Progresión medible (`LevelConfig`)

| Parámetro | Laborategia | Energia | Erreaktorea |
| --- | --- | --- | --- |
| Torretas (giratorias) | 1 (0) | 0 | 3 (1) |
| Drones (cazadores) | 0 | 3 (0) | 3 (1) |
| Barreras láser | 2 | 1 | 3 |
| Intervalo de disparo de torreta | 1,5 s | — | 1,0 s |
| Velocidad del dron | — | 3,0 m/s | 3,0 / 4,0 m/s |
| Botikinak / baterías | 1 / 0 | 2 / 2 | 2 / 2 |
| Datu-nukleoak | 5 | 6 | 6 |
| Límite de tiempo | — | — | 180 s desde el terminal |
| Lo nuevo | EMP normal, tarjeta, botiquín, láser | Oscuridad, linterna, EMP cargado, batería, fusibles | Cuenta atrás, todo combinado |

### 5.11 Victoria y derrota por sector

| Sector | Victoria | Derrota |
| --- | --- | --- |
| Laborategia | Tarjeta recogida + salida | 0 vidas |
| Energia | 3 paneles activos (fusible + interruptor) + salida | 0 vidas |
| Erreaktorea | Terminal activado con la tarjeta + ihes-ontzia antes de 180 s | 0 vidas o tiempo agotado |

### 5.12 Telemetría para equilibrar

Un `TelemetriaErregistroa` escucha `GameEvents` y escribe una línea JSON por evento (encuentro, muerte, daño por origen, disparos normales y cargados, consumibles, detecciones, tiempo por encuentro) en `Application.persistentDataPath`. Solo está activo en builds de desarrollo. Se usa en las pruebas del 15–16/11 para ajustar los valores de esta sección con datos, no a ojo. La pantalla de estadísticas lee de `GameSession`, no de la telemetría.

## 6. Fuentes externas

No existe un juego completo, libre, en Unity 6 y con licencia clara que sirva de base; se usan piezas oficiales y donantes con licencia MIT. Los datos de GATE, Nightfall y Survival Arena están comprobados clonando sus repositorios.

| Fuente | Decisión | Datos verificados |
| --- | --- | --- |
| [PixelWizards simple-character-controller](https://github.com/PixelWizards/simple-character-controller) | Jugador (prueba de 1 h el 7/11) | Código MIT, animaciones Unity Companion; modo libre + modo apuntar con movimiento en 8 direcciones; Cinemachine con colisión; Unity 2022.3.24 |
| [Starter Assets: Character Controllers (URP)](https://assetstore.unity.com/packages/essentials/starter-assets-character-controllers-urp-267961) | Contingencia del jugador | Paquete oficial combinado: las fichas antiguas de Starter Assets ThirdPerson y FirstPerson remiten a él para las actualizaciones. Unity Companion; declarado compatible con 2022.3 LTS y Unity 6 LTS, **no con 6000.6 de forma explícita** (se comprueba el 7/11). Usa CharacterController; no funciona con el Input Manager antiguo. No confundir con `com.unity.charactercontroller`, que es el controlador para Entities (DOTS) |
| Animation Rigging, AI Navigation, Input System | Usar | Paquetes oficiales: pose de apuntado, NavMesh del dron, *action maps* |
| [FPS Microgame](https://assetstore.unity.com/packages/templates/fps-microgame-156015) | Referencia de lógica de torreta, dron y proyectiles | Unity Companion, URP; versión 5.0.0 actualizada para Unity 6 |
| [GATE Unity](https://github.com/JJNCreator/GATEUnity) | Donante opcional de menú, opciones y pausa | MIT (Jake Bayer, 2025); Unity 6000.2.7f2; unas 2.600 líneas; Cinemachine 2.10 y Post-Processing v2 |
| [Nightfall (IUT-Jam)](https://github.com/adruiid/IUT-Jam) | Referencia: torretas, `IDamageable` | Unity 6000.5.1f1, URP; el README dice MIT pero **no hay archivo LICENSE**: no se copia código |
| [Poly Haven](https://polyhaven.com) | Texturas en superficies grandes y HDRI | CC0; sustitución selectiva para no romper atlas del kit |
| [Unity Compute Light Baker](https://docs.unity3d.com/Manual/unity-compute-light-baker.html) | Usar (probar el 3/11) | Opcional en 6000.6, recomendado frente al CPU deprecado; sin elección de GPU ni vista previa interactiva |
| Freesound | Audio real | Licencia de cada sonido citada en `LICENSES/` |
| Survival Arena, Abdulsamed00/Gameplay, Kyrevant, Hide and Seek 3D, Zombie Survival, rob1997, Invector, Game Creator 2, JU TPS, TACT-OPS | Descartados | Trabajo de clase ajeno, sin licencia, demasiadas dependencias o de pago |
| Copias "gratis" de assets de pago | Prohibido | Piratería; los scripts de Editor se ejecutan al importar |
| Unity AI / MCP | Nada antes del 6/11 | Beta |

## 7. Entrega del 6/11: el escenario

El 6/11 se entrega el escenario; **no se programa inventario ni disparos antes de esa fecha**. Lo que sí se hace es dejar el espacio preparado para el juego.

**Kits candidatos** (decide la prueba en Unity, no la ficha):

| Kit | Precio | Datos de la ficha | Papel |
| --- | --- | --- | --- |
| [Sci-Fi Facility Kit 2](https://www.fab.com/listings/ded3e278-dc90-43f5-a0a6-da192895fdbe) (Fab) | De pago; GPT encontró $34.99 en septiembre de 2026, sin confirmar | 76 piezas, rejilla de 2 m, pared de 3 m, 3 LODs, colliders convexos, UV de lightmap, URP 14+, escena de ejemplo horneada; al importar ofrece aplicar sus ajustes de proyecto | Favorito si el precio es asumible |
| [3D Free Modular Kit](https://assetstore.unity.com/packages/3d/environments/3d-free-modular-kit-85732) (Barking Dog) | Gratis | Versión 3.5 del 25/09/2026; URP en 6000.3 y 6000.7 | Favorito gratis |
| Modular SciFi Pack, 146 objects, LowPoly | Gratis | URP, 146 prefabs, 58,9 MB | Candidato; low-poly va contra el realismo |
| 3D Scifi Kit Starter (Creepy Cat) | Gratis | Cuelgue conocido en Unity 6 con DirectX 12 (se arregla con DirectX 11) | Plan B |

**Protocolo de prueba** (máximo 90 minutos por kit, en un proyecto desechable):

1. Importar sin aceptar ajustes de proyecto automáticos.
2. Comprobar que no quedan materiales rosas en 30 minutos.
3. Montar una sala de 6×6 m y un pasillo; meter la cápsula de Starter Assets para comprobar escala y pasillos de 3–4 m (cámara al hombro).
4. Bake de prueba con Compute Light Baker; buscar artefactos de UV2.
5. Poner un Reflection Probe y mirar el resultado en la Game view.
6. Descartar el kit al primer fallo grave.

**Preparar el espacio para un shooter (sin código):** coberturas a media altura en cada encuentro, líneas de visión de torretas comprobadas con la cámara al hombro, circuitos cerrados para los drones, salas de fusibles separadas, conductos como rutas sigilosas. Se marcan con GameObjects vacíos con nombre (`Spawn_Jokalaria`, `Spawn_Dorrea_01`, `Pickup_Botikina_01`, `Panela_01`, `Checkpoint_S2`), que a partir del 8/11 sustituye por prefabs la herramienta `MarkatzaileOrdezkatzailea` (sección 12.1).

**Sectores autónomos desde el primer día** (sección 10.4): cada sector vive en su raíz (`Sektorea1`, `Sektorea2`, `Sektorea3`) con su geometría, luces, probes, Volume, ambiente y marcadores; nada fuera de las raíces salvo la cámara de trabajo, y ningún objeto de un sector apunta a otro. Así el 7/11 se puede pasar a 3 escenas sin rehacer nada.

**Temario a evidencia:**

| Tema | Qué se hace | Evidencia |
| --- | --- | --- |
| Topología, non-manifold, normales, pivotes, UV | Una pieza propia revisada en Blender | Capturas antes/después |
| Escalas | 1 unidad = 1 m, comprobado con la cápsula de 1,8 m | Captura |
| Convenciones de nombres | Prefijos `SM_`, `M_`, `T_`, `PF_` y jerarquía ordenada | Project + Hierarchy |
| Textura única / tileable / trim sheet | Pieza protagonista con textura propia; suelos de Poly Haven; trim del kit | Inspector de materiales |
| Luces y skybox | Baked y Mixed, emisivos, temperaturas frías y cálidas | Escena + ajustes |
| Lightmap | Bake con rebote | Con y sin bake |
| Reflection Probe | Una por sala, con Box Projection | Suelo metálico antes/después |
| Light Probes | Rejilla en zonas transitables | Vídeo de luz a sombra |

**Calendario:**

| Día | Trabajo | Criterio de "hecho" |
| --- | --- | --- |
| Mar 3 | Enunciado completo y ZIP del profesor; proyecto y repo; prueba de kits y del Compute Light Baker | Kit elegido |
| Mié 4 | Distribución de los 3 sectores con coberturas, rutas y marcadores; sector 1 primero y con máxima calidad | La cápsula lo recorre todo sin atascarse; cada sector en su raíz autónoma |
| Jue 5 | Bake final, probes, post-procesado, decals, pieza de Blender, capturas, memoria | Congelado a las 22:00 |
| Vie 6 | Comprobación y entrega antes de las 12:00 | Etiqueta `entrega-1` |

**Pista paralela opcional (4–6/11, sin tocar Unity):** Claude puede redactar los contratos (sección 9.2) y la lógica en C# puro con sus tests en una carpeta aparte del repositorio (`logika-zirriborroa/`, .NET con NUnit, C# 9 para coincidir con Unity). Se revisa el 7/11 y se mueve a Unity. Solo si no le quita a Ion atención del escenario. Requiere el SDK de .NET en Fedora; instalarlo es un cambio del sistema que tiene que aprobar Ion.

## 8. Entrega del 20/11: el juego, por dependencias

Primero se fijan los contratos; después se implementan en paralelo los módulos que no comparten archivos; la integración en Unity es secuencial y la valida Ion.

| Días | Módulo | Depende de | Claude / Ion |
| --- | --- | --- | --- |
| 7 | **Decisión de escenas** (sección 10.4). Contratos fijados. Prueba de PixelWizards (1 h). Lógica pura integrada con sus `.asmdef` | Escenario entregado | Claude: contratos y lógica; Ion: decisiones y controlador |
| 7–8 | `GameSession`, `Inbentarioa`, `EMPLogika`, puntuación, vidas, con tests EditMode | Contratos | Claude, en paralelo por `git worktree` |
| 8–9 | EMP en Unity: raycast + comprobación de línea de visión, carga, retícula, receptores, cajas | `EMPLogika`, controlador | Claude programa; Ion monta y prueba |
| 9 | Objetos recogibles, inventario (panel + teclas 1–4), objetos de misión, puertas, paneles, terminal | `Inbentarioa` | Claude programa; Ion monta prefabs y UI |
| 10 | Torreta, dron (máquina de estados + NavMesh), láser | Receptores EMP, `IKaltegarria` | Claude programa; Ion coloca y ajusta |
| 10, noche | **Punto de decisión:** si el apuntado no queda bien, la EMP pasa a pulso de área en los dos modos | — | Ion |
| 11–12 | Transiciones de sector, snapshot y reintento, luz de los sectores 2 y 3 (cambios en tiempo real) | `GameSession`, `LevelManager` | Claude programa; Ion hace bakes |
| 13–14 | Menú, intro, final, estadísticas, pausa, audio, efectos, telemetría | Todo lo anterior | Claude programa; Ion elige audio y valida |
| 15–16 | Pruebas con alguien de fuera + telemetría, equilibrio, bugs. **Congelado el lunes 16** | — | Ion juega; Claude corrige |
| 17 | Build de Windows y prueba en otro PC; SFX solo si hace falta, después del build probado | — | Ion |
| 18–19 | Memoria en euskera siguiendo la rúbrica + vídeo de 2 minutos; entrega el jueves | — | Claude redacta; Ion revisa |

## 9. Arquitectura técnica

### 9.1 Carpetas

```text
Assets/_Eclipse/
├── Eszenak/        00_Hasiera · 01_Instalazioa (o 01_Laborategia / 02_Energia / 03_Erreaktorea)
├── Scriptak/
│   ├── Core/          GameManager, GameSession, GameEvents, LevelManager, LevelConfig
│   ├── Jokalaria/     Bizitzak, Ezkutua, JokalariKontsumitzailea, Linterna
│   ├── EMP/           EMPLogika, EMPArma, IEMPHartzailea, EMPBultzagarria
│   ├── Inbentarioa/   Inbentarioa, ElementuDatuak, ElementuBiltzailea, InbentarioArgazkia
│   ├── Puntuazioa/    PuntuazioLogika
│   ├── Etsaiak/       Dorrea, Droia, IKaltegarria
│   ├── Arriskuak/     Laserra
│   ├── Interakzioak/  Atea, Panela, Terminala, Checkpoint, DatuNukleoa
│   ├── UI/            HUD, InbentarioUI, Menua, Estatistikak, Pausa, Mezuak
│   └── Telemetria/    TelemetriaErregistroa
├── Datuak/         Elementuak/ (4 ElementuDatuak) · Mailak/ (3 LevelConfig) · Argiztapena/ (3 perfiles + ECLIPSE_Argiztapena.lighting)
├── Prefabak/       PF_Iraunkorra (GameManager, HUD, InbentarioUI, EventSystem), PF_Jokalaria, enemigos, objetos, interacciones
├── Editor/         EszenaBalidatzailea, EbidentziaKapturak, BuildScript
│   └── Tresnak/       ElementuDatuSortzailea, PrefabSortzailea, MarkatzaileOrdezkatzailea, IdEsleitzailea,
│                      ArgiztapenPreseta, ProbeJartzailea, UV2Konfiguratzailea, ProiektuEzarpenak (sección 12.1)
└── Tests/          EditMode/ · PlayMode/
ThirdParty/         kit, controlador elegido (solo lectura)
LICENSES/           MIT (GATE si se usa, PixelWizards), Unity Companion, CC0, Freesound
docs/               fases, qa, memoria
```

Ensamblados: `Eclipse.Runtime`, `Eclipse.Editor`, `Eclipse.Tests.EditMode`, `Eclipse.Tests.PlayMode`. La lógica pura no depende de `MonoBehaviour`.

### 9.2 Contratos: quién es dueño de qué

Hay un único dueño del estado de partida: `GameSession`. El resto lee, pide o avisa.

| Elemento | Tipo | Dueño del estado | Responsabilidad | Por qué existe (KISS) |
| --- | --- | --- | --- | --- |
| `GameManager` | MonoBehaviour, Singleton, dentro de `PF_Iraunkorra` (`DontDestroyOnLoad`) | La instancia de `GameSession` | Flujo global, carga de sectores o escenas, reintento | Patrón Singleton visto en el curso; persistente con 1 o 3 escenas |
| `GameSession` | C# puro | Puntos, vidas, `Inbentarioa`, ids recogidos, ids premiados, snapshot del sector | Reglas de partida y snapshot | Un solo dueño evita estados duplicados; se testea sin Unity |
| `Inbentarioa` | C# puro, dentro de `GameSession` | 4 ranuras + objetos de misión | `Gehitu`, `Erabili`, `Badago`, `Kendu`, `Argazkia`, `Berrezarri` con resultados explícitos | Reglas e invariantes testeables |
| `ElementuDatuak` | ScriptableObject inmutable | — | Id estable, tipo, nombre e icono, pila máxima, efecto y valor | Datos editables sin tocar código |
| `ElementuBiltzailea` | MonoBehaviour | Su id y si ya se recogió | Recogida segura y única | Evita duplicados por reentradas de trigger |
| `IKontsumitzailea` | Interfaz | — | Aplicar efectos al jugador (`SaludaBehar`, `SaludaGehitu`, `EnergiaBehar`, `EnergiaBete`) | `Inbentarioa` no conoce al jugador; se testea con uno falso |
| `InbentarioUI` | MonoBehaviour | Ninguno (vista) | Mostrar el estado y enviar peticiones | La interfaz no contiene reglas |
| `EMPLogika` | C# puro | Energía, estado de carga, tiempos | Costes, umbral de carga, regeneración, cancelación, cadencia | El arma se testea sin escena |
| `EMPArma` | MonoBehaviour | Ninguno (usa `EMPLogika`) | Entrada, raycasts, efectos, sonido | Unity solo en la capa fina |
| `IEMPHartzailea` | Interfaz | — | Recibir la EMP | Un solo punto de contacto para torreta, dron, láser y cajas |
| `IKaltegarria` | Interfaz | — | Recibir daño | Contrato de daño de la v2.0 |
| `PuntuazioLogika` | C# puro, dentro de `GameSession` | Combo, ids premiados | Puntos, combo, bonus, antifarm | Testeable |
| `LevelManager` | MonoBehaviour, uno por sector | Estado del sector | Entrada, objetivos, salida, snapshot al entrar; crea al jugador en `Spawn_Jokalaria` | Funciona igual con 1 o 3 escenas |
| `LevelConfig` | ScriptableObject | — | Dificultad y objetivos | Progresión medible |
| `GameEvents` | Clase estática con unos 12 eventos tipados | — | Avisos entre sistemas | Sin bus genérico ni sistema de mensajes propio |
| `TelemetriaErregistroa` | C# + escritura a archivo | Buffer | Registro para equilibrar | Solo en desarrollo |

**Eventos de `GameEvents`:** `PuntuakAldatuta`, `BizitzakAldatuta`, `OsasunaAldatuta`, `EnergiaAldatuta`, `InbentarioaAldatuta`, `MisioElementuaLortuta`, `HelburuaBeteta`, `SektoreaHasi`, `SektoreaAmaitu`, `JokalariaHil`, `EtsaiaNeutralizatuta`, `Detektatuta`.

**Resultados explícitos** (sin excepciones para el flujo normal, sin `null` ambiguos):
- `Gehitu` → `Onartua` | `Beteta` | `Baliogabea`.
- `Erabili` → `Erabilia` | `EzDaBeharrezkoa` | `Hutsik`.
- `Kendu` → `true` solo si había suficiente; si no, **no cambia nada**.

Se valida siempre antes de modificar, y nunca hay cantidades negativas ni por encima del máximo.

## 10. Arquitectura de ejecución: estados, persistencia y snapshots

### 10.1 Flujo de estados

```text
Menua ──► Intro ──► Sektorea 1 ──► Estatistikak ──► Sektorea 2 ──► Estatistikak ──► Sektorea 3 ──► Amaiera ──► Menua
                        │                               │                               │
                        └──────────── GameOver ◄────────┴───────────────────────────────┘
                                        ├── Berriz saiatu ──► recarga la escena del sector + snapshot de entrada (10.3)
                                        └── Menu nagusia

Dentro de cada sector:
  Sarrera (título "N. Maila") ──► Jokatzen ──► Helburua beteta ──► Irteera ──► Estatistikak
                                   │   ▲
                                   ▼   │
                         Heriotza ──► Checkpoint (−1 vida; si quedan 0 ──► GameOver)

Por encima de Jokatzen, sin cambiar de estado: Pausa · Inbentarioa (ambos pausan y activan el action map UI)
```

### 10.2 Qué se conserva y qué se reinicia

| Dato | Al pasar de sector | Al perder una vida | Al reintentar tras game over |
| --- | --- | --- | --- |
| Puntuación | Se conserva | Se conserva | La de la entrada del sector |
| Vidas | Se conservan | −1 | Las de la entrada del sector (mínimo 3) |
| Salud, escudo, energía EMP | Al máximo | Al máximo | Al máximo |
| Combo | ×1 | ×1 | ×1 |
| Consumibles del inventario | Se conservan | **Se conservan** | Los de la entrada del sector |
| Objetos de misión | Se conservan | Se conservan | Los de la entrada del sector |
| Objetos ya recogidos del sector | — | Siguen recogidos | Vuelven a aparecer los que se recogieron en este intento |
| Neutralizaciones ya premiadas (`saritutakoIdak`) | Se conservan | Se conservan | Las de la entrada del sector |
| Enemigos (posición, estado, destruidos) | Los del nuevo sector | Siguen como estaban | Estado inicial (recarga) |
| Puertas, paneles, fusibles colocados, láseres, terminal, cajas | Los del nuevo sector | Siguen como estaban | Estado inicial (recarga) |
| Checkpoint activo | Inicio del nuevo sector | El último alcanzado | Inicio del sector |
| Cuenta atrás de S3 | — | Vuelve a 180 s en el checkpoint del terminal | Parada; empieza otra vez al activar el terminal |
| Récord | Se guarda al terminar o al perder | — | — |

Con estas reglas no se puede *farmear*: los objetos solo reaparecen en un reintento, y el reintento devuelve a la vez puntos, inventario, objetos y mundo al estado de entrada.

### 10.3 Snapshot y antiduplicación

**`SektoreArgazkia`** (en memoria, nunca en disco) se toma al empezar cada sector y contiene:
- `puntuak`, `bizitzak`;
- copia del `Inbentarioa` (ranuras con id y cantidad + objetos de misión con id y cantidad);
- copia de `bildutakoIdak` (ids de objetos recogidos);
- copia de `saritutakoIdak` (enemigos y láseres ya premiados).

**Ids estables:** cada `ElementuBiltzailea` y cada enemigo tienen un id único y fijo (asignado en el Editor, por ejemplo `S2_Fusiblea_03`). El validador rechaza ids repetidos en todas las escenas: es la "clave de negocio" del proyecto.

**Recogida segura (`ElementuBiltzailea`):**
1. Si ya está marcado como recogido, sale sin hacer nada (reentrada de trigger).
2. Pide `Gehitu` a `GameSession`.
3. Solo si es `Onartua`: lo marca, desactiva el collider, registra su id en `bildutakoIdak` y se desactiva con `SetActive(false)` (no se destruye, para poder reaparecer en un reintento).
4. Si es `Beteta`: muestra el mensaje y sigue en el suelo.

Al entrar en un sector, cada objeto consulta `bildutakoIdak` y se desactiva si ya estaba recogido.

**Reinicio del mundo al reintentar (corrección 1).** "Berriz saiatu" no restaura el mundo pieza a pieza: **recarga la escena del sector**. Con una escena se recarga `01_Instalazioa` entera y se activa la raíz del sector actual; con tres, solo la escena de ese sector.

1. `GameSession.Berrezarri(argazkia)`: puntos, vidas (mínimo 3), inventario, objetos de misión, `bildutakoIdak` y `saritutakoIdak`; combo ×1; bonus del sector (sin daño, Fantasma) a cero.
2. Fundido a negro y `SceneManager.LoadSceneAsync` de la escena del sector. Todo lo que vive en la escena (puertas, paneles y fusibles colocados, láseres, terminal, enemigos, cajas, checkpoints, cuenta atrás) vuelve al estado con el que se diseñó en el Editor. **No hay código de reinicio por componente** (ningún `Berrezarri()` en puertas o enemigos), así que no se puede olvidar ninguno.
3. En `Start`, cada `ElementuBiltzailea` consulta `bildutakoIdak`, ya restaurado: solo siguen ocultos los que se recogieron antes de entrar al sector.
4. El jugador aparece en el inicio del sector, no en el checkpoint intermedio, y el `LevelManager` vuelve a tomar el snapshot de entrada (idéntico al restaurado).

Requisitos para que funcione:
- El estado del mundo vive solo en la escena y el de la partida solo en `GameSession`. Ningún objeto de escena guarda estado en campos `static`.
- Las salidas de sector son de un solo sentido y se cierran detrás del jugador: nunca se vuelve a un sector anterior, así que no hace falta guardar su estado.
- Las suscripciones a `GameEvents` se hacen en `OnEnable` y se quitan en `OnDisable`; si no, tras una recarga los eventos llamarían a objetos destruidos. Si algún día se desactiva la recarga de dominio al entrar en Play, `GameEvents` se limpia con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- `PF_Iraunkorra` comprueba en `Awake` si ya existe una instancia y destruye la copia. Para probar un sector directamente desde el Editor, el `LevelManager` lo crea si no existe.
- Perder una vida **no** recarga nada (sección 5.8): solo lleva al jugador al checkpoint.
- Con una sola escena, la recarga incluye los lightmaps de los 3 sectores: unos segundos tapados por el fundido. Se mide el 11/11.

### 10.4 Decisión de escenas y reglas de sector autónomo

- **6/11:** una escena `01_Instalazioa` con raíces `Sektorea1`, `Sektorea2` y `Sektorea3` (decisión v2.0, sin cambios).
- **7/11, punto de decisión para el juego:** si el profesor confirma que 3 sectores con transición cuentan como 3 niveles, se queda una escena. Si no responde, **Claude recomienda pasar a 3 escenas** por la evidencia de la sección 17, punto 1. **La decisión es de Ion.**

**Corrección 2: cambiar a 3 escenas no es solo cambiar el `GameManager`.** La lógica de juego no cambia (solo el `SektoreKargatzailea`, que activa una raíz o carga una escena), pero la escena sí:

| Aspecto | 1 escena | 3 escenas | Regla desde el 4/11 |
| --- | --- | --- | --- |
| Referencias entre sectores | Funcionan, y por eso es fácil crearlas sin querer | Unity no guarda referencias entre escenas: se pierden | Prohibidas. Los sectores se comunican solo con `GameEvents`, `GameSession` e ids. El validador da ERROR |
| Lightmaps y probes | Un bake con los 3 sectores | Un bake por escena; lightmaps, Light Probes y Reflection Probes van con cada escena | Probes dentro de la raíz de su sector. Un solo asset `ECLIPSE_Argiztapena` (`LightingSettings`) asignado a cada escena |
| Entorno (skybox, luz ambiental, niebla) | Uno para toda la escena; las diferencias, con perfiles en tiempo real | Los de la **escena activa**: tras cargar un sector en aditivo hay que llamar a `SceneManager.SetActiveScene` | `ArgiztapenPreseta` los aplica desde el perfil del sector |
| NavMesh | Superficie global o por sector | Cada escena necesita la suya | Un `NavMeshSurface` (AI Navigation) por raíz de sector, nunca uno global |
| Volume y ambiente sonoro | Por sector, activados con su raíz | Igual | Dentro de la raíz de cada sector |
| Objetos persistentes | `GameManager`, HUD, `InbentarioUI`, `EventSystem` | No pueden vivir en una escena de sector | Todos en `PF_Iraunkorra`, nunca dentro de un sector |
| Jugador | Uno que se mueve de sector | Uno por escena o persistente | Desechable en los dos casos: el `LevelManager` crea `PF_Jokalaria` en `Spawn_Jokalaria`; su estado vive en `GameSession`. La cámara (Cinemachine) se asigna al crearlo |
| Build Profiles | `00_Hasiera`, `01_Instalazioa` | `00_Hasiera`, `01_Laborategia`, `02_Energia`, `03_Erreaktorea` | `ProiektuEzarpenak` actualiza la lista; el validador comprueba que están todas |
| Reintento (10.3) | Recarga `01_Instalazioa` y activa la raíz actual | Recarga solo la escena del sector | El jugador no nota la diferencia |

**Cómo se cambia, si Ion lo decide el 7/11:**
1. Commit y etiqueta `eszena-bakarra`.
2. Abrir `01_Instalazioa` y tres escenas nuevas a la vez (multi-escena); Ion arrastra cada raíz de sector a su escena en el Hierarchy. Sin tocar YAML.
3. `ArgiztapenPreseta` en cada escena; `NavMeshSurface` horneado por escena.
4. Validador: 0 referencias entre sectores y 0 objetos estáticos fuera de las raíces.
5. Tres bakes (de noche si hace falta), Build Profiles y prueba de paso 1 → 2 → 3 y de reintento en cada sector.

**Coste estimado:** medio día de Ion más 3 bakes, si se respetan las reglas desde el 4/11. Es barato el 7/11 porque aún no hay enemigos ni objetos colocados; cada día después lo encarece.

### 10.5 Recuperación

Etiqueta Git por hito (`entrega-1`, `inbentarioa-ok`, `sektorea-2-ok`…) y copias según la sección 13.3.

## 11. Diseño técnico de iluminación

Cada sector se hornea en el estado en que se juega casi todo el tiempo. Los cambios se hacen con luces en tiempo real, emisivos y post-procesado.

| Sector | Horneado | Tiempo real | Cambio de estado |
| --- | --- | --- | --- |
| Laborategia | Luces de techo y paneles emisivos | Ninguna | Ninguno |
| Energia | Apagón: luces rojas de emergencia, emisivos apagados | Linterna del jugador (spot con sombras) | Al activar los 3 paneles: 3–4 luces en tiempo real se encienden, cambian los emisivos y el Volume pasa de rojo a neutro en 2 s. La luz indirecta no cambia; el jugador sale enseguida |
| Erreaktorea | Base naranja y luces de servicio | 2–4 balizas giratorias sin sombras, intensidad pulsante | Al activar el terminal: balizas más rápidas, Volume más saturado, sirena |

- Forward+ de URP, para que varias luces en tiempo real por zona no penalicen.
- Light Probe Groups (lo que enseña el curso), más densos donde cambia la luz y en zonas de combate.
- Reflection Probes horneados por sala, con Box Projection; en Energia, en estado de apagón.
- Perfiles `ArgiztapenProfila` por sector (niebla, Volume, balizas), aplicados por un `ArgiztapenKontrolatzailea`. Claude ajusta perfiles, nunca luces sueltas.
- Efectos de la EMP con partículas y emisivos. Como mucho, una luz puntual muy breve y sin sombras en el impacto del disparo cargado.
- El validador comprueba el modo de cada luz contra esta tabla, *Contribute GI* y UV2 en la geometría estática, y un Reflection Probe por sala.
- `EbidentziaKapturak` saca capturas desde cámaras fijas por sector para comparar y para la memoria.

## 12. Arquitectura multiagente

Lo que frena el proyecto no es escribir código: es Unity (una instancia por proyecto) y la validación de Ion. Se paraleliza lo que no comparte archivos; la integración es secuencial.

| Agente | Qué hace | Puede escribir | No puede tocar |
| --- | --- | --- | --- |
| Implementador (Claude Code, sesión principal) | Mecánicas, herramientas de Editor, integración | `Assets/_Eclipse/Scriptak/`, `Assets/_Eclipse/Editor/`, `Assets/_Eclipse/Datuak/` (con herramientas) | Escenas, prefabs, `.meta` a mano, `ThirdParty/` |
| QA (subagente) | Escribe y lanza tests, lee el informe del validador | `Assets/_Eclipse/Tests/` | Código de juego |
| Revisor (subagente, solo lectura) | Revisa cada diff contra `CLAUDE.md`, la API de Unity 6, el GDD y los contratos | Nada | Todo |
| Docs (subagente) | Memoria en euskera, matriz rúbrica → evidencia, licencias | `docs/`, `LICENSES/` | Código y Assets |
| Auditor de terceros (subagente, solo lectura) | Revisa repos y paquetes antes de abrirlos | Nada | El proyecto |
| GPT (externo) | Revisión independiente al cerrar cada módulo | — | — |
| Ion | Escenas, prefabs, UI, luces, bakes, audio; prueba en Play; aprueba merges | Todo | — |

**Trabajo en paralelo realista (7–9/11):**

| Worktree | Módulo | Archivos | Tests |
| --- | --- | --- | --- |
| A | `Inbentarioa` + `ElementuDatuak` + `IKontsumitzailea` | `Scriptak/Inbentarioa/` | `InbentarioTests` |
| B | `EMPLogika` + `IEMPHartzailea` | `Scriptak/EMP/` | `EMPLogikaTests` |
| C | `GameSession` + `PuntuazioLogika` + vidas | `Scriptak/Core/`, `Scriptak/Puntuazioa/` | `GameSessionTests`, `PuntuazioTests`, `BizitzaTests` |
| D | Validador (ids únicos, cantidades, luces) | `Editor/` | Ejecución sobre una escena de prueba |

**Reglas de integración:**
1. Interfaces y eventos se congelan antes de abrir los worktrees.
2. Merge de uno en uno; después de cada uno, tests EditMode + validador; si algo falla, se revierte ese merge.
3. Escenas, prefabs y `.meta` solo los toca Ion en Unity, o las herramientas de Editor de la sección 12.1, ejecutadas por él o por un agente en batchmode con Unity cerrado. Nunca dos a la vez.
4. Ningún agente hace push; los merges a `main` los aprueba Ion.

**Qué se delega en Claude:** lógica pura y tests, MonoBehaviours finos, herramientas de Editor (sección 12.1), telemetría, borradores de documentación.
**Qué no se delega:** distribución del escenario, colocación final de luces y bakes, sensación de control y apuntado, elección de audio, aceptación final.

### 12.1 Automatización de Unity con herramientas de Editor (corrección 4)

Lo repetitivo o masivo dentro de Unity (crear prefabs, añadir componentes, configurar luces y probes, asignar ids, sustituir marcadores) lo hace una **herramienta de Editor en C#** que escribe Claude, menú `ECLIPSE/…`, nunca editando YAML. Solo se aplica a **cambios autorizados**: cada herramienta declara su alcance y no toca nada fuera de él.

**Cuándo merece una herramienta:** si la tarea se repite 3 veces o más, o toca más de unos 10 objetos. Si es una vez y con pocos objetos, Ion lo hace a mano: es más rápido y no deja código que mantener.

| Herramienta | Qué hace | Toca | Ctrl+Z | Cuándo |
| --- | --- | --- | --- | --- |
| `ElementuDatuSortzailea` | Crea los 4 `ElementuDatuak` y los 3 `LevelConfig` con los valores del GDD | `Datuak/` | No: Git | 7/11 |
| `PrefabSortzailea` | Crea prefabs de juego desde una plantilla: modelo del kit + componentes + collider + capa + datos (`PF_Dorrea`, `PF_Droia`, `PF_Botikina`, `PF_EMPBateria`, `PF_Fusiblea`, `PF_Panela`, `PF_Atea`, `PF_Checkpoint`, `PF_DatuNukleoa`) | `Prefabak/` | No: Git | 8–10/11, a medida que existen los scripts |
| `MarkatzaileOrdezkatzailea` | Sustituye los marcadores del 6/11 por instancias del prefab en la misma posición y rotación; el id sale del nombre del marcador | Escena, sector seleccionado | Sí | 9–10/11 |
| `IdEsleitzailea` | Pone un id estable (`S2_Fusiblea_03`) a objetos y enemigos sin id; nunca cambia uno existente | Escena | Sí | Tras colocar objetos |
| `ArgiztapenPreseta` | Flags estáticos y *Contribute GI* en la geometría, modo de cada luz según la sección 11, `ECLIPSE_Argiztapena`, entorno y perfil del sector | Escena + asset de iluminación | Escena sí; asset no | Antes de cada bake |
| `ProbeJartzailea` | Un Reflection Probe por sala (Box Projection, tamaño a partir de un volumen `Gela_*`) y rejilla de Light Probes en zonas de combate y cambios de luz | Escena | Sí | Antes de cada bake |
| `UV2Konfiguratzailea` | Activa *Generate Lightmap UVs* en los modelos del kit sin UV2 | `.meta` en `ThirdParty/` | No: Git | Solo si el bake de prueba muestra artefactos y con autorización (sección 17, punto 13) |
| `ProiektuEzarpenak` | Capas, etiquetas y lista de escenas | `ProjectSettings/` | No: Git | Con autorización de Ion y commit separado |
| `EszenaBalidatzailea` · `EbidentziaKapturak` | Solo leen; escriben informe o capturas | `docs/` | — | Antes de merges y bakes; hitos |

**Patrón obligatorio de cada herramienta:**
1. **Vista previa por defecto:** calcula los cambios y los muestra en un informe sin modificar nada.
2. **Aplicar** en un solo grupo de Undo (`Undo.SetCurrentGroupName`, `Undo.RegisterCreatedObjectUndo`, `Undo.AddComponent`, `Undo.RecordObject` y `Undo.CollapseUndoOperations` al final): un Ctrl+Z deshace toda la operación.
3. **Idempotente:** busca por id o nombre antes de crear; la segunda ejecución informa "0 cambios" y no duplica nada.
4. **Alcance acotado:** solo la raíz del sector seleccionado o la carpeta indicada.
5. **Informe** en `docs/qa/tresnak/<herramienta>_<fecha>.md`: qué creó, cambió u omitió, y por qué.
6. Después: validador → Ion revisa en la escena y en Play → commit.

**Límites que hay que conocer:**
- Undo cubre los cambios en escenas abiertas. Los cambios en archivos del proyecto (prefabs guardados con `PrefabUtility.SaveAsPrefabAsset`, ScriptableObjects, `.meta`, `ProjectSettings/`) **no** se deshacen con Ctrl+Z: los protege el commit previo obligatorio y se revierten con Git.
- Las instancias se crean con `PrefabUtility.InstantiatePrefab`, no con `Instantiate`, para que sigan vinculadas al prefab.
- En modo interactivo la herramienta marca la escena como modificada, pero no la guarda: guarda Ion. En batchmode (`Unity -batchmode -quit -projectPath <ruta> -executeMethod Eclipse.EditorTresnak.<Clase>.<Metodo> -logFile <log>`) guarda solo si no es vista previa.
- Batchmode solo con Unity cerrado para ese proyecto o worktree. Cada worktree tiene su propia `Library/` y la primera importación tarda; por eso los agentes en paralelo escriben código y tests, y las herramientas se ejecutan en el proyecto principal, de una en una.

## 13. Metodología: Claude Code + GPT + Unity + Git

### 13.1 Flujo de cada cambio

Auditar → plan + archivos permitidos → copia → aplicar → Unity / Play → revisar diff → aprobar o deshacer → commit + documentar. Un test en verde **no** equivale a una experiencia visual o sonora aprobada.

**Definición de terminado:**
1. Git limpio o cambios identificados, con commit previo.
2. Compila sin errores ni avisos nuevos.
3. Funciona en Play con sus referencias configuradas en la escena.
4. Evidencia (captura o vídeo) y tests en verde si es lógica.

### 13.2 Reglas fijas en `CLAUDE.md`

- Unity 6000.6.0f1, URP; API de Unity 6 (`linearVelocity`, `FindFirstObjectByType`); C# 9.
- **Escritura por defecto:** `Assets/_Eclipse/`, `docs/`, `LICENSES/`.
- **`Packages/` y `ProjectSettings/`:** solo con autorización explícita de Ion para ese cambio, preflight (`git status` limpio + copia de los archivos afectados), diff mostrado antes de aplicar y commit separado.
- **`ThirdParty/`:** siempre solo lectura.
- Nunca editar como texto el YAML de escenas o prefabs. Los cambios masivos en escenas, prefabs o ajustes se hacen con herramientas de Editor que siguen el patrón de la sección 12.1. Unity cerrado antes de cualquier batchmode.
- Sectores autónomos: ningún objeto apunta a otro sector; nada persistente dentro de un sector (sección 10.4).
- Suscripciones a `GameEvents` en `OnEnable` y bajas en `OnDisable`; ningún estado de escena en campos `static` (sección 10.3).
- Comentarios, documentación XML y textos de interfaz en euskera. KISS + DRY: helpers pequeños cuando eliminan lógica repetida; validar antes de modificar; resultados explícitos; nunca un `catch` vacío.
- Nunca push sin preguntar. `git push --force`, `git reset --hard` y `rm -rf` bloqueados en `.claude/settings.json`.
- Repos y paquetes de terceros: revisión de solo lectura antes de abrirlos en Unity.

### 13.3 Copias de seguridad (corregido)

Git, `git bundle` y los commits **no** guardan por sí solos los objetos de Git LFS ni los assets que no están versionados.

| Capa | Qué guarda | Cómo se comprueba |
| --- | --- | --- |
| 1. Remoto privado en GitHub con LFS | Código, escenas, prefabs, `.meta`, texturas y modelos propios (con LFS) | `git lfs ls-files` lista los binarios esperados; `git lfs push --all origin` sin errores; `git lfs fsck` limpio |
| 2. Copia independiente | Proyecto completo **sin** `Library/`, `Temp/`, `Logs/`, `obj/` ni builds, en una carpeta con fecha en un disco externo; además, los paquetes originales del kit y de los assets (`.unitypackage` o ZIP de Fab; la caché de la Asset Store en Linux suele estar en `~/.local/share/unity3d/Asset Store-5.x/`, a comprobar) | Copia sin `--delete`; manifiesto `sha256sum` comparado tras copiar |
| 3. Prueba de restauración | — | El 6/11 por la noche y el 17/11: clonar el remoto en una carpeta temporal, `git lfs pull`, abrir en Unity, comprobar que no faltan referencias |

Los assets con licencia de la Asset Store solo van al repo **privado** y a copias personales, nunca a un repo público.

## 14. Plan de calidad (QA)

| Nivel | Qué verifica | Cuándo |
| --- | --- | --- |
| EditMode | `Inbentarioa`, `EMPLogika`, puntuación, combos, vidas, escudo, snapshot de `GameSession` | Antes de cada merge |
| Validador de Editor | Scripts perdidos, referencias nulas, colliders en interactuables y receptores EMP, NavMesh, escenas en Build Profiles, materiales rotos, duplicados creados por herramientas, modos de luz, UV2, flags estáticos, **ids únicos de objetos y enemigos**, objetos con cantidad 1, `ElementuDatuak` con id único, puertas y paneles que apuntan a un objeto existente, acciones de entrada presentes, **referencias entre sectores, objetos estáticos fuera de las raíces, un `NavMeshSurface`, un Volume y un `Spawn_Jokalaria` por sector, nada persistente dentro de un sector** | Antes de cada merge y de cada bake |
| Herramientas de Editor | Vista previa sin cambios, idempotencia, un solo Undo, alcance (TRE-01 a TRE-04), en una escena de prueba creada por el propio test | Al crear o cambiar una herramienta |
| PlayMode (humo) | Carga de escena, recogida, EMP, daño, transición, reintento | Antes de cada etiqueta |
| Build de Windows | Arranque, partida completa, gráficos, controles, audio, en otro PC | 17/11 y antes de entregar |

Los tests también se lanzan sin abrir el Editor, siempre con el Editor cerrado: `Unity -batchmode -projectPath <ruta> -runTests -testPlatform EditMode -testResults <archivo>.xml`. El validador escribe un informe en `docs/qa/` con OK / AVISO / ERROR.

**Casos de prueba principales:**

| Id | Caso | Resultado esperado |
| --- | --- | --- |
| INB-01 | Añadir un botiquín con el inventario vacío | Ranura 1 = Botikina ×1 |
| INB-02 | Añadir con la pila en el máximo y una ranura libre | Nueva ranura con ×1 |
| INB-03 | Añadir con las 4 ranuras llenas al máximo | `Beteta`; nada cambia |
| INB-04 | Usar botiquín con 100 de salud | `EzDaBeharrezkoa`; cantidad igual |
| INB-05 | Usar botiquín con 70 de salud | Salud 100; cantidad −1 |
| INB-06 | Usar batería con la energía llena | `EzDaBeharrezkoa`; cantidad igual |
| INB-07 | Usar una ranura vacía | `Hutsik` |
| INB-08 | `Kendu` un fusible que no se tiene | `false`; nada cambia |
| INB-09 | `Argazkia` + cambios + `Berrezarri` | Estado idéntico al de la foto |
| EMP-01 | Disparo normal con 100 | Energía 75 |
| EMP-02 | Soltar a los 0,9 s con 100 | Cargado; energía 40 |
| EMP-03 | Mantener con 50 de energía y soltar | Disparo normal; energía 25 |
| EMP-04 | Disparar con 20 de energía | Sin disparo; energía 20 |
| EMP-05 | Abrir inventario mientras carga | Carga cancelada; energía sin cambios |
| EMP-06 | Soltar una sola vez | Un solo disparo |
| EMP-07 | Regeneración | Empieza 1 s después del último disparo, a 20/s; no regenera mientras carga |
| EMP-08 | Pared entre la pistola y el punto apuntado (PlayMode) | El impacto se produce en la pared |
| EMP-09 | Cargado contra dron (PlayMode) | Dron destruido; +150 |
| PUN-01 | Aturdir la misma torreta dos veces | Solo +50 la primera vez |
| PUN-02 | 3 acciones que puntúan en menos de 3 s | Combo ×3 |
| PUN-03 | Recibir daño con combo ×3 | Combo ×1 |
| PUN-04 | Una torreta entra en aviso | Se pierde el bonus Fantasma |
| BIZ-01 | Perder una vida con un botiquín en el inventario | Botiquín conservado; reaparece en el checkpoint |
| BIZ-02 | Llegar a 5.000 puntos con 5 vidas | Sigue en 5 |
| BIL-01 | Entrar dos veces en el trigger de un objeto (PlayMode) | Se añade una sola vez |
| BIL-02 | Morir después de recoger un objeto | No reaparece |
| BIL-03 | Game over y reintento | El objeto reaparece; inventario y puntos como al entrar al sector |
| MIS-01 | Puerta del laboratorio sin tarjeta / con tarjeta | Mensaje / se abre |
| MIS-02 | Panel con fusible | Fusible −1; panel activo |
| TRA-01 | Pasar del sector 1 al 2 | Se conservan puntos, vidas, inventario y tarjeta |
| TRA-02 | Usar la tarjeta en el terminal del sector 3 | Empieza la cuenta atrás de 180 s |
| REI-01 | En S2: activar 2 paneles, abrir una puerta, destruir un dron; game over y reintento (PlayMode) | Paneles apagados, puerta cerrada, fusibles otra vez en el suelo y no en el inventario, dron en su patrulla inicial |
| REI-02 | En S3: activar el terminal; game over y reintento | Cuenta atrás parada, terminal inactivo, tarjeta en el inventario |
| REI-03 | En S2: activar un panel y perder una vida | El panel sigue activo y los enemigos como estaban; no hay recarga |
| REI-04 | Reintento después de pasar el checkpoint intermedio | Se reaparece al inicio del sector |
| REI-05 | Dos reintentos seguidos | Un solo `GameManager` y un solo HUD; ningún error por eventos de objetos destruidos |
| SEK-01 | Escena de prueba con un objeto de `Sektorea1` que apunta a otro de `Sektorea2` | El validador da ERROR |
| TRE-01 | Ejecutar una herramienta en vista previa | Informe generado; la escena no queda marcada como modificada |
| TRE-02 | Ejecutar dos veces la misma herramienta | La segunda informa "0 cambios"; sin duplicados |
| TRE-03 | Aplicar y pulsar Ctrl+Z una vez | La escena vuelve al estado anterior completo |
| TRE-04 | Aplicar con `Sektorea2` seleccionado | Nada cambia en `Sektorea1` ni en `Sektorea3` |

**Rúbrica → implementación → prueba → evidencia:**

| Criterio | Implementación | Prueba observable | Evidencia |
| --- | --- | --- | --- |
| Hasiera eta amaiera | Intro, Zero Protokoloa, escape, derrota | Partida completa y una derrota | Vídeo |
| Pertsonaiak eta etsaiak | Protagonista, torreta, dron y variantes | Cada enemigo recorre todos sus estados | Vídeo por enemigo |
| Eszenatokia | Escenario y narrativa ambiental | Capturas desde las cámaras de evidencia | Capturas |
| Mailak | 3 sectores y `LevelConfig` | Paso 1 → 2 → 3 y tabla de progresión | Tabla + vídeo |
| Puntuazioa | Puntos, combos, bonus, estadísticas | PUN-01 a PUN-04 + pantalla final | Captura + informe de tests |
| Bizitzak | Salud, escudo, vidas, checkpoints, reintento con mundo restaurado | BIZ-01, BIL-03, REI-01 a REI-05, game over | Vídeo |
| Interakzioa | EMP doble, inventario, misión, paneles, linterna | EMP-01 a EMP-09, INB, MIS | Vídeo + informe |

## 15. Guía artística y sonora

**Código de color** (igual en luces, emisivos e interfaz):

| Color | Significado |
| --- | --- |
| Cian | Interactuable o recogible: paneles, terminales, objetos, puertas abiertas |
| Amarillo | Aviso o sospecha: un dron que te oye, una torreta que apunta |
| Rojo | Peligro: alerta, láser, puerta bloqueada |
| Verde | Hecho: objetivo cumplido, checkpoint, panel activo |

**Paleta por sector:**

| Sector | Paleta | Materiales | Atmósfera |
| --- | --- | --- | --- |
| Laborategia | Blanco, gris claro, cian | Paneles satinados, cristal, metal limpio | Limpio, frío, ordenado |
| Energia | Negro, rojo de emergencia, haz cálido de la linterna | Metal sucio, rejillas, cables, óxido | Oscuro y tenso, polvo en el haz |
| Erreaktorea | Naranja, ámbar, sombras duras | Metal quemado, tuberías, humo | Urgente: alarmas, sirena, cuenta atrás |

**HUD e inventario** (en euskera, una sola tipografía sans con licencia libre):
- Vidas y escudo abajo a la izquierda; energía EMP con marcas en 25 y 60 junto a la retícula; puntuación y combo arriba a la derecha; objetivo actual arriba a la izquierda; ranuras rápidas 1–4 abajo en el centro.
- Panel del inventario: 4 ranuras con icono y cantidad, lista de objetos de misión, descripción del objeto seleccionado, botón "Erabili". Fondo semitransparente sobre el juego pausado.
- Objetos recogibles: brillo cian suave; los de misión, además, con un haz vertical para verlos de lejos.

**Regla de feedback:** toda interacción importante produce efecto visual, sonido y cambio de estado.

| Evento | Visual | Sonido | Cambio de estado |
| --- | --- | --- | --- |
| Disparo normal | Rayo fino cian + chispa en el impacto | Chasquido eléctrico | −25 de energía |
| Carga y disparo cargado | Anillo que se llena; rayo grueso y onda de choque | Zumbido que sube + descarga grave | −60 de energía; retroceso de cámara |
| La EMP alcanza un dron | Chispas y parpadeo azul | Descarga | Aturdido o destruido |
| Recibir daño | Destello rojo y vibración de cámara | Golpe + alarma del escudo | Baja escudo o salud; combo a ×1 |
| Recoger | Partícula + "+1 Botikina" o "+100 ×2" | Tono ascendente | Inventario o puntos |
| Usar consumible | Pulso verde (salud) o cian (energía) en el HUD | Sonido propio de cada objeto | Salud o energía suben |
| Inventario lleno / objeto innecesario | Mensaje centrado | Tono grave corto | Nada cambia |
| Panel con fusible | Luz del panel de rojo a verde | Inserción + interruptor | Panel activo |
| Checkpoint | Luz verde | Confirmación | Nuevo punto de reaparición |

**Audio:** grabaciones reales (Freesound, licencia citada), nunca sintetizadas. Un ambiente por sector, Audio Reverb Zones en salas grandes y pasos distintos en metal y rejilla.

**Criterios visuales comprobables:** sin fugas de luz ni manchas en los lightmaps de las capturas de evidencia; ninguna zona negra sin intención; densidad de lightmap uniforme; 60 FPS a 1080p en el build con la RTX 4060.

## 16. Riesgos y plan B

| Si falla… | Se detecta | Plan B |
| --- | --- | --- |
| El kit (materiales rosas, UV2, escala) | Prueba del 3/11 | Siguiente kit; si ninguno convence, templo o ruinas con el mismo código |
| El kit sobrescribe ajustes del proyecto | Al importar | Commit justo antes; `git diff` y revertir |
| El bake en Fedora | Prueba del 3/11 | Compute → Progressive GPU → CPU; escenas compactas, bake de noche |
| PixelWizards en 6000.6 | 7/11 | Paquete oficial Starter Assets: Character Controllers (URP). La prueba de kits ya lo abre en 6000.6 (cápsula de escala), así que se sabe antes si funciona |
| Estado del mundo que no se reinicia al reintentar | REI-01 a REI-05 | La recarga de escena lo resuelve; si algo persiste, es estado `static` o un objeto fuera de su sector: se corrige ahí, no con código de reinicio |
| Referencias entre sectores al pasar a 3 escenas | Validador (SEK-01) | Prohibidas desde el 4/11; se sustituyen por eventos o ids |
| El apuntado en tercera persona | Noche del 10/11 | EMP como pulso de área en los dos modos (mismo `EMPLogika`, otro método de impacto) |
| Disparos que atraviesan paredes | EMP-08 | Doble raycast obligatorio; capa de colisión dedicada |
| Conflictos de entrada con la interfaz abierta | EMP-05 | Cambio de *action map* obligatorio al abrir cualquier interfaz |
| Pausa del inventario y animaciones de la interfaz | Pruebas del 9/11 | La interfaz usa tiempo sin escalar |
| Duplicados o *farm* de objetos | BIL-01 a BIL-03 | Ids únicos + snapshot conjunto; el validador bloquea ids repetidos |
| NavMesh del dron | 10/11 | Patrulla por puntos fijos |
| Solo 2 tipos de enemigo para "anitzak" | Revisión del 12/11 | Variantes por configuración (giratoria, cazador) y conductas muy diferenciadas |
| El profesor no acepta sectores como niveles | Su respuesta o el 7/11 | 3 escenas (sección 10.4) |
| Falta tiempo para los 3 sectores el 6/11 | Miércoles por la noche | Sector 1 espectacular; 2 y 3 más sencillos |
| El build en Windows | 17/11 | Revisar primero entorno y dispositivo de audio; después el build |
| Alcance total (inventario + EMP doble + 3 sectores) | Revisión del 12/11 | Orden de recorte: telemetría → remapeo → variantes → linterna como objeto. **Nunca** se recortan criterios de la rúbrica |

## 17. Valoración crítica: contradicciones detectadas

1. **Escenas frente a lo enseñado en el curso.** En la captura de la rúbrica, la barra lateral de Moodle muestra lecciones de una unidad anterior: "10. Ondoz ondoko bi maila" (dos niveles consecutivos) y "11. Puntuak gordetzen: Singleton" (guardar puntos con Singleton). Es un **indicio, no un requisito**: el profesor asocia "maila" a escenas consecutivas con un Singleton que conserva los puntos. Por eso Claude recomienda 3 escenas para el 20/11 si el profesor no responde antes del 7/11; además, así se demuestra esa técnica del curso. No se cambia la decisión de v2.0 sin Ion (sección 10.4).
2. **La barrera láser estaba entre los enemigos (v2.0).** Pasa a ser peligro ambiental, como pide el encargo. Consecuencia: quedan 2 tipos de enemigo para "Pertsonaiak eta etsaiak — anitzak". Se compensa con variantes por configuración y conductas muy distintas, sin sistemas nuevos.
3. **Exploit de puntos en v2.0.** Aturdir una torreta daba +50 cada vez y alimentaba el combo: se podía *farmear*. Corregido: neutralización premiada una vez por enemigo y sector.
4. **Bonus Fantasma en el sector 1.** Sin drones, siempre se cumplía. Ahora cuenta cualquier alerta, incluido el aviso de las torretas.
5. **"Sin inventario" en v2.0.** Eliminado de los recortes; el inventario ligero entra en GDD, arquitectura, QA, calendario y rúbrica.
6. **Permisos de Claude demasiado estrictos.** "Solo `Assets/_Eclipse/`" impedía cambios legítimos en `Packages/` o `ProjectSettings/`. Corregido con autorización explícita, preflight y diff.
7. **Copias de seguridad incompletas.** Git y `git bundle` no cubren LFS ni assets sin versionar. Corregido con 3 capas y una prueba de restauración.
8. **Ortografía de nombres en el encargo.** Usaba `Inventarioa` e `InbentarioUI` a la vez. Se unifica en **`Inbentarioa`**, que es la forma correcta en euskera, en clases, carpetas e interfaz.
9. **Recogida parcial frente a KISS.** El encargo pide aclararla. Con objetos de 1 unidad no existe; la regla general queda escrita por si algún día cambia.
10. **Disparo al soltar.** Evita el doble disparo, pero añade un poco de retraso al disparo normal. Es aceptable en un *shooter* táctico; si en las pruebas se nota, el umbral de carga puede bajar a 0,6 s.
11. **Reintento con mínimo 3 vidas.** Es generoso (entrar al sector 3 con 1 vida y reintentar con 3), pero evita una espiral sin salida, y la puntuación vuelve a la de entrada, así que no se gana nada. Se mantiene.
12. **`GetAxis` frente al Input System.** El tutorial 4 enseña ambos; los controladores elegidos usan el nuevo. Si se quiere mostrar `GetAxis`, *Active Input Handling* en Both.
13. **`ThirdParty/` de solo lectura frente a los UV2 del kit.** Si un modelo del kit no trae UV2, activar *Generate Lightmap UVs* cambia su `.meta` dentro de `ThirdParty/`. Es la única excepción: con `UV2Konfiguratzailea`, autorización de Ion y commit separado. El contenido del kit (modelos, texturas, scripts) sigue sin tocarse.
14. **"Todo con Undo" frente a los assets.** Undo solo cubre escenas abiertas. Prefabs, ScriptableObjects, `.meta` y ajustes guardados en disco se protegen con el commit previo, no con Ctrl+Z (sección 12.1).
15. **Reintento con una escena grande.** Recargar `01_Instalazioa` entera para reintentar un sector es la opción más simple y segura, pero carga los 3 sectores. Si tarda demasiado, es un argumento más para las 3 escenas.

## 18. Preguntas abiertas y regla de cierre

**Para el profesor:**
- [ ] ¿3 sectores con transición de nivel cuentan como 3 "maila", o deben ser escenas separadas?
- [ ] ¿El juego del 20/11 debe transcurrir en el escenario del 6/11?
- [ ] ¿Se permite código de terceros con licencia MIT y herramientas de IA, citándolos?

**Pendiente de Ion (martes 3):**
- [ ] Enunciado completo del 6/11, formato de entrega y si tiene rúbrica propia.
- [ ] Contenido de los ZIP del profesor.
- [ ] Precio actual del Sci-Fi Facility Kit 2.

**Datos sin verificar:** compatibilidad real de los kits, de PixelWizards y de Starter Assets: Character Controllers (URP) con 6000.6; que el Compute Light Baker funcione en Fedora con la RTX 4060; la ruta de la caché de la Asset Store en Linux; el precio del Facility Kit 2.

**Regla de cierre:** la v2.1 está aprobada y la estrategia queda congelada. Solo cambia por evidencia: una prueba en Unity que falla o una respuesta del profesor. Los números del GDD son valores iniciales que se ajustan jugando.

## 19. Criterios de aceptación

El juego se da por terminado cuando **todos** se cumplen en el build de Windows, no solo en el Editor:

| Id | Criterio |
| --- | --- |
| AC-01 | Se recoge un botiquín y una batería; aparecen en sus ranuras con la cantidad correcta |
| AC-02 | El botiquín cura 50 sin pasar de 100 y **no** se gasta con la salud llena; la batería llena la energía y **no** se gasta con la energía llena |
| AC-03 | Con las 4 ranuras llenas, un objeto no se recoge, sigue en el suelo y aparece el mensaje |
| AC-04 | La tarjeta abre la salida de Laborategia y activa el terminal de Erreaktorea; sin tarjeta aparece el mensaje |
| AC-05 | Cada fusible activa un panel y se consume; con los 3 paneles se restablece la energía y cambia la luz |
| AC-06 | Ningún objeto se duplica: ni por reentradas de trigger, ni al morir, ni al cambiar de sector |
| AC-07 | El disparo normal cuesta 25 y aturde; el cargado cuesta 60, destruye drones, aturde torretas 8 s y empuja cajas; ambos tienen retícula, efecto y sonido |
| AC-08 | Nunca hay doble disparo al soltar, ni disparos con la interfaz abierta, ni impactos a través de paredes |
| AC-09 | Al pasar de sector se conservan puntos, vidas, inventario y objetos de misión |
| AC-10 | Al perder una vida se conservan inventario y objetos recogidos; se reaparece en el checkpoint |
| AC-11 | Tras game over, "Berriz saiatu" restaura **juntos** puntos, vidas, inventario y objetos del inicio del sector, y el mundo del sector (puertas, paneles, fusibles colocados, terminal, enemigos, checkpoints y cuenta atrás) vuelve a su estado inicial |
| AC-12 | Ninguna acción repetida da puntos más de una vez por enemigo o láser |
| AC-13 | Los 3 sectores tienen título de entrada, objetivo visible, estadísticas al salir y dificultad creciente según `LevelConfig` |
| AC-14 | La torreta y el dron muestran todos sus estados con aviso visual y sonoro |
| AC-15 | Intro, final con escape y derrota funcionan, con textos en euskera |
| AC-16 | Tests EditMode en verde, validador sin errores, PlayMode de humo en verde |
| AC-17 | El build de Windows arranca en otro PC y la partida se completa de principio a fin, con audio |
| AC-18 | El validador informa 0 referencias entre sectores, y cada sector tiene su `NavMeshSurface`, Volume, probes y `Spawn_Jokalaria` |
| AC-19 | Cada herramienta de Editor usada en el proyecto pasa TRE-01 a TRE-04 |
