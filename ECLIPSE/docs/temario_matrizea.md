# ECLIPSE: temario → implementación → evidencia

> Entrega 1: *Zure eskenatokia osatu*, 6/11/2026 14:15. **Comprobado** = verificado en Unity con informe o captura. **Pendiente** = aún no está hecho. La tabla se actualiza con cada ejecución real.

## 1. Materiales del profesor: qué hay realmente

Búsqueda del 6/11 en `Baliabideak/`, `1_Ebaluazioa/`, `~/Downloads`, `~/Documents` y GitHub (por nombre y, en notas de texto, por contenido).

| Fuente | Estado | Qué aporta |
| --- | --- | --- |
| Moodle (`ikasgela.tolosaldealh.eus/course/section.php?id=14808`) | **No accesible desde aquí** (requiere sesión) | Solo la captura `03_Temario_Escenarios.png`: confirma los **títulos** de las lecciones. **No hay contenido de ninguna lección** |
| ZIP del profesor | **No encontrados** | La estrategia (sección 18) y tus notas ya los daban por pendientes |
| `Baliabideak/Gidak_eta_Apuntak/` | **Vacía** | Nada |
| GitHub del profesor | **No identificado** | Ninguna nota local lo menciona |
| `Baliabideak/cursosYouTube` (`ncabanes/cursosYouTube`) | Local. **Nada lo vincula al profesor** | Cursos de Nacho Cabanes; **sin licencia** en GitHub (no se reutiliza código). Sin coincidencias de lightmap, probes, skybox ni trim sheet en `.md`, `.txt` y `.cs` de `unity2020` y `krakoutUnity2021` |
| `Baliabideak/AssetsExternos/ModKit` | Local. **No es del profesor** | CC0 (JaronKBragg7337). Su README trata mallas estancas y trim sheets; solo referencia |
| `Baliabideak/AssetsExternos/Docs` | **Vacía** | Nada |

**Conclusión:** no se ha podido revisar ningún ejemplo del profesor, porque no están disponibles. Esta matriz sigue los títulos de Moodle; el contenido se justifica con lo implementado, no con sus ejemplos.

**Sin revisar:** el resto de `AssetsExternos`, los proyectos Unity de otros cursos (`Unity_Amigo_Analisis`) y el contenido de los `.unitypackage`.

## 2. Matriz (títulos de Moodle → ECLIPSE)

Datos de `GeometriaAuditoria_2026-11-06_022519.md`, capturas en `docs/qa/kapturak/`. Lightmaps: bake del 06/11 a las 01:13.

| Lección | Implementación real | Evidencia | Estado |
| --- | --- | --- | --- |
| Topologia | Auditoría de las 42 mallas de la escena: triángulos, aristas abiertas, degenerados | `GeometriaAuditoria`; `SM_DatuNukleoa`: 96 tri, 0 abiertas | **Comprobado** |
| Topologia: Non-Manifold | Medido, no corregido (el kit no se toca). 6 de 35 mallas del kit con aristas non-manifold (`Cube.040`: 391; `Cube.033`: 322). Propia: 0 | Misma auditoría | **Comprobado**. Pendiente: reparar una pieza con captura antes/después; no hay Blender instalado |
| Geometria: Eskalak | Importación ×0,01 (MagixBox) y ×0,03 (rusty). `ModuluEskala` 1,25: hueco de puerta 1,38 × 2,08 m, pasillo 3,2 m. Cápsula de 1,8 m | `E4_eskala_kapsula.png`; recorrido físico | **Comprobado**; a 1,25 es decisión pendiente de Ion |
| Geometria: Normalak | Normales unitarias en todas las mallas leídas; vista en color por dirección | `E7_normalak.png`; auditoría | **Comprobado** |
| Geometria: UV | UV0 auditado; UV2 con *Generate Lightmap UVs* en 25 modelos (autorizado por Ion) y en las mallas propias | `E7_uv0.png`, `E7_uv2.png`, `E6_lightmap_*.png` | **Comprobado** |
| Geometria: Piboteak | Hoja `Door`: centro de la caja a (0,55; 0,71; 0) del pivote, fuera del centro de la malla (bisagra: hipótesis). `SM_DatuNukleoa`: pivote en la base | Auditoría, columna de pivote | **Comprobado** (las medidas) |
| Izendatze konbentzioak | Piezas propias con `SM_`, `T_`, `M_`, `PF_`; escena con `Sektorea*`, `Gela_*`, `Argia_Baked_*`, `Argia_Realtime_*`. Mallas del kit (`Cube.040`) como contraejemplo | `Prefabak/Propioak/`, jerarquía | **Comprobado** |
| Texturak: Unique | `T_DatuNukleoa_*` (UV sin solapar, 0..1) y `T_Seinalea_*`, propias y generadas por código | `E2_zorua_bai.png`, `E4_eskala_kapsula.png` | **Comprobado** |
| Texturak: Tileable | `T_Zorua_Tile` en wrap Repeat, UV 0..4, patrón periódico | `E2_zorua_bai.png` | **Comprobado** |
| Texturak: Trim Sheet | `T_Eclipse_Trim_*`: una textura de 4 franjas. `SM_Trim_Hazard` y `SM_Trim_Banda_*` usan franjas distintas | `E4_eskala_kapsula.png`, `E1_bake_bai_*` | **Comprobado**. Moodle tiene una **"Jarduera"** en esta lección cuyo contenido no conozco |
| Argiak | 40 Baked (13 Rectangle en S1, 27 Point en S2 y S3), 4 Realtime propias, 1 Mixed, más las Point realtime del kit en los pasillos. Fría 6500 K, cálida 3800 K | `E1_bake_gabe_*` frente a `E1_bake_bai_*` | **Comprobado** |
| SkyBox-ak | `M_Zerua` (Skybox/Procedural) y luz ambiental; se ve en S2 y S3, sin techo | `E1_bake_bai_Gela_S3_Erreaktorea.png` | **Comprobado**. Sin HDRI: requeriría descarga aprobada |
| LightMap | Bake real con `UnityComputeGPU`: 32 texel/m, 4 atlas, 529 s; S2 y S3 a escala 0,5 | `E1_*`, `E6_lightmap_*.png` | **Comprobado, con defecto visible**: manchas en parte de las paredes |
| Reflection Probe | 8 probes Baked, uno por `Gela_*`, ajustados a la caja | `E2_ispilua_bai.png` frente a `_ez.png`; `E2_zorua_*` | **Comprobado** |
| Light Probe-ak | 841 probes; descartados los que caían en sólidos; 4 siguen negros | `E3_lightprobe_bai.png` frente a `_ez.png`; `E5_probe_sarea_planta.png` | **Comprobado** |
| Zure eskenatokia osatu | `01_Instalazioa`: 3 sectores, ambientes propios, núcleo y señales propias | Todo lo anterior | **Falta**: tu validación en Play, la memoria y confirmar el enunciado |

## 3. Límites conocidos

- **Manchas de lightmap:** bloques oscuros y claros en algunos paneles (por ejemplo, pared norte de Sarrera). Cambian de sitio entre bakes. Descartado: solape de paredes en esquinas. Sin probar: ring buffer; el único intento se colgó antes de hornear.
- **Núcleo y lightmap:** `SM_DatuNukleoa` cambió a las 01:18, después del bake de las 01:13. No se ve problema en las capturas; se resuelve en el próximo bake.
- **Capturas sin sombras del kit:** se quitan en esa sesión a las Point del kit; la escena no cambia.
- **Rendimiento:** con esas sombras activas llegaba a 79 mapas de sombra. Revisar antes del juego.
- **Comparación Box Projection:** las capturas `box_ez` difieren, pero el cambio es sutil.
- **Marca del kit:** las paredes muestran el logo "THE ARK", que es de la textura del kit.
