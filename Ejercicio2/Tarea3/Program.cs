using System;
using System.Collections.Generic;
using System.Threading;

namespace Ejercicio2.Tarea3
{
    // Estados posibles del componente
    public enum EstadoComponente
    {
        EsperaMecanizado,
        EnMecanizado,
        EsperaInspeccion,
        EnInspeccion,
        Completado
    }

    // Clase que representa un componente
    public class Componente
    {
        public int Id { get; set; }
        public int TiempoMecanizado { get; set; }
        public bool RequiereInspeccion { get; set; }
        public EstadoComponente Estado { get; set; }
        public int OrdenLlegada { get; set; }

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
        static SemaphoreSlim estaciones = new SemaphoreSlim(4, 4);

        // 2 máquinas de control de calidad
        static SemaphoreSlim qc = new SemaphoreSlim(2, 2);

        static object lockRandom = new object();
        static object lockConsola = new object();
        static object lockIds = new object();

        static Random random = new Random();
        static HashSet<int> idsUsados = new HashSet<int>();

        static void Main(string[] args)
        {
            Thread[] hilos = new Thread[20];

            // Generamos 20 componentes
            for (int i = 0; i < 20; i++)
            {
                int orden = i + 1;
                Componente componente = CrearComponente(orden);

                // Mostrar llegada al sistema
                Log(componente, "Llegado al sistema");

                hilos[i] = new Thread(() => ProcesarComponente(componente));
                hilos[i].Start();

                // Llega un componente cada 2 segundos
                Thread.Sleep(2000);
            }

            // Esperar a que todos terminen
            for (int i = 0; i < 20; i++)
            {
                hilos[i].Join();
            }

            Console.WriteLine("Fin de la simulación.");
        }

        // Crear componente con valores aleatorios
        static Componente CrearComponente(int ordenLlegada)
        {
            int id;
            int tiempoMecanizado;
            bool requiereInspeccion;

            lock (lockRandom)
            {
                tiempoMecanizado = random.Next(5, 16);       // 5 a 15 segundos
                requiereInspeccion = random.Next(0, 2) == 1; // true o false
            }

            // Generar ID único entre 1 y 100
            lock (lockIds)
            {
                do
                {
                    lock (lockRandom)
                    {
                        id = random.Next(1, 101);
                    }
                }
                while (idsUsados.Contains(id));

                idsUsados.Add(id);
            }

            return new Componente(id, tiempoMecanizado, requiereInspeccion, ordenLlegada);
        }

        // Procesar el recorrido completo del componente
        static void ProcesarComponente(Componente componente)
        {
            bool yaHaMostradoEsperaMecanizado = false;

            // Esperar estación libre
            while (!estaciones.Wait(0))
            {
                if (!yaHaMostradoEsperaMecanizado)
                {
                    componente.Estado = EstadoComponente.EsperaMecanizado;
                    Log(componente, "Esperando estación de mecanizado");
                    yaHaMostradoEsperaMecanizado = true;
                }

                Thread.Sleep(250);
            }

            // Entra en mecanizado
            componente.Estado = EstadoComponente.EnMecanizado;
            Log(componente, "Entra en mecanizado");

            Thread.Sleep(componente.TiempoMecanizado * 1000);

            // Libera estación
            estaciones.Release();

            // Si necesita inspección, pasa por QC
            if (componente.RequiereInspeccion)
            {
                componente.Estado = EstadoComponente.EsperaInspeccion;
                Log(componente, "Esperando control de calidad");

                qc.Wait();

                componente.Estado = EstadoComponente.EnInspeccion;
                Log(componente, "Entra en control de calidad");

                Thread.Sleep(15000);

                qc.Release();
            }

            // Finaliza
            componente.Estado = EstadoComponente.Completado;
            Log(componente, "Completado");
        }

        // Método de log
        static void Log(Componente componente, string mensaje)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {componente.Id}. " +
                    $"Entrada {componente.OrdenLlegada}. " +
                    $"Estado: {componente.Estado}. " +
                    $"TiempoMecanizado: {componente.TiempoMecanizado}s. " +
                    $"QC={componente.RequiereInspeccion}. " +
                    $"{mensaje}");
            }
        }
    }
}