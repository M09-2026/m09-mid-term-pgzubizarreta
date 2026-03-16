# Ejercicio 2 - Tarea 1

## Descripción

En esta tarea se amplía la simulación de la fábrica añadiendo una segunda fase del proceso: el control de calidad (QC).

Después del mecanizado, algunos componentes deben pasar por una inspección de calidad antes de finalizar su producción.

## Funcionamiento

El sistema funciona en dos fases:

### Mecanizado
- 4 estaciones disponibles
- tiempo aleatorio entre 5 y 15 segundos

### Control de calidad
- solo algunos componentes requieren inspección
- hay 2 máquinas de QC
- cada inspección dura 15 segundos

## Estados del componente

Los componentes pueden encontrarse en los siguientes estados:

- EsperaMecanizado
- EnMecanizado
- EsperaInspeccion
- EnInspeccion
- Completado

## Concurrencia

Para controlar el número de estaciones y máquinas disponibles se utiliza `SemaphoreSlim`.

Esto permite limitar cuántos componentes pueden utilizar un recurso al mismo tiempo.

## Ejecución

dotnet run