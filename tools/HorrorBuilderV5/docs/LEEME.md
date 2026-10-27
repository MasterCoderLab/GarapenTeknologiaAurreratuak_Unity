# Horror Builder v5 — Hormak eta zoruak / PBR material pass

## Objetivo

Corregir **solo el aspecto de paredes y suelos** de `Laberintoa_Horror`, después de Horror Builder v4, manteniendo el survival horror ya probado (salto, linterna, tres enemigos, escopeta, vidas, victoria y derrota, HUD en euskera). El usuario no quiere más mejoras gigantes de gráficos: necesita un acabado coherente, rápido y seguro.

Se incorporan **fotografías PBR reales en 2K** de la biblioteca Poly Haven, no materiales simulados proceduralmente:

| Superficie | Recurso original | Mapas descargados | Uso |
|---|---|---|---|
| Hormigón de búnker | https://polyhaven.com/a/concrete | Diffuse / Normal GL / Roughness | Paredes, dos cierres traseros, acceso y techo |
| Suelo gastado | https://polyhaven.com/a/concrete_floor_worn_02 | Diffuse / Normal GL / Roughness | Suelos del laberinto |
| Barro de bosque | https://polyhaven.com/a/mud_forest | Diffuse / Normal GL / Roughness | Terreno, extensión de v4 y sendero |

**Licencia de los tres: CC0** (ver páginas originales de Poly Haven). Se pueden redistribuir y alojar en GitHub público según esa licencia. Atribución recomendada en documentación, aunque no sea obligatoria.

La arquitectura del paquete es importante: las descargas se hacen **en Fedora** mediante URLs oficiales de `dl.polyhaven.org`, pues no han podido incluirse como ficheros adjuntos desde el entorno de preparación. No afirmar que el ZIP ya contiene las fotografías PBR.

## Rutas relevantes

- Repositorio público de trabajo: `~/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity`
- Proyecto: `Laberintoa3D_Final` bajo esa ruta
- Repositorio privado: `~/Projects/Laberintoa3D_Horror_Backup_Privado`
- Escena: `Assets/Laberintoa/Scenes/Laberintoa_Horror.unity`
- Materiales nuevos: `Assets/Laberintoa/Horror/BuilderV5/Materials`
- Texturas nuevas: `Assets/Laberintoa/Horror/BuilderV5/Textures`
- Herramientas propias nuevas: `Assets/Editor/HorrorBuilderV5` y `tools/HorrorBuilderV5`

## Instrucciones

Descarga `HorrorBuilder_v5.zip` y extráelo en `~/Downloads`:

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v5.zip" -d "$HOME/Downloads"
```

1. Auditoría (puede realizarse con Unity abierto):

   ```bash
   bash "$HOME/Downloads/HorrorBuilder_v5/horror-builder-v5.sh" --audit
   ```

   Debe mostrar `HBB5_AUDIT_OK`. **Si el repositorio ya no tiene 84 cambios autorizados, no fuerza nada; detente y revisa.**

2. Descarga original (puede hacerse con Unity abierto):

   ```bash
   bash "$HOME/Downloads/HorrorBuilder_v5/horror-builder-v5.sh" --fetch
   ```

   Obtendrá 9 JPG de 2K de Poly Haven en la carpeta `~/Downloads/HorrorBuilder_v5/descargas/`. Guarda SHA256 local. `HBB5_FETCH_OK` significa que las nueve descargas pasan verificaciones de tamaño y estructura JPEG; no acredita un checksum publicado externamente.

3. **Guardar y cerrar Unity**. Aplicar en la escena:

   ```bash
   bash "$HOME/Downloads/HorrorBuilder_v5/horror-builder-v5.sh" --apply
   ```

   Debe mostrar `HBB5_PRECHECK_OK`, `HBB5_OK`, `HBB5_LOCAL_OK`. Si no aparecen: **NO REPETIR `--apply` automáticamente**, conservar log y revisar.

4. Abrir Unity, cargar `Laberintoa_Horror`, inspeccionar pasillo oscuro con la linterna y bosque, entrar en Play y validar.

## Garantías y limitaciones

- El script no invoca `git add`, `git commit` ni `git push`. Checkpoints privados y públicos seguirán separados tras la revisión visual.
- El script nunca toca `SampleScene`, los scripts de IA, NavMesh, escopeta, salto ni los de interfaz.
- Compara SHA256 de esos archivos antes y después; construye un respaldo de escena en `~/HorrorBuilderV5_XXXXXXXX/`.
- El C# modifica referencias a **materiales de Renderer** en la escena; no cambia vértices ni colliders.
- Si falla, el shell intenta restaurar la escena y elimina únicamente los recursos nuevos v5, sin tocar v1–v4. Conserva las texturas descargadas en Downloads.
- Los JPG se importan con mipmaps; los mapas normales se importan como `NormalMap`; el mapa `rough` se convierte a canal alfa de un mapa metallic/smoothness URP para aprovechar su variación.
- Hay tres variantes de tiling de paredes para limitar el estiramiento; el suelo exterior usa más repetición para no ver píxeles gigantes.
- La referencia visual será **mejor que las texturas v4**, pero depende de iluminación, escala, calidad y FPS; no podemos prometer el resultado final sin ejecutarlo en tu Unity 6.
- La importación de fotografías puede aumentar el tamaño del repositorio y tiempos de carga. Son ~9 mapas 2K JPG más máscaras PNG/materiales; el 2K minimiza el coste.

## Siguiente paso tras Play

Una vez aceptado el aspecto, realizar **primero** el checkpoint privado completo y verificar, y **después** el público con lista de archivos autorizados, incluyendo solamente los recursos CC0 y el código propio.


## Revisión correctiva del auditor (27/10/2026)

El primer ZIP rechazaba erróneamente `HorrorGameManager.cs.meta` y
`HorrorShotgun.cs.meta` como cambios ajenos. Estos metadatos proceden de
las clases del Builder v3 y son necesarios para conservar sus GUID de Unity.
La lista blanca de `status_check` ahora autoriza **solo estas dos rutas
adicionales exactas**, conservando el conteo de **84 archivos**,
la prohibición de staging y las verificaciones del repo privado.
No se ha modificado la construcción C#, los materiales ni la lógica de Git.
Si aparece otro rechazo, detenerse y revisar la ruta concreta; no desactivar
la auditoría ni usar `git clean`/`reset`.


## Corrección CS0619 + carpeta Materials

En Unity 6000.6, tras crear `Materials/` vacía con Bash el Editor puede no
registrarla al iniciar batchmode. Ahora `horror-builder-v5.sh` crea
`Materials/LEEME_MATERIALAK.txt` antes de abrir Unity, y C# puede forzar
una segunda importación si fuera necesario. Marcador esperado:
`HBB5_MATERIALS_FOLDER_OK`.

Conservar las nueve texturas en `descargas/` al descomprimir esta corrección.
No se hacen commits ni pushes y la escena se restaura si Unity devuelve error.
