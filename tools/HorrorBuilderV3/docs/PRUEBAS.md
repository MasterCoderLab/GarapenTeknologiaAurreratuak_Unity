# Pruebas de Horror Builder v3

1. Ejecuta `--audit`; detente si no muestra `HBB3_AUDIT_OK`.
2. Si hace falta prueba primero el NavFix en Play: `HORROR_NAV_READY: 3/3 agents on NavMesh`. Si no aparece, NO continúes con `--apply`; comparte Console.
3. Guarda y cierra Unity; ejecuta `--apply`; exige `HBB3_PRECHECK_OK`, `HBB3_OK`, `HBB3_LOCAL_OK`.
4. Abre Unity y entra en Play: movimiento, linterna F, tres monstruos navegando.
5. Camina a la chabola, E, clic izquierdo, R, dos disparos por enemigo, HUD de vidas, victoria/derrota.
6. No hagas Git hasta tener pruebas satisfactorias. Los commits privado y público deben hacerse por separado.
