# Modelo del Abrams

`AbramsBody.tscn` es el modelo estilizado del Abrams, inspirado en fotografías del M1A2 Abrams del Ejército de EE. UU.: [referencia visual DVIDS](https://www.dvidshub.net/image/6745942/m1a2-abrams-live-fire).

La geometría es propia. Incluye casco y torreta con blindaje inclinado, faldones laterales, ruedas, cañón con evacuador, escotillas, ópticas, ametralladora, antenas, cesta de estiba y rejillas traseras. El blindaje conserva el color arena; `LeftTrim` y `RightTrim` reciben el color de cada facción.

Abre esta escena para editar las piezas. `Scenes/Units/Abrams.tscn` instancia el modelo como `Body` y conserva la lógica, estadísticas y anillos del tanque. `SmallTank.tscn` mantiene su modelo original.

El generador `Tools/BuildAbrams.gd` permite reproducir la geometría con `godot --headless --path . --script res://Tools/BuildAbrams.gd`. Solo es necesario si quieres regenerarla: sobrescribe `AbramsBody.tscn`, por lo que perdería cambios manuales hechos en ese archivo.

# Modelo del Humvee

`HumveeBody.tscn` es el modelo del Humvee: carrocería color arena con estación de armas remota y ametralladora en el techo, cuatro ruedas, ventanas, retrovisores y antena. `LeftTrim`, `RightTrim` y `WeaponLight` reciben el color de cada facción. Está construido con primitivas y `Scenes/Units/Humvee.tscn` lo instancia como `Body`.

# Modelo del Leopard

`LeopardBody.tscn` es el modelo del Leopard (inspirado en el Leopard 2A6): casco bajo con glacis, torreta en cuña con cañón largo y freno de boca, siete ruedas por lado con rodillos de retorno, faldones laterales, lanzahumos, cruz blanca en la torreta y camuflaje de tres tonos (verde, negro y marrón) hecho con parches planos. `LeftTrim`, `RightTrim` y `SightLight` reciben el color de cada facción.

`Scenes/Units/Leopard.tscn` (antes `MediumTank.tscn`) lo instancia como `Body`. Todavía no se puede fabricar: no tiene tipo de unidad propio en el juego.
