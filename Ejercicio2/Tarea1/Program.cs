using System;
using System.Threading;

namespace Ejercicio2.Tarea1
{
    // Estados posibles del componente según el enunciado
    public enum EstadoComponente
    {
        EsperaMecanizado,
        EnMecanizado,
        EsperaInspeccion,
        Completado
    }

    // Clase que representa un componente de la línea de producción
    public class Componente
    {
        // Identificador único del componente
        public int Id { get; set; }

        // Tiempo de mecanizado en segundos
        public int TiempoMecanizado { get; set; }

        // Indica si el componente necesita pasar control de calidad
        public bool RequiereInspeccion { get; set; }

        // Estado actual del componente
        public EstadoComponente Estado { get; set; }

        // Orden de llegada del componente a la fábrica
        public int OrdenLlegada { get; set; }

        // Constructor
        public Componente(int id, int tiempoMecanizado, bool requiereInspeccion, int ordenLlegada)
        {
            Id = id;
            TiempoMecanizado = tiempoMecanizado;
            RequiereInspeccion = requiereInspeccion;
            OrdenLlegada = ordenLlegada;
            Estado = EstadoComponente.EsperaMecanizado;
        }
    }

    class Program
    {
        // 4 estaciones de mecanizado
        static SemaphoreSlim estacionesMecanizado = new SemaphoreSlim(4, 4);

        // 2 máquinas de control de calidad
        static SemaphoreSlim maquinasQC = new SemaphoreSlim(2, 2);

        // Generador aleatorio
        static Random random = new Random();

        // Bloqueos para proteger recursos compartidos
        static object lockRandom = new object();
        static object lockConsola = new object();

        static void Main(string[] args)
        {
            Thread[] hilos = new Thread[4];

            // Llegan 4 componentes, uno cada 2 segundos
            for (int i = 0; i < 4; i++)
            {
                int ordenLlegada = i + 1;
                Componente componente = CrearComponente(ordenLlegada);

                // Mostrar estado inicial
                MostrarLog(componente);

                // Crear hilo para procesar el componente
                hilos[i] = new Thread(() => ProcesarComponente(componente));
                hilos[i].Start();

                // Esperar 2 segundos antes de la llegada del siguiente componente
                Thread.Sleep(2000);
            }

            // Esperar a que todos los hilos terminen
            for (int i = 0; i < 4; i++)
            {
                hilos[i].Join();
            }

            Console.WriteLine("Fin de la simulación.");
        }

        // Crea un componente con valores aleatorios
        static Componente CrearComponente(int ordenLlegada)
        {
            int id;
            int tiempoMecanizado;
            bool requiereInspeccion;

            lock (lockRandom)
            {
                id = random.Next(1, 101);
                tiempoMecanizado = random.Next(5, 16);
                requiereInspeccion = random.Next(0, 2) == 1;
            }

            return new Componente(id, tiempoMecanizado, requiereInspeccion, ordenLlegada);
        }

        // Simula el paso del componente por mecanizado y, si hace falta, por QC
        static void ProcesarComponente(Componente componente)
        {
            // Espera a una estación de mecanizado libre
            estacionesMecanizado.Wait();

            componente.Estado = EstadoComponente.EnMecanizado;
            MostrarLog(componente);

            // Simular el tiempo de mecanizado
            Thread.Sleep(componente.TiempoMecanizado * 1000);

            // Liberar estación de mecanizado
            estacionesMecanizado.Release();

            // Si requiere inspección, pasa a cola de QC
            if (componente.RequiereInspeccion)
            {
                componente.Estado = EstadoComponente.EsperaInspeccion;
                MostrarLog(componente);

                // Espera a una máquina de QC libre
                maquinasQC.Wait();

                // Simular inspección fija de 15 segundos
                Thread.Sleep(15000);

                // Liberar máquina de QC
                maquinasQC.Release();
            }

            // El componente termina su recorrido
            componente.Estado = EstadoComponente.Completado;
            MostrarLog(componente);
        }

        // Muestra por consola el estado actual del componente
        static void MostrarLog(Componente componente)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {componente.Id}. " +
                    $"Entrada {componente.OrdenLlegada}. " +
                    $"Estado: {componente.Estado}. " +
                    $"TiempoMecanizado: {componente.TiempoMecanizado}s. " +
                    $"RequiereInspeccion: {componente.RequiereInspeccion}");
            }
        }
    }
}