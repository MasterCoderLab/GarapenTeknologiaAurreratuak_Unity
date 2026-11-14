# ECLIPSE — Recursos de terceros, licencias y atribuciones

**Uso:** ECLIPSE es un proyecto académico, solo para uso local y para presentarlo al profesor (DAM, Tolosaldea LHII). No se comercializa ni se redistribuyen los archivos originales de terceros: están en `Assets/ThirdParty/`, que Git ignora.
**Comprobado el:** 14/11/2026. Solo se registra lo verificado en esta fecha.

## 1. Protagonista: "Sci-Fi Cyborg Character" (HypJo)

| Campo | Valor |
|---|---|
| Obra | "Sci-Fi Cyborg Character" |
| Autor | HypJo — https://sketchfab.com/HypJo |
| Origen | https://sketchfab.com/3d-models/sci-fi-cyborg-character-91f2c3da0a0648b7b371b4d9ff584e9f |
| Licencia | **CC BY 4.0** — https://creativecommons.org/licenses/by/4.0/ ("Author must be credited. Commercial use is allowed.") |
| Comprobación | `license.txt` incluido en la descarga de Sketchfab (copia: `Assets/ThirdParty/HypJo_Cyborg/LICENSE_HypJo_CC-BY-4.0.txt`) |
| Procedencia | El archivo fuente se llama `Meshy_AI_Remeshed_Cyborg_10k_…`: es un modelo **generado con IA (Meshy)** subido por la cuenta HypJo. Riesgo documentado en `Assets/ThirdParty/HypJo_Cyborg/LICENCIA_Y_PROCEDENCIA.md`; aceptable para uso académico |

**Modificaciones nuestras (ECLIPSE v2):**
- escala normalizada a 1,82 m;
- texturas y colores retocados (armadura grafito, neón cian, visor ahumado, bajotraje de tela);
- braga de cuello nueva;
- rig de Mixamo;
- materiales URP.

**Atribución obligatoria** (créditos del juego y memoria):

> This work is based on "Sci-Fi Cyborg Character" (https://sketchfab.com/3d-models/sci-fi-cyborg-character-91f2c3da0a0648b7b371b4d9ff584e9f) by HypJo (https://sketchfab.com/HypJo), licensed under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/). Modified for ECLIPSE: materials, colours, scale, neck gaiter and Mixamo rig.

## 2. Rig del protagonista: Adobe Mixamo

- **Qué es:** autorrig del modelo anterior hecho por Ion en Mixamo (13/11/2026), esqueleto "Standard (65)". El FBX resultante tiene **33 huesos**.
- **Archivo:** `Assets/ThirdParty/HypJo_Cyborg/Cyborg_ECLIPSE_v2_RigMixamo65_Tpose.fbx`, SHA-256 `db522e5710b17f8c876dda173eb98208e69acf90d2858ed741e4a7ccbb8d45ea`.
- **Términos:** los del servicio Mixamo de Adobe (cuenta de Ion). No se redistribuye el FBX en el repositorio.

## 3. Animaciones: Universal Animation Library [Standard] (Quaternius)

| Campo | Valor |
|---|---|
| Autor | Quaternius — https://quaternius.com |
| Licencia | **CC0 1.0 Universal** (dominio público) — https://creativecommons.org/publicdomain/zero/1.0/ |
| Comprobación | `License.txt` y `README.txt` del paquete (copia: `Assets/ThirdParty/Quaternius_UAL/`) |
| Archivo | `Assets/ThirdParty/Quaternius_UAL/UAL1_Standard.fbx` (sin root motion), SHA-256 `21b32d912da3cb93426d974fb945e86f5b2e86970acd2ce89905e0fbf9f1dcc2` |
| Clips usados (A2) | `Idle_Loop`, `Walk_Loop`, `Jog_Fwd_Loop`, `Sprint_Loop`, `Crouch_Idle_Loop`, `Crouch_Fwd_Loop`, `Jump_Loop` |

CC0 no obliga a atribuir. Se cita igualmente: "Animations: Universal Animation Library by Quaternius (CC0)".

## 4. Escenario (registrado antes de A2; no revisado en esta fecha)

- `Sci Fi Modular Pack` (MagixBox) y `Sci-Fi Old rusty props PBR` (KMProd), de la Unity Asset Store, con los términos del **EULA de la Asset Store**. Están en `Assets/ThirdParty/` y no se publican.
- **Pendiente para Ion:** confirmar los términos de cada paquete en "My Assets" y su atribución en los créditos.
- La criatura `Creep` (A3) **no** se importa hasta validar su licencia.

## 5. Reconstruir desde un clon limpio

Git no contiene `Assets/ThirdParty/`.

**Para el protagonista, hay que copiar a mano** estos archivos (sus copias de trabajo están en `~/ECLIPSE_babeskopia/`, fuera del repositorio):

| Destino | Origen local |
|---|---|
| `Assets/ThirdParty/HypJo_Cyborg/Cyborg_ECLIPSE_v2_RigMixamo65_Tpose.fbx` | `~/ECLIPSE_babeskopia/a2_blender/assets_candidatos/hypjo_cyborg/trabajo/mixamo/` |
| `Assets/ThirdParty/HypJo_Cyborg/T_Cyborg_{BaseColor,Emission,MetallicSmoothness_URP}_2k.png` | `~/ECLIPSE_babeskopia/a2_blender/assets_candidatos/hypjo_cyborg/trabajo/eclipse/texturas_v2/` |
| `Assets/ThirdParty/Quaternius_UAL/UAL1_Standard.fbx`, `License.txt`, `README.txt` | Paquete "Universal Animation Library [Standard]" (`~/ECLIPSE_babeskopia/a2_ebaluazioa/ext_ual/`) |

**Después**, en Unity: `ECLIPSE/Jokalaria/Bisuala: aplikatu`, o en batch `BatchJokalariBisuala -eclipseAplikatu`.
- La herramienta comprueba los SHA-256 anteriores.
- Rehace la importación (Humanoid, Avatar sin dedos, clips sin root motion), los materiales, el Animator y el prefab `Assets/Resources/Jokalaria/PF_Jokalaria_HypJo.prefab`.

**Si faltan esos archivos,** el juego sigue funcionando con la cápsula anterior, y el log muestra `ECL_JOKALARIA_BISUALA_ERROR`.
