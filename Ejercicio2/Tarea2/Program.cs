using System;
using System.Collections.Generic;
using System.Threading;

namespace Ejercicio2.Tarea1
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

    // Clase componente
    public class Componente
    {
        public int Id { get; set; }
        public int TiempoMecanizado { get; set; }
        public bool RequiereInspeccion { get; set; }
        public EstadoComponente Estado { get; set; }
        public int OrdenLlegada { get; set; }

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
        static SemaphoreSlim estaciones = new SemaphoreSlim(4);

        // 2 máquinas de control de calidad
        static SemaphoreSlim qc = new SemaphoreSlim(2);

        static Random random = new Random();
        static object lockRandom = new object();
        static object lockConsola = new object();

        static void Main(string[] args)
        {
            Thread[] hilos = new Thread[4];

            for (int i = 0; i < 4; i++)
            {
                int orden = i + 1;
                Componente c = CrearComponente(orden);

                Log(c, EstadoComponente.EsperaMecanizado);

                hilos[i] = new Thread(() => ProcesarComponente(c));
                hilos[i].Start();

                Thread.Sleep(2000);
            }

            for (int i = 0; i < 4; i++)
                hilos[i].Join();

            Console.WriteLine("Fin de la simulación.");
        }

        static Componente CrearComponente(int orden)
        {
            int id;
            int tiempo;
            bool inspeccion;

            lock (lockRandom)
            {
                id = random.Next(1, 101);
                tiempo = random.Next(5, 16);
                inspeccion = random.Next(0, 2) == 1;
            }

            return new Componente(id, tiempo, inspeccion, orden);
        }

        static void ProcesarComponente(Componente c)
        {
            // Espera estación de mecanizado
            estaciones.Wait();

            c.Estado = EstadoComponente.EnMecanizado;
            Log(c, c.Estado);

            Thread.Sleep(c.TiempoMecanizado * 1000);

            estaciones.Release();

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

            c.Estado = EstadoComponente.Completado;
            Log(c, c.Estado);
        }

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
