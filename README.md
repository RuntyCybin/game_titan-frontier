# CYBIN · Frontera de Titanio

Prototipo jugable de RTS 3D en **Godot 4.7.2 .NET y C#**, con un campo de batalla industrial y dos facciones. Inspirado en la producción y el control táctico de Command & Conquer y en la estética de ejércitos mecanizados de Ashes of the Singularity y Planetary Annihilation. Todos los modelos y el escenario son escenas editables con geometría propia; no necesita assets externos.

## Ejecutar

1. Instala **Godot 4.7.2 .NET** y el **SDK .NET 10** (son las versiones utilizadas en este equipo).
2. Importa `project.godot` en Godot.
3. Pulsa **Compilar** y después **F5** para abrir el menú principal. Selecciona un mapa y pulsa **Iniciar escaramuza**. **F6** sobre una escena de partida permite probar ese mapa directamente.

También puedes compilar con `dotnet build` y ejecutar `godot --path .` si Godot está en el PATH. La primera restauración necesita los paquetes Godot de NuGet o la carpeta `GodotSharp/Tools/nupkgs` incluida con Godot .NET: `dotnet build --source "RUTA_A_GODOT/GodotSharp/Tools/nupkgs"`.

## Menú y mapas

El proyecto arranca en **Scenes/UI/MainMenu.tscn**. Hay dos escenarios disponibles:

| Mapa | Distribución | Escena de partida |
| --- | --- | --- |
| Frontera de Titanio | Zona industrial; 5 depósitos; bases al suroeste y noreste | `Scenes/Main.tscn` |
| Cuenca de Ámbar | Llanura desértica; 7 depósitos; bases al oeste y este, con recursos al norte y sur | `Scenes/AmberSkirmish.tscn` |

Las vistas tácticas del menú leen las posiciones de bases y depósitos de las escenas reales. Selecciona una tarjeta y pulsa **Iniciar escaramuza**. El menú admite navegación con Tab y activación con Enter/Espacio. **Salir** cierra el juego.

Desde **Esc → Volver al menú**, o al finalizar una partida, puedes elegir otro mapa. Reiniciar conserva el mapa actual y reinicia recursos, unidades y producción. Volver al menú descarta la partida; la selección del mapa se recuerda mientras el juego sigue abierto.

El nuevo terreno es editable en `Scenes/Maps/AmberBasin.tscn`; su configuración inicial de unidades y cámara está en `Scenes/AmberSkirmish.tscn`. El menú también es una escena editable con controles nativos de Godot.

## Primera partida

En Frontera de Titanio comienzas al suroeste y el enemigo ocupa el noreste; en Cuenca de Ámbar empiezas al oeste y el enemigo al este. En ambos mapas dispones de un centro de mando, un extractor y cuatro vehículos. **Destruye su centro de mando para ganar; perder el tuyo termina la partida.** Ambos centros de mando tienen defensas automáticas.

Recibes 4 de titanio por segundo y 12 adicionales por extractor. Acerca vehículos a los depósitos turquesa y construye extractores para ampliar la economía. La primera oleada llega tras 55 segundos; las siguientes aumentan en tamaño y frecuencia. Puedes atacar directamente la base enemiga.

| Control | Acción |
| --- | --- |
| Clic izquierdo / arrastrar | Seleccionar unidad / grupo |
| Mayús + selección | Añadir unidades |
| Clic derecho | Mover en formación o atacar enemigo |
| F, después clic izquierdo | Avanzar atacando enemigos encontrados |
| X | Detener movimiento y cancelar objetivo; mantiene defensa automática |
| WASD / flechas | Desplazar cámara |
| Rueda | Zoom |
| Espacio | Centrar cámara en tu base |
| Tab | Seleccionar todos tus vehículos |
| Ctrl + 1–5 / 1–5 | Guardar / recuperar grupo |
| Q / E / R | Producir explorador / tanque / artillería |
| B, después clic en depósito | Construir extractor (250 Ti, requiere fuerzas a 18 m) |
| Esc / clic derecho | Cancelar orden de ataque o construcción |
| Esc sin orden pendiente | Pausar / continuar |
| Clic izquierdo / derecho en minimapa | Mover cámara / ordenar desplazamiento |

La interfaz también ofrece botones de producción, pausa y reinicio. Límite: 60 vehículos del jugador, incluyendo unidades en producción; cola máxima de 8.

| Unidad | Coste | Tiempo | Papel |
| --- | --- | --- | --- |
| Explorador | 100 | 4 s | Rápido, explora y permite expandirse |
| Bastión | 180 | 7 s | Blindado resistente para el frente |
| Artillería | 260 | 10 s | Largo alcance y daño de área; necesita protección |

## Editar visualmente en Godot

Abre `Scenes/Main.tscn`: el mapa, los depósitos, la cámara y las diez entidades iniciales se ven directamente en la vista 3D. Si ya estaba abierta durante la conversión, acepta recargar los archivos modificados externamente o cierra y vuelve a abrir la escena. Compila C# una vez para que aparezcan las propiedades del Inspector.

| Escena | Contenido editable |
| --- | --- |
| `Scenes/Units/Scout.tscn` | Explorador |
| `Scenes/Units/Tank.tscn` | Tanque Bastión |
| `Scenes/Units/Artillery.tscn` | Artillería |
| `Scenes/Buildings/Headquarters.tscn` | Centro de mando |
| `Scenes/Buildings/Extractor.tscn` | Extractor |
| `Scenes/Maps/TitaniumFrontier.tscn` | Suelo, carretera, rocas, iluminación y posiciones de depósitos |
| `Scenes/Maps/TitaniumDeposit.tscn` | Modelo compartido de los depósitos |

Selecciona la raíz de una unidad o edificio para editar **Team**, **Max Health**, **Speed**, **Range**, **Damage**, **Fire Interval** y **Radius** en el Inspector. Las estadísticas se leen de la escena; ya no se sustituyen por valores fijos al iniciar. Activa **Editor Preview → Preview Attack Range** para ver el círculo tenue y ajustar **Range** visualmente. Esta opción solo previsualiza el alcance en el editor: durante la partida aparece al seleccionar el vehículo.

Dentro de **Body** puedes mover o cambiar las mallas de casco, torreta, cañón y otros componentes. Los componentes con metadato `team_role` reciben los colores de facción automáticamente. El indicador `AttackRange` permite ajustar el material (por ejemplo, su opacidad); su radio se controla con **Range**. Conserva los nodos `Body`, `SelectionRing` y `AttackRange`, utilizados por el script.

Edita una escena de unidad para cambiar todas sus instancias, incluidas las que se produzcan después. Para modificar solo un vehículo inicial, selecciona su instancia en `Main.tscn` y cambia sus propiedades allí. Puedes duplicar unidades iniciales como hijos directos de `Battlefield` y moverlas; la partida las registra automáticamente. Mantén un centro de mando por equipo. Para mover o duplicar depósitos, edita los hijos de `Deposits` en la escena del mapa. La partida lee esas posiciones al arrancar. El tamaño jugable sigue siendo de 120 × 120 m.

Los cambios en escenas se ven en el editor al guardarlos; los cambios en código C# requieren compilar. Reinicia la partida para aplicar cambios a una partida nueva. La interfaz y los efectos temporales de disparos siguen creándose al ejecutar.

## Código

- `Scripts/Battlefield.cs`: registro de entidades y depósitos de la escena, economía, producción mediante PackedScene, órdenes, grupos, oleadas y estado de partida.
- `Scripts/CombatEntity.cs`: estadísticas, vehículos y edificios, movimiento con separación local, adquisición de objetivos y salud.
- `Scripts/BattleHud.cs`: interfaz, minimapa interactivo, selección, barras de salud y pausa.
- `Scripts/Visuals.cs`: primitivas, materiales y anillos de selección.
- `Scenes/Main.tscn`: punto de entrada con instancias editables del mapa, los vehículos y edificios iniciales.

## Validación

`dotnet build` verifica C# y los generadores de Godot.

`godot --headless --path . --fixed-fps 60 res://Scenes/Main.tscn -- --smoke-test` ejecuta una partida de prueba de 180 fotogramas, comprueba propiedades sobrescritas en escenas, independencia de materiales y círculos entre instancias, economía inicial, pago de producción, construcción de extractores, rechazo de depósitos ocupados, daño, muerte, movimiento, oleadas, pausa, victoria y derrota. Termina con código 0 si pasa o 1 si falla. Este modo modifica la partida para comprobar los sistemas; no debe usarse para jugar. Sus valores esperados corresponden a la escaramuza original: al cambiar el balance o las unidades iniciales hay que actualizar las comprobaciones.

`godot --path . res://Scenes/Main.tscn -- --capture-preview` genera `preview.png` después de 60 fotogramas y cierra la partida.

La misma prueba de partida se puede ejecutar con `res://Scenes/AmberSkirmish.tscn` para verificar el segundo mapa.

`godot --headless --path . res://Tests/MenuFlow.tscn` verifica selección e inicio de ambos mapas, estado inicial, reinicio en el mismo mapa, vuelta al menú desde pausa y vuelta después de victoria.

`godot --path . -- --capture-menu` genera `menu-preview.png` y cierra el menú.

## Alcance de esta versión

Son escaramuzas iniciales en dos mapas planos, con gráficos de primitivas editables y combate instantáneo representado por trazas. La IA envía oleadas y defiende su base; no administra una economía propia. El mapa es visible por completo. El movimiento usa separación local, sin búsqueda de rutas alrededor de obstáculos complejos. Los depósitos son inagotables. No incluye todavía niebla de guerra, audio, guardado, multijugador, campaña, mapas planetarios ni el volumen de unidades de los juegos de referencia.

El siguiente paso natural es incorporar navegación y niebla de guerra, y sustituir los modelos provisionales por assets artísticos.
