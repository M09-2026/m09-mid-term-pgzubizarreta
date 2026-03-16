using System;
using System.Collections.Generic;
using System.Threading;

namespace Tarea2
{
    // Clase que representa un componente que llega al sistema
    public class Componente
    {
        // Identificador del componente
        public int Id { get; set; }

        // Tiempo en el que entra al sistema (segundos)
        public int TiempoEntrada { get; set; }

        // Tiempo que tarda en mecanizarse
        public int TiempoMecanizado { get; set; }

        // Estado del componente
        // 0 = En espera
        // 1 = En mecanizado
        // 2 = Completado
        public int Estado { get; set; }

        // Constructor para crear el componente con sus valores iniciales
        public Componente(int id, int tiempoEntrada, int tiempoMecanizado)
        {
            Id = id;
            TiempoEntrada = tiempoEntrada;
            TiempoMecanizado = tiempoMecanizado;
            Estado = 0; // Al crearse siempre empieza "en espera"
        }
    }

    class Program
    {
        // Array que representa las estaciones de mecanizado
        // false = estación libre
        // true = estación ocupada
        static bool[] estaciones = new bool[4];

        // Objeto utilizado para bloquear el acceso concurrente
        // evita que varios hilos accedan a estaciones a la vez
        static object bloqueo = new object();

        static void Main(string[] args)
        {
            // Generador de números aleatorios
            Random rnd = new Random();

            // Lista donde guardaremos los hilos creados
            List<Thread> hilos = new List<Thread>();

            // Simulación de llegada de 4 componentes
            for (int i = 1; i <= 4; i++)
            {
                // Creamos un nuevo componente con valores aleatorios
                Componente componente = new Componente(
                    rnd.Next(1, 101),      // ID aleatorio entre 1 y 100
                    (i - 1) * 2,           // tiempo de entrada: 0,2,4,6
                    rnd.Next(5, 16)        // tiempo de mecanizado entre 5 y 15 segundos
                );

                // Mostramos información inicial del componente
                Console.WriteLine($"Componente detectado -> Orden llegada: {i}, ID: {componente.Id}, TiempoEntrada: {componente.TiempoEntrada}s, TiempoMecanizado: {componente.TiempoMecanizado}s, Estado: En espera");

                int estacionAsignada = -1;

                // Mientras no se encuentre estación libre
                while (estacionAsignada == -1)
                {
                    // Intentamos elegir una estación aleatoria
                    int intento = rnd.Next(0, 4);

                    // Bloqueamos acceso para evitar conflictos entre hilos
                    lock (bloqueo)
                    {
                        // Si la estación está libre
                        if (!estaciones[intento])
                        {
                            // La marcamos como ocupada
                            estaciones[intento] = true;

                            // Guardamos la estación asignada
                            estacionAsignada = intento;
                        }
                    }

                    // Si no encontramos estación libre esperamos un poco
                    if (estacionAsignada == -1)
                    {
                        Thread.Sleep(200);
                    }
                }

                int ordenLlegada = i;

                // Sumamos 1 porque las estaciones van de 1 a 4 para el usuario
                int estacion = estacionAsignada + 1;

                // Creamos un hilo que ejecutará el mecanizado
                Thread t = new Thread(() => Procesar(componente, ordenLlegada, estacion));

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

        // Método que simula el proceso de mecanizado
        static void Procesar(Componente componente, int ordenLlegada, int estacion)
        {
            // Cambiamos el estado a "en mecanizado"
            componente.Estado = 1;

            Console.WriteLine($"Componente ID {componente.Id} (entrada {ordenLlegada}) entra en estación {estacion}. Estado: En mecanizado");

            // Simulación del tiempo de mecanizado
            Thread.Sleep(componente.TiempoMecanizado * 1000);

            // Cambiamos el estado a completado
            componente.Estado = 2;

            Console.WriteLine($"Componente ID {componente.Id} (entrada {ordenLlegada}) abandona la estación {estacion}. Estado: Completado");

            // Liberamos la estación para que otro componente pueda usarla
            lock (bloqueo)
            {
                estaciones[estacion - 1] = false;
            }
        }
    }
}