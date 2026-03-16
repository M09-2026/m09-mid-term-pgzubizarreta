using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Tarea3
{
    // Clase que representa un componente
    public class Componente
    {
        // Identificador del componente
        public int Id { get; set; }

        // Tiempo de entrada al sistema
        public int TiempoEntrada { get; set; }

        // Tiempo que tarda en mecanizarse
        public int TiempoMecanizado { get; set; }

        // Estado del componente
        // 0 = En espera
        // 1 = En mecanizado
        // 2 = Completado
        public int Estado { get; set; }

        // Constructor
        public Componente(int id, int tiempoEntrada, int tiempoMecanizado)
        {
            Id = id;
            TiempoEntrada = tiempoEntrada;
            TiempoMecanizado = tiempoMecanizado;
            Estado = 0;
        }
    }

    class Program
    {
        // Array para controlar si las 4 estaciones están libres u ocupadas
        // false = libre
        // true = ocupada
        static bool[] estaciones = new bool[4];

        // Objeto de bloqueo para sincronizar el acceso a las estaciones
        static object bloqueo = new object();

        // Cronómetro global para medir tiempos de espera
        static Stopwatch reloj = new Stopwatch();

        static void Main(string[] args)
        {
            // Generador de números aleatorios
            Random rnd = new Random();

            // Lista para guardar todos los hilos
            List<Thread> hilos = new List<Thread>();

            // Iniciamos el cronómetro global
            reloj.Start();

            // Simulamos la llegada de 4 componentes
            for (int i = 1; i <= 4; i++)
            {
                // Creamos un componente con:
                // ID aleatorio entre 1 y 100
                // tiempo de entrada 0, 2, 4, 6
                // tiempo de mecanizado entre 5 y 15 segundos
                Componente componente = new Componente(
                    rnd.Next(1, 101),
                    (i - 1) * 2,
                    rnd.Next(5, 16)
                );

                // Mostramos que el componente ha llegado
                MostrarLog(componente, i, "Llegado", 0);

                int estacionAsignada = -1;

                // Guardamos el momento en que empieza a buscar estación
                long inicioEspera = reloj.ElapsedMilliseconds;

                // Mientras no se asigne una estación libre, sigue intentando
                while (estacionAsignada == -1)
                {
                    // Elegimos una estación aleatoria entre 0 y 3
                    int intento = rnd.Next(0, 4);

                    // Bloqueamos el acceso al array de estaciones
                    lock (bloqueo)
                    {
                        // Si la estación está libre, la ocupamos
                        if (!estaciones[intento])
                        {
                            estaciones[intento] = true;
                            estacionAsignada = intento;
                        }
                    }

                    // Si no se ha encontrado estación, esperamos un poco
                    if (estacionAsignada == -1)
                    {
                        Thread.Sleep(200);
                    }
                }

                // Calculamos cuánto tiempo ha esperado
                long finEspera = reloj.ElapsedMilliseconds;
                int duracionEspera = (int)((finEspera - inicioEspera) / 1000);

                // Solo mostramos "EnEspera" si realmente ha esperado más de 0 segundos
                if (duracionEspera > 0)
                {
                    MostrarLog(componente, i, "EnEspera", duracionEspera);
                }

                // La estación se muestra de 1 a 4
                int estacion = estacionAsignada + 1;

                // Guardamos el orden de llegada en variable local
                int ordenLlegada = i;

                // Creamos un hilo para procesar el componente
                Thread t = new Thread(() => Procesar(componente, ordenLlegada, estacion));

                // Añadimos el hilo a la lista
                hilos.Add(t);

                // Iniciamos el hilo
                t.Start();

                // Los componentes llegan cada 2 segundos
                Thread.Sleep(2000);
            }

            // Esperamos a que todos los hilos terminen
            foreach (Thread t in hilos)
            {
                t.Join();
            }

            Console.WriteLine("Fin de la simulación.");
        }

        // Método que simula el mecanizado del componente
        static void Procesar(Componente componente, int ordenLlegada, int estacion)
        {
            // Cambiamos el estado a EnMecanizado
            componente.Estado = 1;

            // Mostramos el log del mecanizado
            MostrarLog(componente, ordenLlegada, "EnMecanizado", componente.TiempoMecanizado);

            // Simulamos el mecanizado
            Thread.Sleep(componente.TiempoMecanizado * 1000);

            // Cambiamos el estado a Completado
            componente.Estado = 2;

            // Mostramos el log de completado
            MostrarLog(componente, ordenLlegada, "Completado", 0);

            // Liberamos la estación para que otro componente pueda usarla
            lock (bloqueo)
            {
                estaciones[estacion - 1] = false;
            }
        }

        // Método para mostrar mensajes de log
        static void MostrarLog(Componente componente, int ordenLlegada, string estadoTexto, int duracion)
        {
            Console.WriteLine($"Componente {componente.Id}. Entrada {ordenLlegada}. Estado: {estadoTexto}. Duración: {duracion} segundos.");
        }
    }
}