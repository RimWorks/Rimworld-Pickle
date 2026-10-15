# Pickle: framework de pruebas para mods de RimWorld

Escribe pruebas automáticas para tu mod de RimWorld y ejecútalas dentro del juego. Pickle lee escenarios de Gherkin y los reproduce contra una sesión en marcha. Tus pruebas pulsan la interfaz real, esperan al estado real del juego y comprueban la simulación en vivo. Sin mocks.

```
Scenario: a drafted colonist waits for combat
  Given the save "test-colony" is loaded
  Given a colonist "Soldier" exists
  When I draft "Soldier"
  And I wait 30 ticks
  Then "Soldier" is drafted
  And "Soldier" has job "Wait_Combat"
```

## Para quién es esto

Para autores de mods. Pickle no añade nada a una partida normal, así que no hay motivo para suscribirse salvo que estés creando o probando un mod.

## ¿En qué se diferencia de RimTest Redux?

RimTest Redux y los otros frameworks de pruebas de RimWorld ejecutan pruebas unitarias. Escribes métodos de prueba en C# y compruebas tu propio código, que es la herramienta correcta cuando lo que revisas es una función.

Pickle trabaja un nivel por encima. Un escenario carga una partida, recluta a un colono real, pulsa un botón real, espera ticks reales y comprueba la colonia resultante. Eso detecta fallos que una prueba unitaria no puede ver. Un def que no carga, un parche que entra en conflicto, un job driver que se atasca en el tick 400.

Los dos encajan. Haz pruebas unitarias de tus cálculos y pasa Pickle sobre tu colonia.

## Cómo probar un mod de RimWorld

Pickle te da una ventana de ejecución de pruebas en modo desarrollador. Lista todos los mods que traen una suite, ejecuta los escenarios que elijas y muestra cada paso con su tiempo.

Cuando un paso falla, Pickle pausa el juego en el estado roto para que puedas mirar la colonia que lo provocó. También captura una imagen, el final del log y el estado de cada colono.

## ¿Hace falta escribir C#?

No para empezar. Los ficheros feature por sí solos no necesitan compilación. Pickle trae pasos para partidas guardadas, configuración del mundo, estadísticas de colonos, cirugía, pensamientos, trabajos, órdenes, zonas, armas, ropa, alertas, la cámara y la interfaz. Escribe tus propias definiciones de pasos en C# cuando necesites algo que no cubra.

## ¿Se puede ejecutar en CI?

Sí. Las ejecuciones van sin supervisión y escriben un fichero JUnit, mensajes Cucumber y una página de informe que puedes abrir desde el disco. Un escenario fallido se convierte en una anotación en el pull request en vez de una línea en un log que nadie abre.

También puedes seguir una ejecución desde un navegador en cualquier máquina. Es la única forma de ver una ejecución sin ventana abierta.

## Requisitos

RimWorld 1.5, 1.6 o 1.7 y una librería de parcheo. Sirven tanto Harmony como Concord. Pickle prefiere Concord si tienes las dos, y vuelve a Harmony cuando Concord acepta parches que luego nunca se ejecutan.

## Cómo empezar

Parte del repositorio plantilla, o añade una carpeta Pickle a un mod que ya tengas. La documentación cubre pasos, etiquetas, fixtures y esperas.

Código y documentación: https://github.com/RimWorks/Rimworld-Pickle

## Más herramientas de modding de RimWorks

- [Quickstarts](https://steamcommunity.com/sharedfiles/filedetails/?id=3793646067): entra directo en una colonia ya configurada desde el menú de quicktest.
- [RimLogging](https://steamcommunity.com/sharedfiles/filedetails/?id=3733484696): visor de logs estructurado y envío de informes de error en un clic.
