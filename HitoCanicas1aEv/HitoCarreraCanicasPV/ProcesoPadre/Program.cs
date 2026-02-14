using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CarreraCanicas
{
    class Program
    {
        static void Main(string[] args)
        {
            int numPistas = 3; // Número de pistas
            int numCanicasPorPista = 5; // Número de canicas por pista
            
            Dictionary<int, List<KeyValuePair<int, long>>> resultadosPorPista = new Dictionary<int, List<KeyValuePair<int, long>>>();

            // Se lanza un proceso hijo por cada pista
            for (int i = 0; i < numPistas; i++)
            {
                Console.WriteLine($"\nInicio carrera " + (i + 1));
                Process procesoHijo = new Process();
                procesoHijo.StartInfo.FileName = @"C:\REPOS\PSP\Ejercicios\HitoCarreraCanicas\ProcesoHijo\bin\Debug\net8.0\ProcesoHijo.exe"; // El ejecutable que simula la pista
                procesoHijo.StartInfo.Arguments = $"{i + 1} {numCanicasPorPista}"; // Pista y número de canicas
                procesoHijo.StartInfo.UseShellExecute = false;
                procesoHijo.StartInfo.RedirectStandardOutput = true;
                procesoHijo.Start();

                // Obtiene la salida del proceso hijo
                string salida = procesoHijo.StandardOutput.ReadToEnd();
                procesoHijo.WaitForExit();

                // Separo las diferentes líneas de la salida
                var lineas = salida.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries); // Separa las líneas

                var rankingPista = new List<KeyValuePair<int, long>>();


                foreach (string linea in lineas)
                {
                    string[] partes = linea.Split('|');
                    int idPista = int.Parse(partes[0]);
                    int idCanica = int.Parse(partes[1]);
                    long tiempo = long.Parse(partes[2]);

                    // Guardo los resultados de cada pista en una lista de pares clave valor
                    if (!resultadosPorPista.ContainsKey(idPista))
                    {
                        resultadosPorPista[idPista] = new List<KeyValuePair<int, long>>();
                    }
                    resultadosPorPista[idPista].Add(new KeyValuePair<int, long>(idCanica, tiempo));
                }
                Console.WriteLine($"Fin carrera " + (i + 1));
                Thread.Sleep(1000);
            }

            // Muestro el ranking de cada pista
            Console.WriteLine("\n················");
            Console.WriteLine("·   RANKINGS   ·");
            Console.WriteLine("················");

            foreach (KeyValuePair<int, List<KeyValuePair<int, long>>> pista in resultadosPorPista.OrderBy(r => r.Key))
            {
                int posicion = 1;

                string paisMasRapido = "";

                Console.WriteLine($"\n·   PISTA {pista.Key}   ·");
                Console.WriteLine("···············");

                foreach (var canica in pista.Value.OrderBy(c => c.Value))
                {
                    string pais = "";

                    switch (canica.Key)
                    {
                        case 1:
                            pais = "España";
                            break;
                        case 2:
                            pais = "Perú";
                            break;
                        case 3:
                            pais = "Argentina";
                            break;
                        case 4:
                            pais = "Francia";
                            break;
                        case 5:
                            pais = "Alemania";
                            break;
                    }

                    if (posicion == 1)
                    {
                        paisMasRapido = pais;
                    }

                    Console.WriteLine($"{posicion++}. {pais} (Tiempo: {canica.Value} ms)");
                }

                Console.WriteLine($"El país más rápido de la pista {pista.Key} es: {paisMasRapido}");
            }
        }
    }
}
