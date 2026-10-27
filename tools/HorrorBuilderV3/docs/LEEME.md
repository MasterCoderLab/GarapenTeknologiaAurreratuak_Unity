# Horror Builder v3 — jugabilidad (Unity 6000.6.0f1, Fedora)

## Objetivo

Proyecto de 2.º DAM, escena `Laberintoa_Horror`. Continuamos después de **v2** y `HorrorNavMeshStartFix`:

- Personaje con **3 vidas**, invulnerabilidad temporal de 1,5 s tras cada ataque.
- **Escopeta de la chabola**: acercarse a la mesa, pulsar **E** para recogerla.
- **Clic izquierdo** para disparar; 6 cartuchos cargados y 12 en reserva, **R** para recargar durante 1,4 s. Hay munición limitada.
- Los **3 enemigos** tienen 2 puntos de vida cada uno. Disparos por Raycast: las paredes bloquean las balas. Al morir, se desactiva cada enemigo y suma al contador.
- **Victoria** al derrotar a los tres monstruos. **Derrota** si la vida llega a cero.
- HUD sencillo integrado (`OnGUI`), compatible con el proyecto actual. **No es una UI artística final**; tampoco añade sonidos ni un menú de reinicio automático.

## Seguridad

- Comprueba commits públicos `fd55fd79` y privados `f7a47d7` (base de v1), ramas, ausencia de staging y rutas autorizadas de v2 + NavFix.
- Respaldo de la escena y del movimiento del jugador ANTES de modificar.
- **Únicos ficheros previamente existentes editados**: `Laberintoa_Horror.unity` y `HorrorPlayerMovement.cs` (solo el método `PerderVida`).
- **No toca** `SampleScene`, el NavMesh, los scripts `HorrorEnemyAI`, `HorrorEnemyAnimation`, `HorrorNavMeshRuntime` ni el material Creep.
- Instala archivos nuevos propios bajo `Assets/Editor/HorrorBuilderV3/`, `Assets/Laberintoa/Scripts/` y `tools/HorrorBuilderV3/`.
- Comprueba SHA256 de elementos protegidos; falla ante rutas inesperadas; intenta rollback local de la escena y archivos v3 si falla.
- **NO hace `git add`, `git commit` ni `git push`**. El privado permanece sin tocar. Primero probar el juego; después respaldar en el privado y en el público por separado.

## Uso

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v3.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v3/horror-builder-v3.sh" --audit
# Guarda y CIERRA Unity antes de la construcción.
bash "$HOME/Downloads/HorrorBuilder_v3/horror-builder-v3.sh" --apply
```

### Marcadores

- `HBB3_AUDIT_OK`: inspección bash sin modificaciones.
- `HBB3_LIVES_PATCH_OK`: cambio localizado del método `PerderVida`.
- `HBB3_PRECHECK_OK`: Unity verifica escena y NavMesh Fix.
- `HBB3_OK`: escena guardada y reabierta con referencias v3.
- `HBB3_STATUS_OK`: cambios Git locales autorizados.
- `HBB3_LOCAL_OK`: terminación del lanzador.

## Prueba manual antes de guardar en Git

1. Arranca Play y comprueba `HORROR_NAV_READY: 3/3 agents on NavMesh`, **sin** los tres avisos previos de agente inválido.
2. La consola no debe contener errores rojos de compilación ni `HORROR_AI_ERROR`.
3. Camina por el búnker (WASD), enciende/apaga linterna con F y sal hacia la chabola.
4. Junto a la mesa, pulsa E. Verás `HORROR_PICKUP` y la escopeta básica en primera persona.
5. Dispara clic izquierdo y recarga R. Comprueba los contadores, que paredes impidan impactar a un enemigo detrás y la eliminación tras 2 aciertos.
6. Los ataques enemigos deberían emitir `HORROR_LIFE: 2/3`, `1/3`, `0/3`; comprueba la inmunidad de 1,5 s.
7. Tras eliminar los tres enemigos deberá salir `HORROR_VICTORY`; con cero vidas, `HORROR_GAME_OVER`.
8. Para reiniciar durante pruebas, para Play y vuelve a iniciarlo. **No guardar la escena durante Play**.

## Limitaciones conocidas

No hay sonido, animación de retroceso, HUD artístico, selector de dificultad, recogida de munición adicional ni sistema de reinicio de partida dentro del juego. Todo eso puede ir después. La ejecución de Unity y la prueba manual no se pueden completar desde el contenedor de ChatGPT: el código es una propuesta que el Editor debe compilar y validar en tu Fedora.
