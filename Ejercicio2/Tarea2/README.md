# Ejercicio 2 - Tarea 2

## Descripción

En esta tarea se amplía la simulación de la fábrica añadiendo una restricción en la fase de control de calidad.

Los componentes que requieren inspección deben pasar por las máquinas de QC respetando el orden de llegada a la fábrica.

## Funcionamiento

El programa simula dos fases del proceso productivo:

### Mecanizado
- 4 estaciones disponibles
- tiempo aleatorio entre 5 y 15 segundos
- un componente por estación

### Control de calidad
- 2 máquinas de inspección
- cada inspección dura 15 segundos
- los componentes pasan en orden de llegada

## Estados del componente

Los estados utilizados son:

- EsperaMecanizado
- EnMecanizado
- EsperaInspeccion
- EnInspeccion
- Completado

## Concurrencia

Para controlar los recursos se utiliza `SemaphoreSlim`:

- un semáforo para las 4 estaciones de mecanizado
- un semáforo para las 2 máquinas de QC

Además, se usa una variable global llamada `siguienteOrdenQc` junto con un `lock` para garantizar que los componentes entren en inspección siguiendo estrictamente su orden de llegada.

## Ejecución

```bash
dotnet run