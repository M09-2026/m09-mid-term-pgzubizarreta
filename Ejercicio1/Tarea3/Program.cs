using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ejercicio1.Tarea3
{
    // Estados posibles del componente
    public enum EstadoComponente
    {
        EnEspera,
        EnMecanizado,
        Completado
    }

    // Clase que representa un componente
    public class Componente
    {
        public int Id { get; set; }                       // ID único
        public int TiempoEntrada { get; set; }            // Segundo de entrada
        public int TiempoMecanizado { get; set; }         // Tiempo de mecanizado
        public EstadoComponente Estado { get; set; }      // Estado actual
        public int Prioridad { get; set; }                // Prioridad del componente
        public int OrdenLlegada { get; set; }             // Orden de llegada

        // Constructor
        public Componente(int id, int tiempoEntrada, int tiempoMecanizado)
        {
            Id = id;
            TiempoEntrada = tiempoEntrada;
            TiempoMecanizado = tiempoMecanizado;
        }
    }

    class Program
    {
        // false = libre, true = ocupada
        static bool[] estacionesOcupadas = new bool[4];

        // Locks para sincronizar recursos compartidos
        static object lockEstaciones = new object();
        static object lockIds = new object();
        static object lockConsola = new object();

        // Random compartido
        static Random random = new Random();

        // Para evitar IDs repetidos
        static HashSet<int> idsUsados = new HashSet<int>();

        static void Main(string[] args)
        {
            Thread[] hilos = new Thread[4];

            // Creamos y lanzamos 4 componentes
            for (int i = 0; i < 4; i++)
            {
                int orden = i + 1;
                Componente componente = CrearComponente(orden);

                // Al llegar, el componente está en espera
                componente.Estado = EstadoComponente.EnEspera;
                LogEstado(componente, componente.Estado, 0);

                // Cada componente se procesa en su propio hilo
                hilos[i] = new Thread(() => ProcesarComponente(componente));
                hilos[i].Start();

                // Entra un componente cada 2 segundos
                Thread.Sleep(2000);
            }

            // Esperamos a que terminen todos los hilos
            for (int i = 0; i < 4; i++)
            {
                hilos[i].Join();
            }

            Console.WriteLine("Fin de la simulación.");
        }

        // Crea un componente con datos aleatorios
        static Componente CrearComponente(int orden)
        {
            int id;
            int tiempoMecanizado;
            int prioridad;

            // Tiempo de mecanizado y prioridad aleatorios
            lock (random)
            {
                tiempoMecanizado = random.Next(5, 16); // 5 a 15 segundos
                prioridad = random.Next(1, 4);         // 1, 2 o 3
            }

            // Generación de ID único
            lock (lockIds)
            {
                do
                {
                    lock (random)
                    {
                        id = random.Next(1, 101);
                    }
                }
                while (idsUsados.Contains(id));

                idsUsados.Add(id);
            }

            Componente c = new Componente(id, (orden - 1) * 2, tiempoMecanizado);
            c.Prioridad = prioridad;
            c.OrdenLlegada = orden;

            return c;
        }

        // Procesa el componente dentro de una estación
        static void ProcesarComponente(Componente c)
        {
            int estacion = -1;
            Stopwatch tiempoEspera = Stopwatch.StartNew();

            // Mientras no encuentre estación libre, sigue intentando
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

                // Si no ha encontrado estación libre, espera un poco
                if (estacion == -1)
                {
                    Thread.Sleep(200);
                }
            }

            // Tiempo total que ha esperado antes de mecanizarse
            tiempoEspera.Stop();

            // Cambia a estado EnMecanizado
            c.Estado = EstadoComponente.EnMecanizado;
            LogEstado(c, EstadoComponente.EnEspera, (int)Math.Round(tiempoEspera.Elapsed.TotalSeconds));
            LogEstado(c, c.Estado, c.TiempoMecanizado);

            // Simula el mecanizado
            Thread.Sleep(c.TiempoMecanizado * 1000);

            // Cambia a estado Completado
            c.Estado = EstadoComponente.Completado;
            LogEstado(c, c.Estado, 0);

            // Libera la estación
            lock (lockEstaciones)
            {
                estacionesOcupadas[estacion] = false;
            }
        }

        // Muestra el log de estado del componente
        static void LogEstado(Componente c, EstadoComponente estado, int duracion)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {c.Id}. Entrada {c.OrdenLlegada}. Estado: {estado}. Duración: {duracion} segundos.");
            }
        }
    }
}
