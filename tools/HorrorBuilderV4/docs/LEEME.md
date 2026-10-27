# Horror Builder v4 — Laberintoa Horror — 2AG3 (2026/27)

## Objetivo acotado

Continuar **exactamente** a partir del Horror Builder v3 ya probado. No añadir assets externos, no perseguir el fotorrealismo, no rehacer el mapa o la IA.

1. Salto con **Espacio**, solo si hay suelo bajo los pies. Se añade `HorrorPlayerJump.cs` al jugador existente, sin reemplazar `HorrorPlayerMovement` y sin introducir dobles saltos. **Importante:** el techo del búnker es bajo; puede limitar físicamente la altura allí. Comprobar también el salto al aire libre.
2. Linterna físicamente **visible** en primera persona, sostenida a la izquierda, sin tapar la escopeta que aparece a la derecha. **F** sigue encendiendo/apagando el Spot Light original.
3. Mejor acabado de las **45 paredes** existentes con un material URP basado en la textura de hormigón del Builder v2, más detalles metálicos decorativos **sin colisiones**. Mantiene las posiciones y BoxColliders originales.
4. **Cerrar solo los dos huecos traseros** de ancho ~2 m formados entre `ParedOeste2` y `ParedOeste1` en `Paredes` y `Paredes_1_0`. El Editor obtiene las coordenadas **mediante Collider.bounds en Unity** y verifica posición (z≈-7), ancho, y alineación; si el mapa difiere, se detiene. La salida principal `HB_Irteera`, x≈25.5, z≈22, sigue abierta.
5. **Ampliar terreno** exterior debajo del bosque actual. Reutiliza el material de tierra del Builder v2, y aporta pequeñas matas sin colisión. La capa nueva queda unos centímetros **por debajo** del suelo anterior.
6. **Textos visibles en euskera**: `HorrorGameManager.cs` y `HorrorShotgun.cs`. Solo se cambian las frases de UI y las dimensiones del HUD; el comportamiento de victoria, derrota, munición y daño sigue siendo el mismo. Los nombres internos históricos de objetos, scripts, prefabs y logs no se renombran para no romper referencias.

## Cómo funciona

`--audit`: solo consulta de estado Git y archivos; requiere que la instalación v3 coincida con las fuentes incluidas en `reference/` para los 2 scripts de UI. Verifica los dos repositorios SIN MODIFICARLOS.

`--apply`: **con Unity cerrado**, hace copia de seguridad local de escena y scripts, instala el script de salto, las dos versiones de UI traducidas y el Builder Editor. Unity batch abre solamente `Laberintoa_Horror`, valida jerarquía, posiciones de las dos aberturas, componentes de juego y referencia de NavMesh, genera 3 materiales URP, modifica solo la escena Horror, guarda y reabre. Revisa hashes de archivos protegidos y estado Git esperado.

**Ninguna operación Git de escritura: NO `git add`, NO `git commit`, NO `git push`, NO sincronización privada automática.** Los 2 repositorios se guardarán después de las pruebas por separado.

## Instalación desde Fedora

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v4.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v4/horror-builder-v4.sh" --audit
# Si la auditoría pasa: guardar la escena en Unity, detener Play y cerrar Unity completamente.
bash "$HOME/Downloads/HorrorBuilder_v4/horror-builder-v4.sh" --apply
```

Marcadores de salida: `HBB4_STATUS_OK`, `HBB4_PREAUDIT_OK`, `HBB4_AUDIT_OK` (auditoría); `HBB4_PRECHECK_OK`, `HBB4_OK`, `HBB4_LOCAL_OK` (construcción).

El ZIP no puede ejecutarse contra Unity desde el entorno de creación; **la ejecución batch de tu Fedora es la validación real**. Ante error, el script intenta restaurar el estado local v3 y elimina exclusivamente recursos nuevos de v4. Conserva respaldo/log en `~/HorrorBuilderV4_XXXXXXXX` y **NO debe repetirse automáticamente** si falla.

## Protección

- `Assets/Laberintoa/Scenes/SampleScene.unity` original, y sus prefabs/licencias, intactos.
- El archivo binario `NavMesh_Horror_BuilderV1.asset` conserva su hash; scripts AI, Animator, NavMesh Runtime, jugador y vida también se comprueban con hash.
- No se cambia `Packages`, `ProjectSettings`, ni el ajuste `Active Input Handling = Both`. El aviso de Input deprecado de Unity puede persistir: **no es objetivo de v4**.
- Los colliders de paredes anteriores no se mueven ni eliminan; se añaden solamente los 2 colliders de cierre y el suelo exterior.
- El juego ya probado (vida, disparos, recarga, victoria/derrota) no se reimplementa.

## Riesgos y decisiones

- La **NavMesh no se recalcula**. Los huecos traseros cerrados tenían geometría vacía y el archivo NavMesh antiguo puede contener triángulos que los cruzan; probar que los agentes no intentan atravesar las paredes añadidas. De haber conflicto, generar una corrección de navegación focalizada, no cambiar la IA precipitadamente.
- Una gran plancha de suelo adicional está pensada para eliminar el fondo gris, no para ser una isla de navegación para enemigos. El jugador podrá caminar más afuera, mientras los monstruos siguen restringidos al NavMesh conocido.
- La linterna visible es un modelo ligero de primitivas y **no una mano animada**. Es adecuada para la entrega actual.
