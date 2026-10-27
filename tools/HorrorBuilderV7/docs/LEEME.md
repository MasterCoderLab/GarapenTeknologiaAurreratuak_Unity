# Horror Builder v7 — dos correcciones visuales

Estado de partida: v6 ya guardado en ambos GitHub, repositorios limpios. Publico `c5370157`, privado `b2691f0`.

## Objetivo

1. Escopeta y linterna visibles por **URP Camera Stacking**. La cámara secundaria `HB7_LehenPlanokoKamera` se dibuja encima del escenario y solo ve la capa interna 30. Los dos modelos se retraen al acercarse a paredes y suelo con `HorrorViewmodelSafety`. El `Spot Light` que ilumina el mundo **no** se mueve a la cámara secundaria.
2. Clonar tres materiales de hormigón v6 con normal map más suave (`0.12`) y menor brillo (`0.27`). Solo 45 paredes y 8 tramos/techos relacionados cambian su referencia de material. Texturas 2K de Poly Haven, materiales v6 y suelos permanecen sin alterar.

## Protecciones

- No editar componentes de física, enemigos, NavMesh, scripts de arma, linterna, salud o HUD; solo añade un script independiente a `Main Camera`.
- No modificar `SampleScene`, `ProjectSettings`, `Packages` ni los repositorios Git.
- Rutas absolutas de respaldo y repositorio privado dentro de la asignatura.
- Exigir los dos repositorios limpios y comparados con remoto antes de aplicar; respaldar escena y hashes de los archivos críticos; rollback de V7 si falla.
- No volver a ejecutar `--apply` después de `HBB7_OK`, sin revisar antes.

## Uso

```bash
unzip -o "$HOME/Downloads/HorrorBuilder_v7_post_checkpoint.zip" -d "$HOME/Downloads"
bash "$HOME/Downloads/HorrorBuilder_v7/horror-builder-v7.sh" --audit
# Guardar escena y File > Exit (Unity cerrado)
bash "$HOME/Downloads/HorrorBuilder_v7/horror-builder-v7.sh" --apply
```

## Verificación en Unity

- En Play, comprobar `HORROR_VIEWMODEL_READY` y `HORROR_NAV_READY: 3/3`.
- Pegar la cámara a paredes y mirar al suelo: el arma y linterna deben retraerse pero permanecer visibles, sin ser cortadas por la pared.
- Escopeta: E para coger, clic para disparar, R recargar; no cambia el raycast de ataque.
- Evaluar juntas de hormigón con linterna al mirar arriba y abajo. Z-fighting por superficies coplanares requeriría una corrección geométrica separada; V7 no altera ninguna pared.
- Revisar que la cámara Overlay muestra el modelo correctamente. Si el Renderizador del proyecto no admite camera stacking, enviar log y no modificar manualmente ajustes URP.

## Marcadores

`HBB7_AUDIT_OK`, `HBB7_PRECHECK_OK`, `HBB7_OK`, `HBB7_LOCAL_OK`, `HBB7_RESULTADO: OK`.

## Licencias

Sin descargas nuevas: solo clona materiales locales que ya emplean texturas Poly Haven CC0. El código y materiales nuevos son propios.
