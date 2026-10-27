# Checklist técnica — Horror Builder v2

- `--audit`: proyecto limpio; commits esperados fd55fd79 / f7a47d7; ningún v2 instalado.
- `--apply`: `HBB2_PRECHECK_OK`, `HBB2_ASSETS_OK`, `HBB2_OK`, `HBB2_STATUS_OK`.
- Escena: única raíz `HB_MunduOsoa_v2`; hijos `HB2_Industrial`, `HB2_ForestDetail`, `HB2_CabinDetail`, `HB2_Ambiente`.
- Bosque: mínimo 45 pinos conservando cada `HB_Nav_Tronco`; 35+ grupos hierba; ruta despejada.
- Búnker: cuatro techos sin agujeros y pasillos transitables; ningún Collider agregado por v2.
- Chabola: puerta abierta, porche visual, escopeta aún no funcional.
- `SampleScene`, NavMesh_BuilderV1, scripts enemigo, animaciones y BuilderV1 SHA-256 **sin cambios**.
- Git: respaldo privado todavía limpio; público solo scene + v2 nuevo; **sin commit ni push**.
- Visual: texturas no magenta; niebla no impide jugar; F enciende linterna; FPS aceptables.

Si hay problemas, entregar `~/LaberintoaHorror_BuilderV2_XXXXXXXX/unity.log` y `git status --short` de público. No ejecutar Git restore/clean/reset.
