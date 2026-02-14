using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ClienteTCP : MonoBehaviour
{
    [Header("Configuración conexión")]
    public string ip = "127.0.0.1";
    public int puerto = 5000;

    [Header("Referencias UI")]
    public Button btnEnviar;
    public TMP_InputField inputMensaje;
    public TMP_Text textoMensajes;

    private TcpClient cliente;
    private NetworkStream flujo;
    private Thread hiloRecepcion;
    private bool clienteActivo = false;

    private string mensajesRecibidos = "";

    private readonly object candado = new object();

    public ScrollRect scroll;


    void Start()
    {
        // Establecer la conexión con el servidor
        ConectarConServidor();

        // Asignar el evento de enviar mensajes al botón 'Enviar'
        if (btnEnviar != null) btnEnviar.onClick.AddListener(EnviarMensaje);        
    }

    private void Update()
    {
        // Si se pulsa enter, se envía el mensaje
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) {
            EnviarMensaje();
        }

        lock (candado)
        {
            if (!string.IsNullOrEmpty(mensajesRecibidos))
            {
                textoMensajes.text += mensajesRecibidos;
                mensajesRecibidos = "";

                // Fuerza el scroll al final para mostrar los mensajes recientes
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
            }
        }
    }

    // Método que establece la conexión con el servidor
    void ConectarConServidor()
    {
        try
        {
            // Instancio el cliente que se conectará al servidor con la IP y puerto definidos
            cliente = new TcpClient(ip, puerto);

            // Se obtiene el flujo de red para enviar y recibir datos
            flujo = cliente.GetStream();

            // Marco el cliente como activo
            clienteActivo = true;

            /* Se crea un hilo independiente que se encargará
             * de recibir los mensajes del servidor continuamente.
             * Así no se bloquea la ejecución principal de Unity. */
            hiloRecepcion = new Thread(RecibirDatosServidor);
            hiloRecepcion.IsBackground = true; // El hilo se ejecuta en segundo plano
            hiloRecepcion.Start();

            Debug.Log("Cliente conectado al servidor");
        }
        catch (Exception e)
        {
            // Captura cualquier error al intentar conectarse
            Debug.LogError("Error al conectar con el servidor: " + e.Message);
        }
    }

    // Método que se encarga de enviar los mensajes
    void EnviarMensaje()
    {
        if (!clienteActivo) return; // Si el cliente no está activo no se envía el mensaje

        // Se obtiene el texto del mensaje del input y se guarda en una variable
        string mensaje = inputMensaje.text;

        // Si el texto del input (mensaje) está vacío o solo contiene espacios, no se envía
        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        // Se convierte el mensaje a bytes (se agrega un salto de línea al final del mensaje)
        byte[] datos = Encoding.UTF8.GetBytes(mensaje + "\n");

        // Se intenta enviar el mensaje al servidor
        try
        {
            // Envía los datos al servidor
            flujo.Write(datos, 0, datos.Length);

            /* Guardo el mensaje enviado en mensajesRecibidos para mostrarlo 
             * en el campo de texto de mensajes, agregando un 'Tú:' delante 
             * para identificar los mensajes enviados por el usuario */
            lock (candado)
            {
                mensajesRecibidos += "Tú: " + mensaje + "\n"; 
            }

            // Limpia el texto del input
            inputMensaje.text = "";
        }
        catch (Exception e)
        {
            // Captura errores al enviar el mensaje
            Debug.LogError("Error al enviar el mensaje: " + e.Message);
        }
    }

    // Hilo independiente que se encarga de recibir mensajes del servidor
    void RecibirDatosServidor()
    {
        byte[] buffer = new byte[1024]; // Espacio para recibir datos

        // Bucle que se mantiene mientras el cliente esté activo
        while (clienteActivo)
        {
            // Intenta leer los mensajes del servidor
            try
            {
                // Lee los datos del flujo de red
                // (Llamada bloqueante, si no hay datos el código se queda aquí parado)
                // (La ejecución del hilo se detiene hasta obtener una respuesta)
                int bytesLeidos = flujo.Read(buffer, 0, buffer.Length);

                // DETECCIÓN DE DESCONEXIÓN:
                // Si Read devuelve 0, significa que el servidor cerró la conexión
                if (bytesLeidos == 0)
                {
                    clienteActivo = false; // Se marca el cliente como inactivo (deja de recibir datos)
                    break;
                }

                // Convierte los bytes leídos a texto legible
                string mensaje = Encoding.UTF8.GetString(buffer, 0, bytesLeidos);

                // Bloqueo para modificar el texto de mensajes recibidos de forma segura
                lock (candado)
                {
                    mensajesRecibidos += mensaje; // Se agrega el mensaje recibido al texto (se coloca debajo del anterior porque viene con salto de línea)
                }
            }
            catch
            {
                // Si ocurre cualquier error de lectura, se marca el cliente como inactivo (deja de recibir datos)
                clienteActivo = false;
            }
        }
    }

    // Al cerrar la aplicación
    void OnApplicationQuit()
    {
        // Se marca el cliente como inactivo (deja de recibir datos)
        clienteActivo = false;

        // Si el hilo de recepción sigue vivo, se cierra su ejecución (Abort)
        if (hiloRecepcion != null && hiloRecepcion.IsAlive)
            hiloRecepcion.Abort();

        // Cierra el flujo de datos
        if (flujo != null)
            flujo.Close();

        // Cierra el cliente TCP
        if (cliente != null)
            cliente.Close();
    }
}
