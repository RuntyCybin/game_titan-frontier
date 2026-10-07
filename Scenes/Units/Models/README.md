# Modelo del Tank

`AbramsBody.tscn` es el modelo estilizado del Tank, inspirado en fotografías del M1A2 Abrams del Ejército de EE. UU.: [referencia visual DVIDS](https://www.dvidshub.net/image/6745942/m1a2-abrams-live-fire).

La geometría es propia. Incluye casco y torreta con blindaje inclinado, faldones laterales, ruedas, cañón con evacuador, escotillas, ópticas, ametralladora, antenas, cesta de estiba y rejillas traseras. El blindaje conserva el color arena; `LeftTrim` y `RightTrim` reciben el color de cada facción.

Abre esta escena para editar las piezas. `Scenes/Units/Tank.tscn` instancia el modelo como `Body` y conserva la lógica, estadísticas y anillos del tanque. `SmallTank.tscn` y `MediumTank.tscn` mantienen sus modelos originales.

El generador `Tools/BuildAbrams.gd` permite reproducir la geometría con `godot --headless --path . --script res://Tools/BuildAbrams.gd`. Solo es necesario si quieres regenerarla: sobrescribe `AbramsBody.tscn`, por lo que perdería cambios manuales hechos en ese archivo.
