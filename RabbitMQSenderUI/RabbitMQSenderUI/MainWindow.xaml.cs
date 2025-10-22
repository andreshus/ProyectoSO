using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;

// Referencias a tu librería compartida
using RabbitMQ.Shared.Models;
using RabbitMQ.Shared.Utilities;
using RabbitMQ.Shared.Services;
using RabbitMQ.Client;

namespace RabbitMQSenderWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string message)
        {
            // Dispatcher.Invoke es la forma segura de actualizar la UI en WPF
            Dispatcher.Invoke(() =>
            {
                // Asegúrate que lbLog existe en tu XAML
                if (lbLog != null)
                {
                    lbLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                    lbLog.ScrollIntoView(lbLog.Items[lbLog.Items.Count - 1]);
                }
            });
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtener y validar la configuración de la UI
            string host = txtHost.Text;
            string user = txtUser.Text;
            string pass = txtPass.Text;

            // Verificación del campo de cantidad
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                Log("Error: La cantidad debe ser un número positivo.");
                return;
            }

            string messageBody = txtMessageBody.Text;
            string targetQueue = rbCola1.IsChecked == true ? "Cola1" : "Cola2";

            // Asegúrate que btnSend existe en tu XAML
            btnSend.IsEnabled = false;

            // 2. Iniciar la operación en un hilo de trabajo (Task)
            Task.Run(() => SendMessages(host, user, pass, targetQueue, quantity, messageBody));
        }

        private void SendMessages(string host, string user, string pass, string targetQueue, int quantity, string messageBody)
        {
            Log($"Iniciando envío de {quantity} mensajes a '{targetQueue}'...");

            // Usamos la clase de servicio compartido y la declaración 'using'
            using (var service = new RabbitMqConnectionService(host, user, pass))
            {
                try
                {
                    service.Connect();
                    Log("Conexión exitosa y canal creado.");

                    service.DeclareQueue(targetQueue);
                    Log($"Cola '{targetQueue}' declarada.");

                    // Bucle de Envío
                    for (int i = 1; i <= quantity; i++)
                    {
                        // 1. Crear el objeto MessageModel (desde la librería Shared)
                        var messageObject = new MessageModel
                        {
                            IdMensaje = i,
                            OrigenProceso = "Sender_UI (WPF)",
                            TimestampEnvio = DateTime.UtcNow,
                            PayloadBase = messageBody.Trim(),
                            ReplicaNum = null,
                            DistintivoHilo = null
                        };

                        // 2. Serializar el objeto a bytes (desde la librería Shared)
                        var body = JsonUtil.Serialize(messageObject);

                        // 3. Publicar el mensaje (el operador !) es para asegurar al compilador que Channel no es null aquí)
                        service.Channel!.BasicPublish(
                            exchange: string.Empty,
                            routingKey: targetQueue,
                            basicProperties: null,
                            body: body
                        );

                        if (i % 100 == 0) Log($"Enviados {i} / {quantity} mensajes.");
                    }

                    Log($"--- ¡Envío Completo! {quantity} mensajes enviados a '{targetQueue}'. ---");
                }
                catch (Exception ex)
                {
                    Log($"ERROR CRÍTICO: No se pudo conectar o enviar. Mensaje: {ex.Message}");
                }
                finally
                {
                    // Re-habilitar el botón en el hilo de la UI
                    Dispatcher.Invoke(() => btnSend.IsEnabled = true);
                }
            }
        }
    }
}