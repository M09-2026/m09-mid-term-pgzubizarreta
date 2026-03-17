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
        public int Id { get; set; }
        public int TiempoMecanizado { get; set; }
        public bool RequiereInspeccion { get; set; }
        public EstadoComponente Estado { get; set; }
        public int OrdenLlegada { get; set; }
        public int Prioridad { get; set; }

        public DateTime InstanteLlegadaBuffer { get; set; }
        public DateTime InstanteInicioMecanizado { get; set; }
        public double TiempoEsperaBuffer { get; set; }

        public Componente(int id, int tiempoMecanizado, bool requiereInspeccion, int ordenLlegada, int prioridad)
        {
            Id = id;
            TiempoMecanizado = tiempoMecanizado;
            RequiereInspeccion = requiereInspeccion;
            OrdenLlegada = ordenLlegada;
            Prioridad = prioridad;
            Estado = EstadoComponente.EsperaMecanizado;
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
        static object lockEstado = new object();

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

            // Generar 20 componentes
            for (int i = 0; i < 20; i++)
            {
                int orden = i + 1;
                Componente componente = CrearComponente(orden);

                lock (lockBuffer)
                {
                    buffer.Add(componente);
                }

                Log(componente, "Llegado al búfer");

                Thread.Sleep(2000);
            }

            lock (lockEstado)
            {
                generacionFinalizada = true;
            }

            planificador.Join();

            relojGlobal.Stop();

            MostrarInformeFinal();
        }

        // Crear componente con datos aleatorios
        static Componente CrearComponente(int ordenLlegada)
        {
            int id;
            int tiempoMecanizado;
            bool requiereInspeccion;
            int prioridad;

            lock (lockRandom)
            {
                tiempoMecanizado = random.Next(5, 16);
                requiereInspeccion = random.Next(0, 2) == 1;
                prioridad = random.Next(1, 4);
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

            return new Componente(id, tiempoMecanizado, requiereInspeccion, ordenLlegada, prioridad);
        }

        // Planificador de entrada a mecanizado
        static void PlanificarMecanizado()
        {
            List<Thread> hilosActivos = new List<Thread>();

            while (true)
            {
                Componente? siguiente = null;
                bool terminar = false;

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
                    else
                    {
                        lock (lockEstado)
                        {
                            if (generacionFinalizada && buffer.Count == 0 && estacionesLibres == 4)
                            {
                                terminar = true;
                            }
                        }
                    }
                }

                if (siguiente != null)
                {
                    Thread hilo = new Thread(() => ProcesarComponente(siguiente));
                    hilosActivos.Add(hilo);
                    hilo.Start();
                }
                else if (terminar)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            foreach (Thread hilo in hilosActivos)
            {
                hilo.Join();
            }
        }

        // Procesar un componente
        static void ProcesarComponente(Componente componente)
        {
            // Tiempo de espera en buffer
            componente.InstanteInicioMecanizado = DateTime.Now;
            componente.TiempoEsperaBuffer =
                (componente.InstanteInicioMecanizado - componente.InstanteLlegadaBuffer).TotalSeconds;

            // Mecanizado
            componente.Estado = EstadoComponente.EnMecanizado;
            Log(componente, "Entra en mecanizado");

            Thread.Sleep(componente.TiempoMecanizado * 1000);

            lock (lockBuffer)
            {
                estacionesLibres++;
            }

            // Control de calidad
            if (componente.RequiereInspeccion)
            {
                componente.Estado = EstadoComponente.EsperaInspeccion;
                Log(componente, "Esperando control de calidad");

                qc.Wait();

                componente.Estado = EstadoComponente.EnInspeccion;
                Log(componente, "Entra en control de calidad");

                Stopwatch swQc = Stopwatch.StartNew();

                Thread.Sleep(15000);

                swQc.Stop();

                lock (lockQcUso)
                {
                    tiempoQcTotal += swQc.Elapsed.TotalSeconds;
                }

                qc.Release();
            }

            componente.Estado = EstadoComponente.Completado;
            Log(componente, "Completado");

            lock (lockCompletados)
            {
                completados.Add(componente);
            }
        }

        // Log por consola
        static void Log(Componente componente, string mensaje)
        {
            lock (lockConsola)
            {
                Console.WriteLine(
                    $"Componente {componente.Id}. " +
                    $"Entrada {componente.OrdenLlegada}. " +
                    $"Prioridad {componente.Prioridad}. " +
                    $"Estado: {componente.Estado}. " +
                    $"QC={componente.RequiereInspeccion}. " +
                    $"{mensaje}");
            }
        }

        // Informe final
        static void MostrarInformeFinal()
        {
            Console.WriteLine();
            Console.WriteLine("--- FIN DEL DÍA ---");
            Console.WriteLine("Componentes producidos:");

            int flash = completados.Count(c => c.Prioridad == 1);
            int estandar = completados.Count(c => c.Prioridad == 2);
            int almacen = completados.Count(c => c.Prioridad == 3);

            Console.WriteLine($"- Flash: {flash}");
            Console.WriteLine($"- Estandar: {estandar}");
            Console.WriteLine($"- Almacen: {almacen}");

            Console.WriteLine();
            Console.WriteLine("Tiempo promedio de espera:");

            double mediaFlash = completados
                .Where(c => c.Prioridad == 1)
                .Select(c => c.TiempoEsperaBuffer)
                .DefaultIfEmpty(0)
                .Average();

            double mediaEstandar = completados
                .Where(c => c.Prioridad == 2)
                .Select(c => c.TiempoEsperaBuffer)
                .DefaultIfEmpty(0)
                .Average();

            double mediaAlmacen = completados
                .Where(c => c.Prioridad == 3)
                .Select(c => c.TiempoEsperaBuffer)
                .DefaultIfEmpty(0)
                .Average();

            Console.WriteLine($"- Flash: {mediaFlash:F2}s");
            Console.WriteLine($"- Estandar: {mediaEstandar:F2}s");
            Console.WriteLine($"- Almacen: {mediaAlmacen:F2}s");

            Console.WriteLine();
            Console.WriteLine("Uso promedio de máquinas de control de calidad:");

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