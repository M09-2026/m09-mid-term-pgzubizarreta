using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ejercicio2.Tarea5
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
        public int Id { get; set; }                      // ID único
        public int TiempoMecanizado { get; set; }       // Tiempo de mecanizado
        public bool RequiereInspeccion { get; set; }    // Si necesita QC
        public EstadoComponente Estado { get; set; }    // Estado actual
        public int OrdenLlegada { get; set; }           // Orden de llegada
        public int Prioridad { get; set; }              // Prioridad 1, 2 o 3

        public DateTime InstanteLlegadaBuffer { get; set; }   // Cuándo entra al buffer
        public DateTime InstanteInicioMecanizado { get; set; } // Cuándo empieza mecanizado
        public double TiempoEsperaBuffer { get; set; }         // Tiempo total de espera

        public Componente(int id, int tiempo, bool inspeccion, int orden, int prioridad)
        {
            Id = id;
            TiempoMecanizado = tiempo;
            RequiereInspeccion = inspeccion;
            OrdenLlegada = orden;
            Prioridad = prioridad;
            InstanteLlegadaBuffer = DateTime.Now;
        }
    }

    class Program
    {
        // Buffer de espera para mecanizado
        static List<Componente> buffer = new List<Componente>();

        // Lista de componentes terminados
        static List<Componente> completados = new List<Componente>();

        // Locks
        static object lockBuffer = new object();
        static object lockCompletados = new object();
        static object lockConsola = new object();
        static object lockRandom = new object();
        static object lockIds = new object();
        static object lockQcUso = new object();

        // Recursos
        static SemaphoreSlim qc = new SemaphoreSlim(2, 2);

        static Random random = new Random();
        static HashSet<int> idsUsados = new HashSet<int>();

        static int estacionesLibres = 4;
        static bool generacionFinalizada = false;

        // Medición global
        static Stopwatch relojGlobal = new Stopwatch();
        static double tiempoQcTotal = 0;

        static void Main(string[] args)
        {
            relojGlobal.Start();

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

            relojGlobal.Stop();

            MostrarInformeFinal();
        }

        // Crea un componente con datos aleatorios
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

        // Planifica la entrada a mecanizado según prioridad
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
            // Mide espera en buffer
            c.InstanteInicioMecanizado = DateTime.Now;
            c.TiempoEsperaBuffer = (c.InstanteInicioMecanizado - c.InstanteLlegadaBuffer).TotalSeconds;

            // Mecanizado
            c.Estado = EstadoComponente.EnMecanizado;
            Log(c, c.Estado);

            Thread.Sleep(c.TiempoMecanizado * 1000);

            lock (lockBuffer)
            {
                estacionesLibres++;
            }

            // Control de calidad
            if (c.RequiereInspeccion)
            {
                c.Estado = EstadoComponente.EsperaInspeccion;
                Log(c, c.Estado);

                qc.Wait();

                c.Estado = EstadoComponente.EnInspeccion;
                Log(c, c.Estado);

                Stopwatch swQc = Stopwatch.StartNew();

                Thread.Sleep(15000);

                swQc.Stop();

                lock (lockQcUso)
                {
                    tiempoQcTotal += swQc.Elapsed.TotalSeconds;
                }

                qc.Release();
            }

            // Finaliza
            c.Estado = EstadoComponente.Completado;
            Log(c, c.Estado);

            lock (lockCompletados)
            {
                completados.Add(c);
            }
        }

        // Muestra el estado del componente
        static void Log(Componente c, EstadoComponente estado)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {c.Id}. Entrada {c.OrdenLlegada}. Prioridad {c.Prioridad}. Estado: {estado}. QC={c.RequiereInspeccion}");
            }
        }

        // Muestra el informe final del día
        static void MostrarInformeFinal()
        {
            Console.WriteLine();
            Console.WriteLine("----- FIN DEL DÍA -----");
            Console.WriteLine("Componentes producidos por prioridad:");

            int prioridad1 = completados.Count(c => c.Prioridad == 1);
            int prioridad2 = completados.Count(c => c.Prioridad == 2);
            int prioridad3 = completados.Count(c => c.Prioridad == 3);

            Console.WriteLine($"Prioridad 1: {prioridad1}");
            Console.WriteLine($"Prioridad 2: {prioridad2}");
            Console.WriteLine($"Prioridad 3: {prioridad3}");

            Console.WriteLine();
            Console.WriteLine("Tiempo medio de espera en buffer por prioridad:");

            double media1 = completados
                .Where(c => c.Prioridad == 1)
                .Select(c => c.TiempoEsperaBuffer)
                .DefaultIfEmpty(0)
                .Average();

            double media2 = completados
                .Where(c => c.Prioridad == 2)
                .Select(c => c.TiempoEsperaBuffer)
                .DefaultIfEmpty(0)
                .Average();

            double media3 = completados
                .Where(c => c.Prioridad == 3)
                .Select(c => c.TiempoEsperaBuffer)
                .DefaultIfEmpty(0)
                .Average();

            Console.WriteLine($"Prioridad 1: {media1:F2} segundos");
            Console.WriteLine($"Prioridad 2: {media2:F2} segundos");
            Console.WriteLine($"Prioridad 3: {media3:F2} segundos");

            Console.WriteLine();
            Console.WriteLine("Uso medio de las máquinas de control de calidad:");

            double tiempoTotal = relojGlobal.Elapsed.TotalSeconds;
            double usoQc = 0;

            if (tiempoTotal > 0)
            {
                usoQc = (tiempoQcTotal / (2 * tiempoTotal)) * 100;
            }

            Console.WriteLine($"{usoQc:F2}%");
        }
    }
}
