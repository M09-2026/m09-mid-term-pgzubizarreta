# Ejercicio 2 - Tarea 2

## Descripción

En esta tarea se debe implementar una sincronización para que la inspección de calidad se realice en el orden estricto de entrada a la fábrica.

Esto significa que los componentes deben pasar por control de calidad en el orden 1, 2, 3 y 4, independientemente de cuál haya terminado antes el mecanizado.

Por ejemplo, si el componente 1 tarda más en mecanizarse que el componente 2, el componente 2 no podrá inspeccionarse antes. Tendrá que esperar hasta que el componente 1 haya completado su inspección.

---

## Objetivo del programa

El objetivo de esta tarea es garantizar un orden secuencial estricto en la fase de control de calidad.

Aunque varios componentes puedan mecanizarse en paralelo, el acceso a la inspección se fuerza para que respete exactamente el orden de llegada a la fábrica.

Con esta solución se busca:

- mantener la concurrencia en la fase de mecanizado,
- controlar el acceso a QC por orden estricto,
- demostrar el uso de sincronización entre hilos,
- visualizar claramente el avance de cada componente.

---

## Requisitos del enunciado

En esta tarea se pide implementar una sincronización para que la inspección de calidad se realice en el orden estricto de entrada a la fábrica, es decir, 1, 2, 3 y 4.

Además, se indica que, si el componente 1 tarda más en mecanizarse, los componentes 2, 3 y 4 deben esperar en la cola de QC hasta que el 1 haya pasado su inspección. :contentReference[oaicite:4]{index=4}

---

## Estructura del programa

El programa se organiza en varias partes.

### Enumeración de estados

Se utiliza una enumeración para representar los estados posibles del componente durante la simulación:

- EsperaMecanizado
- EnMecanizado
- EsperaInspeccion
- EnInspeccion
- Completado

Esto facilita la lectura del código y mejora la claridad del seguimiento del proceso.

---

### Clase Componente

La clase `Componente` representa cada pieza que entra en la planta.

Cada componente contiene:

- **Id**: identificador único aleatorio.
- **TiempoMecanizado**: tiempo de mecanizado aleatorio entre 5 y 15 segundos.
- **RequiereInspeccion**: indica si debe pasar por control de calidad.
- **Estado**: estado actual del componente.
- **OrdenLlegada**: número de orden de entrada en la fábrica.

En esta tarea se fuerza que todos los componentes pasen por QC para poder comprobar claramente el orden secuencial.

---

### Clase principal del programa

La clase principal gestiona toda la simulación.

En ella se definen:

- las 4 estaciones de mecanizado,
- las 2 máquinas de control de calidad,
- una variable compartida que indica qué componente tiene permiso para entrar en QC,
- varios bloqueos para proteger recursos compartidos,
- y los hilos que procesan los componentes.

---

## Funcionamiento de la simulación

El programa sigue estos pasos:

1. Se generan 4 componentes.
2. Cada componente llega con una separación de 2 segundos.
3. Cada componente espera una estación de mecanizado libre.
4. Cuando entra en mecanizado, permanece el tiempo indicado en su tiempo de proceso.
5. Al terminar el mecanizado, pasa al estado `EsperaInspeccion`.
6. En ese momento, el componente no puede entrar en QC hasta que llegue exactamente su turno según el orden de entrada.
7. Cuando le corresponde su turno:
   - entra en una máquina de QC,
   - pasa al estado `EnInspeccion`,
   - permanece 15 segundos en inspección.
8. Solo cuando termina por completo la inspección, se permite avanzar al siguiente componente.
9. Finalmente, el componente pasa al estado `Completado`.

---

## Sincronización utilizada

La solución combina dos mecanismos de sincronización:

### Semáforo para mecanizado

Se utiliza un `SemaphoreSlim` con capacidad 4 para representar las 4 estaciones de mecanizado.

Esto permite que hasta 4 componentes puedan mecanizarse al mismo tiempo.

### Semáforo para QC

También se utiliza un `SemaphoreSlim` con capacidad 2 para representar las 2 máquinas de control de calidad.

### Control de turno secuencial

Además, se utiliza una variable compartida llamada `siguienteOrdenQc` que indica qué componente puede entrar en QC.

Cada hilo espera hasta que su `OrdenLlegada` coincida con ese valor. Cuando el componente termina completamente su inspección, incrementa el turno para permitir que pase el siguiente.

Este mecanismo garantiza que el orden de inspección sea estrictamente 1, 2, 3 y 4.

---

## Explicación de la solución planteada

La solución escogida consiste en separar dos ideas:

- por un lado, el acceso físico a los recursos de la planta,
- y por otro, el orden lógico en que se permite avanzar por QC.

Las estaciones de mecanizado siguen funcionando en paralelo, porque eso sí forma parte del comportamiento normal de la planta.

Sin embargo, antes de que un componente pueda entrar en control de calidad, debe comprobar si realmente le corresponde el turno. Si todavía no le toca, permanece esperando aunque ya haya terminado el mecanizado.

Esta solución se ha escogido porque es sencilla de entender, fácil de implementar y permite demostrar claramente el concepto de sincronización secuencial entre hilos.

---


## Visualización del avance

El programa muestra por consola:

- el identificador del componente,
- el número de entrada,
- el estado actual,
- el tiempo de mecanizado,
- y si necesita pasar por QC.

Esto permite observar claramente el recorrido de cada componente y comprobar que la inspección se realiza en orden secuencial.

---

## Captura de ejecución

Aquí debe añadirse una captura de pantalla mostrando la ejecución del programa en consola.

*(2.2.2.png)*




