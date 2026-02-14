using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServidorTCP
{
    class Program
    {
        // Lista que almacena todos los clientes conectados 
        // (Es static para que todos los hilos puedan acceder a la misma lista)
        private static List<TcpClient> _clientes = new List<TcpClient>();

        // Objeto simple para bloquear el acceso a la lista _clientes
        // (Evita errores cuando dos hilos intentan modificar la lista al mismo tiempo)
        private static readonly object _candado = new object();

        private static TcpListener? _servidorListener;

        static void Main(string[] args)
        {
            int puerto = 5000;

            // Creación del listener que escuchará las conexiones entrantes
            // (IPAddress.Any indica que escucha en todas las interfaces de red (Wi-Fi, Ethernet, localhost))
            _servidorListener = new TcpListener(IPAddress.Any, puerto);
            
            _servidorListener.Start(); // Arranca el servidor

            Console.WriteLine($"Servidor iniciado (Puerto: {puerto}). Esperando clientes...");

            // Bucle infinito que acepta las nuevas conexiones
            while (true)
            {
                // Al llamar al método AcceptTcpClient() (llamada BLOQUEANTE)
                // el código se detiene y espera hasta que alguien intente conectarse
                TcpClient clienteNuevo = _servidorListener.AcceptTcpClient();

                // Cuando un cliente se conecte, se le agrega a la lista 
                /* El candado (lock) hace que solo un hilo pueda acceder 
                 * y modificar la lista a la vez. Cuando un hilo (cliente) entra, 
                 * los demás deben esperar. Así se evita que varios hilos modifiquen 
                 * la lista al mismo tiempo y se previenen errores de concurrencia.*/
                lock (_candado)
                {
                    _clientes.Add(clienteNuevo);
                }

                Console.WriteLine("Nuevo cliente conectado.");

                /* Si atendiéramos al cliente aquí mismo, el servidor se quedaría parado
                 * hablando solo con él y no podría aceptar a nadie más.
                 * Por eso, creamos un nuevo hilo (Thread) dedicado exclusivamente a este cliente. */
                Thread hiloCliente = new Thread(ManejoCliente);
                hiloCliente.IsBackground = true; // El hilo se ejecuta en segundo plano
                hiloCliente.Start(clienteNuevo);
            }
        }

        // Metodo que maneja un hilo independiente para cada cliente
        private static void ManejoCliente(object clienteNuevoRecibido)
        {
            TcpClient cliente = (TcpClient) clienteNuevoRecibido;
            NetworkStream stream = cliente.GetStream();
            byte[] buffer = new byte[1024]; // Espacio para recibir datos

            try
            {
                // Bucle conversación
                while (true)
                {
                    // Lee el mensaje del flujo de red (Se detiene hasta recibir algo)
                    int bytesLeidos = stream.Read(buffer, 0, buffer.Length);

                    // DETECCIÓN DE DESCONEXIÓN:
                    // En TCP, si Read devuelve 0, significa que el otro lado cerró la conexión.
                    if (bytesLeidos <= 0) break;

                    // Se pasan los bytes a texto legible
                    string mensaje = Encoding.UTF8.GetString(buffer, 0, bytesLeidos).Trim();

                    // Ignora mensajes vacíos
                    if (string.IsNullOrEmpty(mensaje)) continue;

                    // Muestra el mensaje recibido en la consola del servidor
                    Console.WriteLine($"Mensaje recibido: {mensaje}");

                    // Reenvia el mensaje a todos los clientes
                    Broadcast(mensaje, cliente);
                }
            }
            catch (Exception e)
            {
                // Captura errores (ejemplo: un cliente pierde el internet de golpe)
                Console.WriteLine("Error con un cliente: " + e.Message);
            }
            finally
            {
                // Cuando un cliente se desconecta, se le borra de la lista y se cierra su conexión.
                lock (_candado)
                {
                    _clientes.Remove(cliente);
                }
                cliente.Close();
                Console.WriteLine("Cliente desconectado.");
            }
        }

        // Función para enviar un mensaje a todos los clientes
        private static void Broadcast(string mensaje, TcpClient clienteEmisor)
        {
            // Agregamos un salto de línea al final para que Unity sepa cuándo termina el mensaje
            byte[] data = Encoding.UTF8.GetBytes(mensaje + "\n");

            // Bloqueo de la lista para recorrerla
            lock (_candado)
            {
                // Se envía el mensaje a todos los clientes menos al emisor
                foreach (TcpClient cliente in _clientes)
                {
                    if (cliente != clienteEmisor)
                    {
                        try
                        {
                            NetworkStream stream = cliente.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                        catch
                        {
                            // Si hay algún error en el envío del mensaje, este método lo ignora
                            // (El hilo ManejarCliente lo eliminará al intentar leerlo)
                        }
                    }
                }
            }
        }
    }
}