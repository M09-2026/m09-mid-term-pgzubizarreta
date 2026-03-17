using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ejercicio3.Tarea1
{
    public enum EstadoComponente
    {
        EsperaBuffer,
        EsperaMecanizado,
        EnMecanizado,
        EsperaInspeccion,
        EnInspeccion,
        Completado
    }

    public class Componente
    {
        public int Id { get; set; }
        public int TiempoMecanizado { get; set; }
        public bool RequiereInspeccion { get; set; }
        public int Prioridad { get; set; }
        public int OrdenLlegada { get; set; }
        public EstadoComponente Estado { get; set; }

        public DateTime InstanteLlegadaSistema { get; set; }
        public DateTime InstanteEntradaBuffer { get; set; }
        public DateTime InstanteInicioMecanizado { get; set; }
        public DateTime InstanteFin { get; set; }

        public double TiempoEsperaBuffer { get; set; }

        public Componente(int id, int tiempoMecanizado, bool requiereInspeccion, int prioridad, int ordenLlegada)
        {
            Id = id;
            TiempoMecanizado = tiempoMecanizado;
            RequiereInspeccion = requiereInspeccion;
            Prioridad = prioridad;
            OrdenLlegada = ordenLlegada;
            Estado = EstadoComponente.EsperaBuffer;
            InstanteLlegadaSistema = DateTime.Now;
        }
    }

    class Program
    {
        // Configuración planta
        static readonly int capacidadBuffer = 20;
        static readonly int totalEstaciones = 4;

        // Recursos
        static SemaphoreSlim qc = new SemaphoreSlim(2, 2);

        // Estructuras compartidas
        static List<Componente> buffer = new List<Componente>();
        static List<Componente> completados = new List<Componente>();
        static HashSet<int> idsUsados = new HashSet<int>();

        // Locks
        static object lockBuffer = new object();
        static object lockCompletados = new object();
        static object lockIds = new object();
        static object lockRandom = new object();
        static object lockConsola = new object();
        static object lockEstado = new object();
        static object lockQcUso = new object();

        // Estado global
        static Random random = new Random();
        static int estacionesLibres = totalEstaciones;
        static bool generacionFinalizada = false;
        static int ordenGlobal = 0;
        static int rechazadosPorBufferLleno = 0;

        // Métricas
        static Stopwatch relojGlobal = new Stopwatch();
        static double tiempoQcTotal = 0;
        static int maxOcupacionBuffer = 0;

        static void Main(string[] args)
        {
            int n = LeerCantidadComponentes();

            relojGlobal.Start();

            Thread planificador = new Thread(PlanificarMecanizado);
            Thread generador = new Thread(() => GenerarComponentes(n));

            planificador.Start();
            generador.Start();

            generador.Join();
            planificador.Join();

            relojGlobal.Stop();

            MostrarInformeFinal(n);
        }

        static int LeerCantidadComponentes()
        {
            while (true)
            {
                Console.Write("Introduce N (50, 100 o 1000): ");
                string? entrada = Console.ReadLine();

                if (int.TryParse(entrada, out int n) && (n == 50 || n == 100 || n == 1000))
                {
                    return n;
                }

                Console.WriteLine("Valor no válido. Debes introducir 50, 100 o 1000.");
            }
        }

        static void GenerarComponentes(int n)
        {
            for (int i = 0; i < n; i++)
            {
                int orden;
                lock (lockEstado)
                {
                    ordenGlobal++;
                    orden = ordenGlobal;
                }

                Componente componente = CrearComponente(orden);

                bool añadido = false;
                int ocupacionActual = 0;

                lock (lockBuffer)
                {
                    if (buffer.Count < capacidadBuffer)
                    {
                        componente.Estado = EstadoComponente.EsperaMecanizado;
                        componente.InstanteEntradaBuffer = DateTime.Now;
                        buffer.Add(componente);
                        añadido = true;

                        ocupacionActual = buffer.Count;
                        if (ocupacionActual > maxOcupacionBuffer)
                        {
                            maxOcupacionBuffer = ocupacionActual;
                        }
                    }
                    else
                    {
                        rechazadosPorBufferLleno++;
                    }
                }

                if (añadido)
                {
                    Log(componente, $"Añadido al búfer. Ocupación búfer: {ocupacionActual}/{capacidadBuffer}");
                }
                else
                {
                    Log(componente, $"Búfer lleno. Componente descartado.");
                }

                Thread.Sleep(2000);
            }

            lock (lockEstado)
            {
                generacionFinalizada = true;
            }
        }

        static Componente CrearComponente(int ordenLlegada)
        {
            int id;
            int tiempo;
            bool inspeccion;
            int prioridad;

            lock (lockRandom)
            {
                tiempo = random.Next(5, 16);
                inspeccion = random.Next(0, 2) == 1;
                prioridad = random.Next(1, 4);
            }

            lock (lockIds)
            {
                do
                {
                    lock (lockRandom)
                    {
                        id = random.Next(1, 100000);
                    }
                }
                while (idsUsados.Contains(id));

                idsUsados.Add(id);
            }

            return new Componente(id, tiempo, inspeccion, prioridad, ordenLlegada);
        }

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
                            if (generacionFinalizada && buffer.Count == 0 && estacionesLibres == totalEstaciones)
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

        static void ProcesarComponente(Componente componente)
        {
            componente.InstanteInicioMecanizado = DateTime.Now;
            componente.TiempoEsperaBuffer =
                (componente.InstanteInicioMecanizado - componente.InstanteEntradaBuffer).TotalSeconds;

            componente.Estado = EstadoComponente.EnMecanizado;
            Log(componente, "Entra en mecanizado");

            Thread.Sleep(componente.TiempoMecanizado * 1000);

            lock (lockBuffer)
            {
                estacionesLibres++;
            }

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
            componente.InstanteFin = DateTime.Now;
            Log(componente, "Completado");

            lock (lockCompletados)
            {
                completados.Add(componente);
            }
        }

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

        static void MostrarInformeFinal(int n)
        {
            Console.WriteLine();
            Console.WriteLine("----- FIN DE LA SIMULACIÓN -----");
            Console.WriteLine($"Carga evaluada: N = {n}");
            Console.WriteLine();

            int totalProcesados;
            lock (lockCompletados)
            {
                totalProcesados = completados.Count;
            }

            Console.WriteLine($"Componentes generados: {n}");
            Console.WriteLine($"Componentes procesados: {totalProcesados}");
            Console.WriteLine($"Componentes descartados por búfer lleno: {rechazadosPorBufferLleno}");
            Console.WriteLine($"Ocupación máxima del búfer: {maxOcupacionBuffer}/{capacidadBuffer}");
            Console.WriteLine();

            int flash, estandar, almacen;
            double mediaFlash, mediaEstandar, mediaAlmacen;

            lock (lockCompletados)
            {
                flash = completados.Count(c => c.Prioridad == 1);
                estandar = completados.Count(c => c.Prioridad == 2);
                almacen = completados.Count(c => c.Prioridad == 3);

                mediaFlash = completados.Where(c => c.Prioridad == 1).Select(c => c.TiempoEsperaBuffer).DefaultIfEmpty(0).Average();
                mediaEstandar = completados.Where(c => c.Prioridad == 2).Select(c => c.TiempoEsperaBuffer).DefaultIfEmpty(0).Average();
                mediaAlmacen = completados.Where(c => c.Prioridad == 3).Select(c => c.TiempoEsperaBuffer).DefaultIfEmpty(0).Average();
            }

            Console.WriteLine("Componentes procesados por prioridad:");
            Console.WriteLine($"- Flash (1): {flash}");
            Console.WriteLine($"- Estandar (2): {estandar}");
            Console.WriteLine($"- Almacen (3): {almacen}");
            Console.WriteLine();

            Console.WriteLine("Tiempo medio de espera en búfer:");
            Console.WriteLine($"- Flash: {mediaFlash:F2} segundos");
            Console.WriteLine($"- Estandar: {mediaEstandar:F2} segundos");
            Console.WriteLine($"- Almacen: {mediaAlmacen:F2} segundos");
            Console.WriteLine();

            double tiempoTotal = relojGlobal.Elapsed.TotalSeconds;
            double usoQc = 0;
            if (tiempoTotal > 0)
            {
                usoQc = (tiempoQcTotal / (2 * tiempoTotal)) * 100;
            }

            Console.WriteLine($"Uso medio de QC: {usoQc:F2}%");
            Console.WriteLine();

            Console.WriteLine("Conclusión automática:");
            if (rechazadosPorBufferLleno > 0)
            {
                Console.WriteLine("- Se detecta cuello de botella en la entrada: el búfer alcanza su capacidad máxima.");
            }
            else if (usoQc > 80)
            {
                Console.WriteLine("- Se detecta alta carga en control de calidad.");
            }
            else
            {
                Console.WriteLine("- El sistema ha soportado la carga sin saturación crítica.");
            }
        }
    }
}
