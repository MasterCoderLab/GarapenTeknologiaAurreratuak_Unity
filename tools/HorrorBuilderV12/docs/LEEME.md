# Horror Builder v12 — paquete completo corregido

Esta distribución sustituye al ZIP anterior. No es un parche sobre una instalación previa.

**Cambio:** en `HorrorBuilderV12.Apply()` el bucle que recorre las raíces de la escena utiliza `enemyRoot`, evitando el error de compilación CS0136 con el objeto `root` del escenario del menú.

El script valida hashes de los dos archivos C# antes de tocar el proyecto, exige que el estado Git sea limpio y crea un respaldo antes de ejecutar Unity en Batch.

## Instrucciones Fedora

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v12_corregido.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v12_corregido/horror-builder-v12.sh" --audit
# Solo si termina HBB12_AUDIT_OK y Unity está cerrado:
bash "$HOME/Downloads/HorrorBuilder_v12_corregido/horror-builder-v12.sh" --apply
```

Salida de auditoría esperada: `HBB12_SOURCE_FIX_OK`, `HBB12_STATUS_OK`, `HBB12_AUDIT_OK`.
Salida de aplicación esperada: `HBB12_RESULTADO: OK`. Si da error, NO ejecutar de nuevo sin revisar log y rollback.

No se realizan commits ni pushes; se conserva el estado anterior de ambos repositorios. Solo `Hasiera`, `EditorBuildSettings`, script del menú y texto `ETSAIA` son cambios previstos.
