using System;
using System.Threading;

namespace Ejercicio2.Tarea2
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

    // Clase que representa un componente de la línea
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

        // 2 máquinas de QC (se mantienen porque forman parte de la planta)
        static SemaphoreSlim qc = new SemaphoreSlim(2, 2);

        // Control del orden secuencial estricto en QC
        static object lockOrdenQc = new object();
        static int siguienteOrdenQc = 1;

        // Locks auxiliares
        static object lockRandom = new object();
        static object lockConsola = new object();

        static Random random = new Random();

        static void Main(string[] args)
        {
            Thread[] hilos = new Thread[4];

            for (int i = 0; i < 4; i++)
            {
                int orden = i + 1;
                Componente componente = CrearComponente(orden);

                Log(componente, EstadoComponente.EsperaMecanizado);

                hilos[i] = new Thread(() => ProcesarComponente(componente));
                hilos[i].Start();

                // Llega un componente cada 2 segundos
                Thread.Sleep(2000);
            }

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
            int tiempo;
            bool inspeccion;

            lock (lockRandom)
            {
                id = random.Next(1, 101);
                tiempo = random.Next(5, 16);
                inspeccion = true; // todos pasan por QC para comprobar el orden
            }

            return new Componente(id, tiempo, inspeccion, orden);
        }

        // Procesa el ciclo completo del componente
        static void ProcesarComponente(Componente componente)
        {
            // Espera estación de mecanizado
            estaciones.Wait();

            componente.Estado = EstadoComponente.EnMecanizado;
            Log(componente, componente.Estado);

            Thread.Sleep(componente.TiempoMecanizado * 1000);

            estaciones.Release();

            // Todos pasan por QC en esta tarea para comprobar el orden secuencial
            if (componente.RequiereInspeccion)
            {
                componente.Estado = EstadoComponente.EsperaInspeccion;
                Log(componente, componente.Estado);

                // Espera hasta que sea exactamente su turno
                bool miTurno = false;
                while (!miTurno)
                {
                    lock (lockOrdenQc)
                    {
                        if (componente.OrdenLlegada == siguienteOrdenQc)
                        {
                            miTurno = true;
                        }
                    }

                    if (!miTurno)
                    {
                        Thread.Sleep(100);
                    }
                }

                // Entra en una máquina de QC
                qc.Wait();

                componente.Estado = EstadoComponente.EnInspeccion;
                Log(componente, componente.Estado);

                // Inspección fija de 15 segundos
                Thread.Sleep(15000);

                qc.Release();

                // Solo al terminar completamente la inspección
                // se permite avanzar al siguiente componente
                lock (lockOrdenQc)
                {
                    siguienteOrdenQc++;
                }
            }

            componente.Estado = EstadoComponente.Completado;
            Log(componente, componente.Estado);
        }

        // Muestra por consola el estado del componente
        static void Log(Componente componente, EstadoComponente estado)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {componente.Id}. " +
                    $"Entrada {componente.OrdenLlegada}. " +
                    $"Estado: {estado}. " +
                    $"TiempoMecanizado: {componente.TiempoMecanizado}s. " +
                    $"QC={componente.RequiereInspeccion}");
            }
        }
    }
}
