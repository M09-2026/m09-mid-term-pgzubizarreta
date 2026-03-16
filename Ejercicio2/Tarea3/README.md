# Ejercicio 2 - Tarea 3

## Descripción

En esta tarea se amplía la simulación de la fábrica aumentando el número de componentes a 20.

El objetivo es comprobar el funcionamiento del sistema cuando llegan más componentes de los que pueden procesarse simultáneamente en las estaciones de mecanizado.

Cuando las 4 estaciones están ocupadas, los nuevos componentes deben permanecer en estado de espera hasta que una estación quede libre.

---

## Funcionamiento del programa

El programa simula una línea de producción con dos fases:

### Mecanizado
- hay 4 estaciones de mecanizado
- cada estación solo puede procesar un componente a la vez
- el tiempo de mecanizado es aleatorio entre 5 y 15 segundos

### Control de calidad
- algunos componentes requieren inspección
- hay 2 máquinas de control de calidad
- cada inspección dura 15 segundos

En esta tarea se generan **20 componentes**, que entran en la fábrica cada 2 segundos.

---

## Estados del componente

Los componentes pueden encontrarse en los siguientes estados:

- `EsperaMecanizado`
- `EnMecanizado`
- `EsperaInspeccion`
- `EnInspeccion`
- `Completado`

Estos estados permiten seguir el recorrido completo de cada componente dentro del sistema.

---

## Concurrencia

El programa utiliza **hilos (`Thread`)** para procesar los componentes de forma concurrente.

Además, se emplea `SemaphoreSlim` para controlar el acceso a los recursos limitados:

- un semáforo de 4 posiciones para las estaciones de mecanizado
- un semáforo de 2 posiciones para las máquinas de control de calidad

También se utilizan `lock` para:

- proteger el uso de `Random`
- evitar IDs repetidos
- evitar que los mensajes de consola se mezclen

---

## Gestión de la espera en mecanizado

Como en esta tarea se generan 20 componentes pero solo existen 4 estaciones de mecanizado, algunos componentes no pueden entrar inmediatamente en producción.

Cuando esto ocurre, el componente permanece en estado `EsperaMecanizado` hasta que una estación queda libre.

Esto permite simular un comportamiento más realista del sistema, ya que no todos los componentes pueden procesarse al mismo tiempo.

---

## Ejecución

Para ejecutar el programa:

```bash
dotnet run