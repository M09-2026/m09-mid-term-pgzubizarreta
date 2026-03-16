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

    // Clase componente
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

        // Control del orden de entrada a QC
        static object lockOrdenQc = new object();
        static int siguienteOrdenQc = 1;

        // Otros locks compartidos
        static object lockRandom = new object();
        static object lockConsola = new object();

        static Random random = new Random();

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
                tiempo = random.Next(5, 16);   // entre 5 y 15
                inspeccion = true;             // todos pasan por QC para ver bien el orden
            }

            return new Componente(id, tiempo, inspeccion, orden);
        }

        // Procesa el ciclo completo del componente
        static void ProcesarComponente(Componente c)
        {
            // Espera una estación de mecanizado
            estaciones.Wait();

            c.Estado = EstadoComponente.EnMecanizado;
            Log(c, c.Estado);

            Thread.Sleep(c.TiempoMecanizado * 1000);

            estaciones.Release();

            // Pasa a espera de inspección
            if (c.RequiereInspeccion)
            {
                c.Estado = EstadoComponente.EsperaInspeccion;
                Log(c, c.Estado);

                // Espera a que sea su turno según el orden de llegada
                while (true)
                {
                    bool puedeEntrar = false;

                    lock (lockOrdenQc)
                    {
                        if (c.OrdenLlegada == siguienteOrdenQc)
                        {
                            siguienteOrdenQc++;
                            puedeEntrar = true;
                        }
                    }

                    if (puedeEntrar)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                // Espera una máquina QC libre
                qc.Wait();

                c.Estado = EstadoComponente.EnInspeccion;
                Log(c, c.Estado);

                Thread.Sleep(15000);

                qc.Release();
            }

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
