# Ejercicio 1 - Tarea 3

## Descripción

En esta tarea se amplía la simulación de la línea de producción para mostrar por consola un log detallado del avance de cada componente.

El objetivo es visualizar los cambios de estado del componente y el tiempo transcurrido en cada fase del proceso.

El formato solicitado en el enunciado es similar a:

Componente <Id>. Entrada N. Estado: <estado>. Duración: <S> segundos.

---

## Funcionamiento del programa

El programa simula una línea de producción con estas características:

- Se generan **4 componentes**
- Entra un componente nuevo cada **2 segundos**
- Hay **4 estaciones de mecanizado**
- Cada estación solo puede procesar **un componente a la vez**
- Cada componente tiene:
  - **ID único aleatorio**
  - **Tiempo de entrada**
  - **Tiempo de mecanizado aleatorio entre 5 y 15 segundos**
  - **Prioridad**
  - **Orden de llegada**
  - **Estado del proceso**

Los estados utilizados en esta tarea son:

- `EnEspera`
- `EnMecanizado`
- `Completado`

---

## Visualización del avance

Cada componente muestra por consola su evolución durante la simulación.

Ejemplo de salida:

Componente 45. Entrada 1. Estado: EnEspera. Duración: 0 segundos.  
Componente 45. Entrada 1. Estado: EnMecanizado. Duración: 10 segundos.  
Componente 45. Entrada 1. Estado: Completado. Duración: 0 segundos.

De esta forma se puede seguir el recorrido completo de cada componente dentro de la línea de producción.

---

## Implementación

Para representar mejor los estados del componente, se ha utilizado un `enum` llamado `EstadoComponente`, ya que resulta más claro y legible que usar números enteros.

También se mantiene la concurrencia mediante hilos (`Thread`), usando un hilo por componente.

Para evitar errores entre hilos se han utilizado varios `lock`:

- `lockEstaciones` para controlar el acceso a las estaciones de mecanizado
- `lockIds` para garantizar IDs únicos
- `lockConsola` para evitar que los mensajes de consola se mezclen
- protección del objeto `Random`

---

## Ejecución

Para ejecutar el programa:

```bash
dotnet run