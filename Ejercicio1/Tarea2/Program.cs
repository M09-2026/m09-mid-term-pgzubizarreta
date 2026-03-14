using System;
using System.Collections.Generic;
using System.Threading;

namespace Ejercicio1.Tarea2
{
    // Clase que representa un componente de la fábrica
    public class Componente
    {
        public int Id { get; set; }               // Identificador único
        public int TiempoEntrada { get; set; }    // Momento en que entra en la línea
        public int TiempoMecanizado { get; set; } // Tiempo que tarda en procesarse
        public int Estado { get; set; }           // Estado del componente

        public int Prioridad { get; set; }        // Prioridad del componente
        public int OrdenLlegada { get; set; }     // Orden en que llega a la fábrica

        // Constructor del componente
        public Componente(int id, int tiempoEntrada, int tiempoMecanizado)
        {
            Id = id;
            TiempoEntrada = tiempoEntrada;
            TiempoMecanizado = tiempoMecanizado;
        }
    }

    class Program
    {
        // Array que indica si cada estación está ocupada
        static bool[] estacionesOcupadas = new bool[4];

        // Lock para controlar el acceso a las estaciones
        static object lockEstaciones = new object();

        // Generador de números aleatorios
        static Random random = new Random();

        // Colección para evitar IDs repetidos
        static HashSet<int> idsUsados = new HashSet<int>();
        static object lockIds = new object();

        static void Main(string[] args)
        {
            // Array para guardar los hilos de los componentes
            Thread[] hilos = new Thread[4];

            // Se crean 4 componentes
            for (int i = 0; i < 4; i++)
            {
                int orden = i + 1;

                // Creamos el componente
                Componente componente = CrearComponente(orden);

                // Mostramos sus datos al detectarlo
                MostrarLlegada(componente);

                // Creamos un hilo para procesar el componente
                hilos[i] = new Thread(() => ProcesarComponente(componente));
                hilos[i].Start();

                // Cada componente entra cada 2 segundos
                Thread.Sleep(2000);
            }

            // Esperamos a que todos los hilos terminen
            for (int i = 0; i < 4; i++)
            {
                hilos[i].Join();
            }

            Console.WriteLine("Fin de la simulación.");
        }

        // Método que crea un componente con valores aleatorios
        static Componente CrearComponente(int orden)
        {
            int id;
            int tiempoMecanizado;
            int prioridad;

            // Generamos tiempo de mecanizado y prioridad
            lock (random)
            {
                tiempoMecanizado = random.Next(5, 16); // entre 5 y 15 segundos
                prioridad = random.Next(1, 4);         // prioridad 1-3
            }

            // Generamos un ID único entre 1 y 100
            lock (lockIds)
            {
                do
                {
                    lock (random)
                    {
                        id = random.Next(1, 101);
                    }
                }
                while (idsUsados.Contains(id)); // si existe se repite

                idsUsados.Add(id);
            }

            // Creamos el objeto componente
            Componente c = new Componente(id, (orden - 1) * 2, tiempoMecanizado);

            c.Estado = 0;          // 0 = En espera
            c.Prioridad = prioridad;
            c.OrdenLlegada = orden;

            return c;
        }

        // Muestra la información del componente al llegar
        static void MostrarLlegada(Componente c)
        {
            Console.WriteLine(
                $"Componente detectado -> ID={c.Id}, Prioridad={c.Prioridad}, OrdenLlegada={c.OrdenLlegada}, TiempoEntrada={c.TiempoEntrada}s, TiempoMecanizado={c.TiempoMecanizado}s");
        }

        // Procesa el componente dentro de la fábrica
        static void ProcesarComponente(Componente c)
        {
            int estacion = -1;

            // Mientras no encuentre estación libre sigue intentando
            while (estacion == -1)
            {
                int intento;

                // Selecciona estación aleatoria
                lock (random)
                {
                    intento = random.Next(0, 4);
                }

                // Comprueba si la estación está libre
                lock (lockEstaciones)
                {
                    if (!estacionesOcupadas[intento])
                    {
                        estacionesOcupadas[intento] = true;
                        estacion = intento;
                    }
                }

                // Si no hay estación libre espera un poco
                if (estacion == -1)
                {
                    Thread.Sleep(200);
                }
            }

            // Cambia el estado a mecanizado
            c.Estado = 1;

            Console.WriteLine(
                $"Componente ID={c.Id} (Prioridad={c.Prioridad}, Llegada={c.OrdenLlegada}) entra en estación {estacion + 1} durante {c.TiempoMecanizado}s");

            // Simula el mecanizado
            Thread.Sleep(c.TiempoMecanizado * 1000);

            // Cambia estado a completado
            c.Estado = 2;

            Console.WriteLine(
                $"Componente ID={c.Id} (Prioridad={c.Prioridad}, Llegada={c.OrdenLlegada}) abandona la estación {estacion + 1}");

            // Libera la estación
            lock (lockEstaciones)
            {
                estacionesOcupadas[estacion] = false;
            }
        }
    }
}