# Checklist de prueba (Unity 6000.6.0f1)

La herramienta usa la API nativa del Editor. Un `HBB1_OK` significa que se guardó y reabrió la escena, **no** que los comportamientos audiovisuales sean perfectos.

## Prueba 1: geometría

En Hierarchy buscar `HB_MunduOsoa_v1`: debe contener 4 techos, `HB_Irteera`, `HB_BasoGaua`, `HB_Chabola` y `HB_BunkerDekorazioa`. Abrir `Laberintoa_Horror`, no `SampleScene`.

## Prueba 2: Game

WASD o flechas; pulsar `F` para encender o apagar la linterna. Confirmar que el jugador no atraviesa paredes y que se puede cruzar la puerta junto a x25.5, z22. Si la cámara está demasiado baja/alta, reportarlo antes de seguir.

## Prueba 3: ruta exterior

Recorrer la salida hasta el suelo de bosque, llegar a la chabola y entrar por su puerta abierta; ver el objeto llamado `HB_Escopeta_PLACEHOLDER` sobre la mesa. **No dispara todavía**.

## Prueba 4: enemigos

Comprobar si un monstruo continúa patrullando y nos persigue al salir al bosque. Validar que no atraviesa paredes ni árboles; la inteligencia anterior sigue activa.

## Prueba 5: control de versiones

Hasta validar las cuatro pruebas anteriores, no ejecutar commit/push de la construcción. El estado de animaciones anterior sí se versionó en ambos GitHub al comenzar `--apply`. El nuevo trabajo queda en Fedora bajo `feature/laberintoa-horror`.

## Si falla

Copiar las últimas líneas del log indicado por el script con `tail -n 90 RUTA/unity.log`. No ejecutar limpieza manual, `git reset --hard` ni segundo `--apply` hasta identificar el fallo.
