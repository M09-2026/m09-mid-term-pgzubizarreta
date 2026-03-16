# Ejercicio 1 - Tarea 2

## Descripción

En esta tarea se simula el funcionamiento de una línea de producción en una fábrica utilizando programación concurrente en C#.

Cada componente se representa mediante una clase `Componente`, que contiene información propia como su identificador, tiempo de entrada en la línea de producción, tiempo de mecanizado y estado.

El sistema utiliza múltiples hilos para procesar los componentes en paralelo.

---

## Funcionamiento del programa

El programa simula el comportamiento de una fábrica con las siguientes características:

- Se generan **4 componentes**.
- Cada componente entra en la línea de producción **cada 2 segundos**.
- Cada componente tiene:
  - **ID único aleatorio entre 1 y 100**
  - **Prioridad aleatoria entre 1 y 3**
  - **Tiempo de mecanizado aleatorio entre 5 y 15 segundos**
  - **Orden de llegada**
  - **Tiempo de entrada**
- Existen **4 estaciones de mecanizado**.
- Cada estación solo puede procesar **un componente a la vez**.
- Cada componente intenta entrar en una estación aleatoria.
- Si la estación está ocupada, el componente espera hasta encontrar una libre.

El mecanizado se simula utilizando `Thread.Sleep()` durante el tiempo correspondiente.

---

## Clase Componente

Cada componente se representa mediante la clase:

```csharp
public class Componente
{
    public int Id { get; set; }
    public int TiempoEntrada { get; set; }
    public int TiempoMecanizado { get; set; }
    public int Estado { get; set; }
    public int Prioridad { get; set; }
    public int OrdenLlegada { get; set; }
}