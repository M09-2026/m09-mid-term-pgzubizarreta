# Ejercicio 2 - Tarea 4

## Descripción

En esta tarea se amplía la simulación de la fábrica introduciendo una cola de espera con prioridad para acceder a las estaciones de mecanizado.

Cuando varios componentes están esperando para entrar en producción, no se atienden simplemente por orden de llegada, sino teniendo en cuenta su nivel de prioridad.

## Funcionamiento del programa

El programa genera 20 componentes que llegan a la fábrica cada 2 segundos.

Cada componente tiene:

- un ID único
- un tiempo de mecanizado aleatorio entre 5 y 15 segundos
- una prioridad aleatoria entre 1 y 3
- un orden de llegada
- la posibilidad de requerir control de calidad

El sistema sigue teniendo dos fases:

### Mecanizado
- hay 4 estaciones de mecanizado
- solo 4 componentes pueden mecanizarse a la vez
- el acceso se gestiona mediante una cola priorizada

### Control de calidad
- algunos componentes requieren inspección
- hay 2 máquinas de QC
- cada inspección dura 15 segundos

## Regla de prioridad

La entrada a mecanizado se organiza del siguiente modo:

1. primero los componentes de prioridad 1
2. después los de prioridad 2
3. por último los de prioridad 3

Si dos componentes tienen la misma prioridad, entra antes el que tenga menor orden de llegada.

## Estados del componente

Los estados posibles del componente son:

- `EsperaMecanizado`
- `EnMecanizado`
- `EsperaInspeccion`
- `EnInspeccion`
- `Completado`

## Concurrencia

El programa utiliza concurrencia de varias formas:

- un hilo principal genera los componentes
- un hilo planificador decide qué componente entra en mecanizado
- cada componente que entra en producción se procesa en su propio hilo

Para gestionar los recursos y evitar conflictos se usan:

- una lista compartida como buffer de espera
- `lock` para proteger el acceso al buffer
- `SemaphoreSlim` para las 2 máquinas de control de calidad
- `lock` para proteger consola, `Random` e IDs únicos

## Gestión de la cola priorizada

Los componentes que esperan mecanizado se almacenan en una lista compartida.

El planificador selecciona siempre el siguiente componente usando este criterio:

- menor prioridad
- si hay empate, menor orden de llegada

De esta forma se garantiza que la entrada a mecanizado siga la política de prioridad establecida.

## Ejecución

Para ejecutar el programa:

```bash
dotnet run