# A5 — Auditoría real y plan maestro de producción (17/11/2026)

Autor: Claude (agente). Para Ion. **Estado: propuesta; ninguna misión se ha modificado.** Sustituye al plan A4 del 15/11 en lo que lo contradiga.
Cada dato va marcado como **[comprobado hoy]**, **[informe anterior]** o **[pendiente]**.

Evidencias de hoy:
- Copia de cambios pendientes: `~/ECLIPSE_babeskopia/a4_auditoria_20261117/` (`git_pendientes.tar`, 653 archivos; `git_diff_modificados.patch`; SHA-256).
- Copias de trabajo de los assets nuevos: `~/ECLIPSE_babeskopia/a2_blender/assets_candidatos/a4_candidatos/` (`SHA256_originales.txt` de los 12 archivos archivados; originales intactos).
- Renders y medidas: `~/ECLIPSE_babeskopia/a2_blender/renders/a4/` (PNG + JSON). Script: `scripts/a4_candidato.py`.
- Diagnóstico NavMesh: `docs/qa/tresnak/NavMeshAuditoria_2026-11-17_*.md` (herramienta nueva, solo lectura: `BatchNavMeshAuditoria`).

---

## 0. Decisión que bloquea parte del plan: Asset Store e IA

Tus reglas vigentes (CLAUDE.md global, skill `herramientas-3d`, memoria del 7/11) me prohíben **abrir, analizar o mirar** assets de la Unity Asset Store, por la cláusula del EULA que impide usarlos como entrada de IA. El encargo de hoy me pide evaluar precisamente esos paquetes (Creep, Ghoul, Female Zombie, Space Soldier PolygonR, Free Blood VFX). Las dos instrucciones chocan, así que **no los he abierto**: solo conozco nombre y tamaño.

| Opción | Qué implica |
|---|---|
| **A. Autorizas expresamente** que los abra y evalúe (asumes tú la interpretación del EULA) | Puedo auditarlos en Blender/Unity, adaptarlos y juzgar su estilo |
| **B. Integración a ciegas** (como el kit MagixBox desde el 7/11) | Los importo e integro por herramienta con métricas (huesos, triángulos, clips), y **tú** juzgas el aspecto en Unity |
| **C. No usarlos** | Criatura y soldado desde Mixamo u otra fuente abierta que descargues tú |

Mi recomendación: **B** para Creep y PolygonR (son tu elección y encajan por descripción), salvo que prefieras A.

---

## 1. Estado real de ECLIPSE

| Área | Estado | Fuente |
|---|---|---|
| Git | Rama `feature/eclipse-hud`, HEAD `09d2c73a`. 16 archivos modificados y 634 sin seguimiento (254 entradas). Sin commits desde A2. Respaldo hecho hoy | [comprobado hoy] |
| Archivos protegidos | 373 (escenas de misión, lightmaps, `Assets/Settings`, `ProjectSettings`, `Packages`) idénticos por hash al inicio de A3 | [comprobado hoy] |
| Sistemas | Arma EMP, daño localizado, IA, HUD, NPC, desmembramiento, contenido de misión en runtime (`MisioEdukia`), cinemática | [informe anterior, 15/11: regresión 24 pruebas OK] |
| Jugado por ti en Unity | Nada de A3/A4 consta como probado en Play | [pendiente] |
| NavMesh `Biologia` (M1) | **Accesible** (8/9 muestras). Mi nota del 15/11 era errónea: el científico no se colocó allí por las distancias a puntos de interés | [comprobado hoy] |
| NavMesh zonas de salida | **Defecto real:** `Irteera` (M1, M2, M3) y `J6` (M1, M2) inalcanzables para agentes. El NavMesh se horneó con la puerta de bloqueo cerrada y no se actualiza al abrirse. Enemigos y NPC no pueden seguirte al tramo final | [comprobado hoy] |
| Equilibrio M2 | Los 2 soldados (12 de daño cada 0,9 s) matan al primer monstruo (100 de vida) con 9 impactos, a 59,6 m del jugador, antes de que participe | [informe anterior, 15/11] |
| Build | Solo M1–M3 en la lista de escenas; `07_AmaieraZinematika` no está. Módulo Windows instalado. Release validada **en Linux64** (1 682 MB). Perfil Windows con `m_Development: 1`. **Build Windows: nunca generada ni probada por mí** | [comprobado hoy] / [pendiente] |
| Decals URP | El renderer `PC_Renderer` solo tiene SSAO: **no hay Decal Renderer Feature**. Los Decal Projector no se dibujarían | [comprobado hoy] |

### Por qué las misiones parecen laberintos (medido en los mapas de la herramienta)

| | M1 | M2 | M3 |
|---|---|---|---|
| Salas / pasillos / cruces 1×1 | 11 / 23 / 6 | 11 / 23 / 6 | 8 / 17 / 6 |
| Superficie transitable en pasillos y cruces | 32 % | 31 % | 28 % |
| Longitud media de pasillo | ≈ 16 m | ≈ 18 m | ≈ 19 m |
| Recorrido mínimo de la misión | 216 m | 403 m | 317 m |
| Sala más grande | **Laberinto `Mantentzea` 288 m²** | `Sorgailuak` 325 m² | `Erreaktorea` 464 m² |
| Sala emblemática hoy | `LabNagusia` **111 m²** | `Sorgailuak` 325 m² | `Erreaktorea` 464 m² |

Causas de raíz, en `EszenaEraikitzailea`/`MailaDatuak.cs`:
1. **Regla del generador:** una conexión es siempre un tubo `Profile1` de ≥ 3 celdas (11 m) o ≥ 6 con una puerta. Dos salas nunca se tocan con una abertura, así que cada sala es una isla.
2. **Retícula de cruces** `J1…J6`: seis nudos de 1×1 por misión que solo sirven para girar.
3. **Mismo esquema** en las tres: inicio → cruces → laberinto → control → bloqueo → salida.
4. **El laboratorio "principal" de M1 es de las salas más pequeñas.**

---

## 2. Herramientas [comprobado hoy con `comprobar_entorno.sh --unirig`]

| Herramienta | Estado | Uso en este plan |
|---|---|---|
| Blender 5.2.3 (`blender-oficial`), Cycles OptiX | Operativo; usado hoy para medir y renderizar | Armas, bata, nave, kitbash de maquinaria, materiales |
| Laboratorio `a2_blender` | 32 scripts previos (HypJo, casco B1), 114 renders. Reutilizo `b1_errendatu`, `p3_deformazioa`, `p1_testurak` | Único laboratorio; nada duplicado |
| UniRig (commit `6793c66`, CUDA OK, 2 checkpoints) | Operativo. Validado solo hasta deformación en un modelo CC0; **Unity Humanoid no validado** | Solo si una criatura llega sin rig. No para humanoides (Mixamo es mejor) |
| glTF Transform 4.5.1, FFmpeg 8.1.3, SoX 14.4.2 | Operativos | Optimizar GLB, vídeo de evidencia, audio |
| Unity 6000.6.0f1 + `unity_batch.sh` | Operativo (usado hoy) | Construcción, bake, pruebas |
| Unity CLI | Responde como **1.0.0** (el inventario decía beta.13) | Sin uso previsto |
| Generador de niveles + 24 pruebas de regresión | Operativo | Rediseño por datos |

---

## 3. Assets nuevos: inventario y veredicto

### 3.1 Armas (Sketchfab, CC BY 4.0, licencia en cada zip) [comprobado hoy]

| Modelo / autor | Geometría | Texturas | Veredicto |
|---|---|---|---|
| **Sci-Fi Pistol** — Milin Andrei | 17 449 tris, 21 mallas, 0,22 m de alto (escala real). Cargador y balas como piezas sueltas | 2 materiales, 2K, **con emisivo** | **ACEPTADA como pistola EMP.** Adaptar: montar cargador, grafito oscuro, emisivo cian, punto de disparo. Comparativa: `renders/a4/eclipse_sci-fi_pistol_hiru_laurden.png` |
| **Futuristic Sci-Fi Rifle** — Janis Zeps | 38 503 tris, 4,1 m de largo (**escala ×4**) | 1 material 4K | **ADAPTABLE** como fusil de soldados. Reescalar, oscurecer (hoy blanco con verde), reducir a 2K |
| **Sci-Fi AK Rifle** — Egor Likhachev | 12 242 tris, 3,45 m (**escala ×4**) | 1 material 2K | **ADAPTABLE**, la silueta más militar. El acabado blanco/naranja es de "skin": hay que repintar la base, no basta oscurecer |

El glTF es el formato a usar (texturas enlazadas y empaquetado metal-rugosidad). El FBX del rifle pesa 47 MB sin ventaja.

### 3.2 Sangre

| Recurso | Licencia | Veredicto |
|---|---|---|
| **Blood Decals** (Weasel Whistle): 3 decals PBR 4K/2K (salpicadura, regueros de pared, charco con huellas) | Uso libre personal y comercial; sin IA generativa (README) | **ACEPTADO.** Mucho mejor que mi textura procedural. Usar las texturas 2K sobre quads (no requiere tocar el renderer). El `.unitypackage` no hace falta |
| Free Blood VFX – URP | Asset Store | Sin abrir (sección 0) |

### 3.3 Kits Quaternius (CC0) [comprobado hoy]

| Kit | Contenido real | Veredicto |
|---|---|---|
| **Modular SciFi MegaKit [Standard]** | 190 piezas: 84 muros, 38 plataformas/escaleras/puertas/ventanas, 28 props (barandillas, ventilación, luces, cables), 8 columnas, 29 decals. Trim sheets PBR con normal y ORM | **ACEPTADO con adaptación.** No es low-poly plano: es sci-fi limpio en blanco/negro/rojo. Ideal para M1 (clínico) y para barandillas, escaleras, columnas y marcos en todas. Para M2/M3 usar las variantes oscuras. Los glTF buscan las texturas en su propia carpeta: hay que reenlazarlas |
| **Sci-Fi Essentials Kit [Standard]** | 37 modelos: cajas, taquillas, estanterías, mesas, silla, antena, 3 robots, armas | **Props ACEPTADOS** (cajas metálicas, taquillas, estanterías, mesas). **Descartar** lonas azules saturadas, planetas y armas (estilizadas) |
| **Modular Sci Fi Guns (2021)** | 78 armas low-poly de color plano | **DESCARTADO**: rompe la coherencia con HypJo |

### 3.4 Asset Store (sin abrir; solo nombre y tamaño)

Creep Horror Creature (217 MB), Free Female Zombie (24 MB), Ghoul-zombie (20 MB), Sci Fi Space Soldier PolygonR (47 MB), Chinese Cyborg Warrior (164 MB), Free Blood VFX – URP (25 MB). Ya en el proyecto: `Sci Fi Modular Pack` (MagixBox) y `Data/Sci-fi_assets` (KMProd). Pendiente de tu decisión (sección 0).

---

## 4. Rediseño arquitectónico propuesto

**Principio:** de "islas unidas por tubos" a **"sala emblemática + salas adosadas + una espina"**.

**Cambio técnico necesario (una vez, sirve para las tres):** nuevo tipo de conexión en el generador, `Irekidura`: abertura ancha directa entre dos salas adyacentes (como ya hacen las ventanas `Leihoak`, pero transitable). Con eso las salas se pueden pegar a la sala central sin tubo de 11 m.

| | Hoy | Propuesta |
|---|---|---|
| Pasillos por misión | 23 / 23 / 17 | ≈ 8 / 9 / 7 |
| Cruces 1×1 | 6 / 6 / 6 | 0–1 |
| Recorrido de misión | 216 / 403 / 317 m | ≈ 150–220 m, más corto e intenso |
| Laberinto `Mantentzea` | En las tres | Solo en M3, reducido |

### M1 Laborategia — "el atrio científico"

- **`LabNagusia` pasa de 3×4 (111 m²) a 8×6 celdas (≈ 675 m²), 3 alturas de panel**, con entreplanta en U a 4,6 m y, en el centro, una **cámara de contención acristalada 2×2** con el espécimen: lo que salió mal.
- **Adosadas al atrio, con cristalera y abertura, sin pasillo:** `Biologia` (observación), `Lab1` (química), `Zerbitzariak` (servidores, ya comparte pared) y una sala médica.
- **`Kontrola` sube a la entreplanta norte**, como mirador sobre el atrio; allí el terminal de seguridad.
- **Recorrido:** `Esnatzea` (bahía médica) → pasillo principal corto → `Harrera` como control de seguridad con soldados → **revelación del atrio** → objetivos alrededor → `Borroka` convertida en zona de cuarentena deteriorada → salida.
- **Ruta secundaria:** el laberinto se sustituye por un túnel de servicio en L, oscuro, con los primeros indicios (sangre, luz que falla).
- Ritmo: calma clínica → descubrimiento → inquietud.

### M2 Energia — "la central"

- **`Sorgailuak` pasa de 5×5 a 8×7 celdas (≈ 790 m²), 4 alturas (≈ 11 m)**, con tres generadores protagonistas y **dos niveles de pasarela** (4,6 y 8,5 m) que cruzan la nave.
- **`Kontrola` acristalada** en el primer nivel, mirando a la nave.
- **`Bobinak` → sala de turbinas 5×6**, unida a la nave por una abertura ancha.
- **`Fusibleak1/2`** como bahías laterales abiertas a la nave.
- **`Sarbidea` → puesto de seguridad** con barricada y soldados; espina principal hasta la nave.
- **`Hodiak` + `Mantentzea` → galería de mantenimiento** a un lado de la nave (ruta secundaria; ahí se ve por primera vez al monstruo, tras tuberías).
- **Encuentro:** alarma al activar el primer cuadro → el monstruo irrumpe en turbinas con dos soldados disparando → lo remata el jugador → segunda oleada de 2 en la nave → salida.
- **Equilibrio:** daño del soldado 12 → 3, los NPC no pueden bajar al monstruo del 40 % de vida, vida 100 → 150 y aparición por proximidad del jugador, no al cargar.

### M3 Erreaktorea — "el reactor"

- **`Erreaktorea` pasa de 6×6 a 9×9 celdas (≈ 1 140 m²), 5–6 alturas (≈ 15 m)**, tres anillos de pasarela con **tramos rotos** que obligan a rodear.
- **Revelación en eje:** `Segurtasuna` → esclusa `Deskontaminazioa` infestada → se abre al reactor.
- **`Kontrola1/2`** como cabinas acristaladas en el primer anillo.
- **`AzkenBorroka` → hangar de evacuación 8×6**, pegado al este con una abertura grande: barricadas, derrumbe, la oleada final.
- **`Irteera` → compuerta exterior**, con el mismo diseño que la fachada de la cinemática.
- **Laberinto:** se conserva solo aquí, reducido a 3×4, oscuro e infestado, opcional.

### Coste y riesgo por misión

Editar datos → reconstruir → preset de iluminación → **bake (~10 min GPU)** → oclusión → NavMesh → atrezzo → `MisioEdukia` → regresión (robot, HUD, interacción, contenido). Respaldo previo de la escena y su carpeta de lightmaps (~110 MB). Riesgo conocido: el defecto de parches en lightmaps (por eso los pasillos siguen fuera del lightmap). La revisión visual de cada bake es tuya.

---

## 5. Personajes, criaturas, armas, VFX y cinemática

| Elemento | Propuesta | Depende de |
|---|---|---|
| **Criatura** | Una sola criatura bien preparada (candidata: Creep) con dos variantes de material para M2 y M3. Rig humanoide → hitboxes y desmembramiento actuales | Sección 0. Si llega sin rig humanoide: UniRig + mapeo manual (**riesgo alto; solo como último recurso**) |
| **Soldados** | PolygonR si pasa tu revisión de estilo; fusil AK adaptado en mano | Sección 0 |
| **Científicos** | **No hay ninguno en la biblioteca.** Humano de Mixamo (lo descargas tú) + bata modelada en Blender con peso copiado del cuerpo; 2–3 variantes por color de piel/pelo/bata | Descarga de Mixamo |
| **Pistola EMP** | Sci-Fi Pistol adaptada (sección 3.1) sustituye al kitbash; el sistema de arma no cambia | Nada: lo puedo hacer ya |
| **Sensación de disparo** | Retroceso de cámara y arma, fogonazo con luz, casquillo/chispa EMP, impacto con decal, sonido con cola; recarga con cargador visible | Nada |
| **Sangre** | Decals de Weasel Whistle en impactos, muñones, cadáveres y como ambientación fija en M3 | Nada |
| **Infestación M3** | **No hay recurso.** Opción 1: modelarla en Blender (metaballs + desplazamiento, material húmedo). Opción 2: CC BY de Sketchfab ("Living Flesh Blob", "The Flesh") | Tu elección |
| **Generadores/turbinas M2** | **No hay recurso.** Kitbash en Blender con columnas, tuberías y paneles del MegaKit (mismo lenguaje visual que el resto) | Nada |
| **Nave de rescate** | **No hay ninguna en la biblioteca.** Hace falta un modelo: los dos candidatos CC BY del 15/11 (Monette; Void Voyager) o uno que elijas | Descarga |
| **Planeta** | Rocas y HDRI CC0 de Poly Haven (Namaqualand), o mejorar el terreno propio con las texturas que ya tenemos | Descarga opcional |
| **Final** | `07_AmaieraZinematika` en la lista de escenas de la build, tras M3; texto «JARRAITUKO DU...» ya puesto | Tu permiso (`ProjectSettings`) |

### Recursos adicionales que faltan de verdad

1. Un humano base para los científicos (Mixamo).
2. Una nave de rescate.
3. (Opcional) rocas/HDRI de desierto e infestación orgánica, si no quieres que las modele.

---

## 6. Plan por fases (8 días hasta el 25/11)

| Día | Trabajo | Requiere |
|---|---|---|
| 17–18 | **Tareas seguras:** equilibrio M2; NavMesh de las zonas de salida; pistola EMP adaptada en Blender y probada en `04_BorrokaProba`; decals de sangre; extensión `Irekidura` del generador probada en `02_PrototipoDiseinua` | Nada |
| 18–19 | **Piloto M1** (atrio científico): respaldo, datos, construcción, bake, regresión → la juegas tú | Tu aprobación del rediseño |
| 19–21 | **M2** y **M3** con el mismo procedimiento; generadores por kitbash; infestación | Aprobación del piloto |
| 21–22 | **Personajes:** criatura, soldados, científicos; encuentros | Sección 0 + Mixamo |
| 23 | **Cinemática:** nave, exterior, enlace M3 → 07 en la build | Nave + permiso `ProjectSettings` |
| 24 | **Build Windows** sin Development, regresión completa, rendimiento | — |
| 25 | Margen | — |

Si el tiempo aprieta, el orden de sacrificio es: variantes → infestación modelada → M3 arquitectura completa (se haría solo el reactor) → cinemática mejorada.

---

## 7. Riesgos

- **Plazo:** tres rediseños con bake en 3–4 días es ajustado. El piloto de M1 dirá si el ritmo es real.
- **Lightmaps:** cada bake puede traer parches; tu revisión es obligatoria.
- **EULA Asset Store:** sin tu decisión, criatura y soldados quedan con maniquíes.
- **Build Windows sin probar:** hay que hacerla pronto, no el último día.
- **Rendimiento:** salas más grandes y más NPC; medir FPS en Play.
- **Estilo:** el MegaKit es más limpio que el kit oxidado actual; hay que unificar con materiales, no mezclar sin más.

## 8. Decisiones que necesito

1. **Asset Store e IA:** A, B o C (sección 0).
2. **Rediseño:** ¿apruebo el piloto de M1 tal como está descrito?
3. **Descargas:** humano de Mixamo y nave de rescate (y, si quieres, desierto e infestación).
4. **Decals URP:** ¿puedo añadir la Decal Renderer Feature a `PC_Renderer` (toca `Assets/Settings`) o sigo con quads?
5. **Build:** ¿puedo añadir `07_AmaieraZinematika` a la lista de escenas cuando toque?

## 9. Primera tarea recomendada

Mientras decides: **equilibrio de M2 + NavMesh de salidas + pistola EMP adaptada + extensión `Irekidura` en la escena de prototipo.** Son cambios de código, datos y laboratorio; ninguno toca las escenas de misión ni su iluminación.
