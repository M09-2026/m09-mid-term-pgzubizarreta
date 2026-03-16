# Ejercicio 2 - Tarea 5

## Descripción

En esta tarea se completa la simulación de la fábrica añadiendo un informe final con estadísticas del funcionamiento del sistema durante la jornada de producción.

Además de gestionar la entrada a mecanizado por prioridad y el paso por control de calidad, al terminar la simulación se muestran datos resumidos que permiten analizar el comportamiento del sistema.

## Funcionamiento del programa

El programa genera 20 componentes que llegan a la fábrica cada 2 segundos.

Cada componente tiene:

- un ID único
- un tiempo de mecanizado aleatorio entre 5 y 15 segundos
- una prioridad aleatoria entre 1 y 3
- un orden de llegada
- la posibilidad de requerir control de calidad

El sistema consta de dos fases:

### Mecanizado
- hay 4 estaciones de mecanizado
- la entrada se gestiona mediante una cola priorizada
- primero entran los componentes de prioridad 1, luego 2 y luego 3
- si dos componentes tienen la misma prioridad, entra antes el que llegó antes

### Control de calidad
- algunos componentes requieren inspección
- hay 2 máquinas de QC
- cada inspección dura 15 segundos

## Estados del componente

Los estados posibles son:

- `EsperaMecanizado`
- `EnMecanizado`
- `EsperaInspeccion`
- `EnInspeccion`
- `Completado`

## Concurrencia

El programa utiliza:

- un hilo principal que genera componentes
- un hilo planificador que decide qué componente entra a mecanizado
- un hilo independiente para cada componente que entra en producción

Para sincronizar correctamente el sistema se utilizan:

- una lista compartida como buffer de espera
- `lock` para proteger buffer, consola, `Random`, IDs y lista de completados
- `SemaphoreSlim` para controlar las 2 máquinas de control de calidad

## Estadísticas finales

Al finalizar la simulación, el programa muestra un informe con:

- número de componentes producidos por prioridad
- tiempo medio de espera en buffer por prioridad
- porcentaje medio de uso de las máquinas de control de calidad

Para calcular estas estadísticas, cada componente guarda:
- el instante en que entra al buffer
- el instante en que empieza mecanizado
- su tiempo total de espera

Además, se mide el tiempo total dedicado a inspección en QC para estimar el porcentaje de uso de las dos máquinas.

## Ejecución

Para ejecutar el programa:

```bash
dotnet run