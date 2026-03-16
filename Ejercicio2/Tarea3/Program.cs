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
        public int Id { get; set; }                    // ID del componente
        public int TiempoMecanizado { get; set; }     // Tiempo de mecanizado
        public bool RequiereInspeccion { get; set; }  // Indica si pasa por QC
        public EstadoComponente Estado { get; set; }  // Estado actual
        public int OrdenLlegada { get; set; }         // Orden de llegada

        public Componente(int id, int tiempo, bool inspeccion, int orden)
        {
            Id = id;
            TiempoMecanizado = tiempo;
            RequiereInspeccion = inspeccion;
            OrdenLlegada = orden;
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

            // Se generan 20 componentes
            for (int i = 0; i < 20; i++)
            {
                int orden = i + 1;
                Componente c = CrearComponente(orden);

                // Al llegar, entra en espera de mecanizado
                c.Estado = EstadoComponente.EsperaMecanizado;
                Log(c, c.Estado);

                hilos[i] = new Thread(() => ProcesarComponente(c));
                hilos[i].Start();

                // Llega un componente cada 2 segundos
                Thread.Sleep(2000);
            }

            // Espera a que terminen todos los hilos
            for (int i = 0; i < 20; i++)
            {
                hilos[i].Join();
            }

            Console.WriteLine("Fin de la simulación.");
        }

        // Crea un componente con valores aleatorios
        static Componente CrearComponente(int orden)
        {
            int id;
            int tiempo;
            bool inspeccion;

            lock (lockRandom)
            {
                tiempo = random.Next(5, 16);          // 5 a 15 segundos
                inspeccion = random.Next(0, 2) == 1;  // true o false
            }

            // Genera un ID único
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

            return new Componente(id, tiempo, inspeccion, orden);
        }

        // Procesa el componente
        static void ProcesarComponente(Componente c)
        {
            // Mientras no consiga estación, sigue en espera
            while (!estaciones.Wait(0))
            {
                c.Estado = EstadoComponente.EsperaMecanizado;
                Thread.Sleep(250);
            }

            // Entra en mecanizado
            c.Estado = EstadoComponente.EnMecanizado;
            Log(c, c.Estado);

            Thread.Sleep(c.TiempoMecanizado * 1000);

            // Libera estación de mecanizado
            estaciones.Release();

            // Si necesita inspección, pasa por QC
            if (c.RequiereInspeccion)
            {
                c.Estado = EstadoComponente.EsperaInspeccion;
                Log(c, c.Estado);

                qc.Wait();

                c.Estado = EstadoComponente.EnInspeccion;
                Log(c, c.Estado);

                Thread.Sleep(15000);

                qc.Release();
            }

            // Componente completado
            c.Estado = EstadoComponente.Completado;
            Log(c, c.Estado);
        }

        // Muestra el estado del componente
        static void Log(Componente c, EstadoComponente estado)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {c.Id}. Entrada {c.OrdenLlegada}. Estado: {estado}. QC={c.RequiereInspeccion}");
            }
        }
    }
}
