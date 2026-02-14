using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CarreraCanicas
{
    class Program
    {
        class Pista
        {
            public List<KeyValuePair<int, long>> Carrera(int idPista, int numCanicas)
            {
                List<Thread> hilosCanicas = new List<Thread>();
                Dictionary<int, long> canicas = new Dictionary<int, long>();

                for (int i = 0; i < numCanicas; i++)
                {
                    int idCanica = i + 1;
                    Thread hiloCanica = new Thread(() =>
                    {
                        lock (canicas) // Asegura que el acceso al diccionario sea seguro entre hilos
                        {
                            canicas[idCanica] = Canica(); // Asigno el tiempo tardado a la canica
                        }
                    });

                    hilosCanicas.Add(hiloCanica);
                    hiloCanica.Start();
                }

                // Espera a que todos los hilos de canicas terminen
                foreach (Thread hilo in hilosCanicas)
                {
                    hilo.Join();
                }

                // Ordena las canicas por tiempo (de menor a mayor) y devuelve el resultado
                return canicas.OrderBy(c => c.Value).ToList();
            }

            private long Canica()
            {
                Stopwatch crono = new Stopwatch();
                crono.Start();

                Thread.Sleep(500);

                crono.Stop();
                return crono.ElapsedMilliseconds;
            }
        }

        static void Main(string[] args)
        {
            int idPista = int.Parse(args[0]);
            int numCanicas = int.Parse(args[1]);

            Pista pista = new Pista();
            List<KeyValuePair<int, long>> rankingPista = pista.Carrera(idPista, numCanicas);

            // Devuelvo los datos de cada pista canica a canica
            foreach (KeyValuePair<int, long> canica in rankingPista)
            {
                Console.WriteLine($"{idPista}|{canica.Key}|{canica.Value}");
            }
        }
    }
}
