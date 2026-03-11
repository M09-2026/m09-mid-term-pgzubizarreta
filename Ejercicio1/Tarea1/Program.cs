using System;
using System.Collections.Generic;
using System.Threading;

namespace Tarea1
{
    class Program
    {
        // Array que indica si cada estación está ocupada o libre
        // false = libre, true = ocupada
        static bool[] estaciones = new bool[4];

        // Objeto que se usa para bloquear el acceso a las estaciones
        // cuando varios hilos intentan acceder al mismo tiempo
        static object bloqueo = new object();

        static void Main(string[] args)
        {
            // Generador de números aleatorios para elegir estación
            Random rnd = new Random();

            // Lista donde guardamos los hilos de los componentes
            // para poder esperar a que todos terminen
            List<Thread> hilos = new List<Thread>();

            // Simulamos la llegada de 4 componentes
            for (int i = 1; i <= 4; i++)
            {
                Console.WriteLine($"Componente {i} detectado");

                int estacionAsignada = -1;

                // Mientras no se haya asignado una estación libre
                // seguimos intentando encontrar una
                while (estacionAsignada == -1)
                {
                    // Elegimos una estación aleatoria (0 a 3)
                    int intento = rnd.Next(0, 4);

                    // lock evita que dos hilos modifiquen el array
                    // de estaciones al mismo tiempo
                    lock (bloqueo)
                    {
                        // Si la estación está libre la ocupamos
                        if (!estaciones[intento])
                        {
                            estaciones[intento] = true;
                            estacionAsignada = intento;
                        }
                    }

                    // Si no encontramos estación libre
                    // esperamos un poco antes de volver a intentar
                    if (estacionAsignada == -1)
                    {
                        Thread.Sleep(200);
                    }
                }

                // Guardamos los valores en variables locales
                // para pasarlos al hilo
                int componente = i;
                int estacion = estacionAsignada + 1;

                // Creamos un hilo que simula el mecanizado del componente
                Thread t = new Thread(() => Procesar(componente, estacion));

                // Guardamos el hilo en la lista
                hilos.Add(t);

                // Iniciamos el hilo
                t.Start();

                // Los componentes llegan cada 2 segundos
                Thread.Sleep(2000);
            }

            // Esperamos a que todos los hilos terminen
            foreach (Thread t in hilos)
            {
                t.Join();
            }

            Console.WriteLine("Fin de la simulación");
        }

        // Método que simula el mecanizado de un componente
        static void Procesar(int componente, int estacion)
        {
            Console.WriteLine($"Componente {componente} entra en estación {estacion}");

            // Simula el tiempo de mecanizado (10 segundos)
            Thread.Sleep(10000);

            Console.WriteLine($"Componente {componente} abandona la estación {estacion}");

            // Cuando termina el mecanizado liberamos la estación
            lock (bloqueo)
            {
                estaciones[estacion - 1] = false;
            }
        }
    }
}