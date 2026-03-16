using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ejercicio2.Tarea4
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
        public int Id { get; set; }                    // ID único
        public int TiempoMecanizado { get; set; }     // Tiempo de mecanizado
        public bool RequiereInspeccion { get; set; }  // Si necesita QC
        public EstadoComponente Estado { get; set; }  // Estado actual
        public int OrdenLlegada { get; set; }         // Orden de llegada
        public int Prioridad { get; set; }            // Prioridad: 1, 2 o 3

        public Componente(int id, int tiempo, bool inspeccion, int orden, int prioridad)
        {
            Id = id;
            TiempoMecanizado = tiempo;
            RequiereInspeccion = inspeccion;
            OrdenLlegada = orden;
            Prioridad = prioridad;
        }
    }

    class Program
    {
        // Buffer de componentes en espera
        static List<Componente> buffer = new List<Componente>();

        // Locks
        static object lockBuffer = new object();
        static object lockConsola = new object();
        static object lockRandom = new object();
        static object lockIds = new object();

        // Recursos
        static SemaphoreSlim qc = new SemaphoreSlim(2, 2);

        static Random random = new Random();
        static HashSet<int> idsUsados = new HashSet<int>();

        static int estacionesLibres = 4;
        static bool generacionFinalizada = false;

        static void Main(string[] args)
        {
            // Hilo que planifica qué componente entra a mecanizado
            Thread planificador = new Thread(PlanificarMecanizado);
            planificador.Start();

            // Generamos 20 componentes
            for (int i = 0; i < 20; i++)
            {
                int orden = i + 1;
                Componente c = CrearComponente(orden);

                c.Estado = EstadoComponente.EsperaMecanizado;

                lock (lockBuffer)
                {
                    buffer.Add(c);
                }

                Log(c, c.Estado);

                // Llega un componente cada 2 segundos
                Thread.Sleep(2000);
            }

            generacionFinalizada = true;

            planificador.Join();

            Console.WriteLine("Fin de la simulación.");
        }

        // Crea un componente con valores aleatorios
        static Componente CrearComponente(int orden)
        {
            int id;
            int tiempo;
            bool inspeccion;
            int prioridad;

            lock (lockRandom)
            {
                tiempo = random.Next(5, 16);           // 5 a 15 s
                inspeccion = random.Next(0, 2) == 1;   // true o false
                prioridad = random.Next(1, 4);         // 1, 2 o 3
            }

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

            return new Componente(id, tiempo, inspeccion, orden, prioridad);
        }

        // Selecciona componentes del buffer según prioridad
        static void PlanificarMecanizado()
        {
            List<Thread> hilosActivos = new List<Thread>();

            while (true)
            {
                Componente siguiente = null;

                lock (lockBuffer)
                {
                    if (estacionesLibres > 0 && buffer.Count > 0)
                    {
                        siguiente = buffer
                            .OrderBy(c => c.Prioridad)
                            .ThenBy(c => c.OrdenLlegada)
                            .First();

                        buffer.Remove(siguiente);
                        estacionesLibres--;
                    }
                }

                if (siguiente != null)
                {
                    Thread hilo = new Thread(() => ProcesarComponente(siguiente));
                    hilosActivos.Add(hilo);
                    hilo.Start();
                }
                else
                {
                    bool terminar = false;

                    lock (lockBuffer)
                    {
                        if (generacionFinalizada && buffer.Count == 0 && estacionesLibres == 4)
                        {
                            terminar = true;
                        }
                    }

                    if (terminar)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }
            }

            foreach (Thread hilo in hilosActivos)
            {
                hilo.Join();
            }
        }

        // Procesa un componente
        static void ProcesarComponente(Componente c)
        {
            c.Estado = EstadoComponente.EnMecanizado;
            Log(c, c.Estado);

            Thread.Sleep(c.TiempoMecanizado * 1000);

            lock (lockBuffer)
            {
                estacionesLibres++;
            }

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

        // Muestra información del componente
        static void Log(Componente c, EstadoComponente estado)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {c.Id}. Entrada {c.OrdenLlegada}. Prioridad {c.Prioridad}. Estado: {estado}. QC={c.RequiereInspeccion}");
            }
        }
    }
}