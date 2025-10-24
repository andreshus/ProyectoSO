using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media; // Necesario para usar Brushes en Log

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

        private void Log(string message, Brush? color = null)
        {
            Dispatcher.Invoke(() =>
            {
                if (lbLog != null)
                {
                    var item = new ListBoxItem
                    {
                        Content = $"[{DateTime.Now:HH:mm:ss}] {message}",
                        Foreground = color ?? Brushes.Black // Soporte para colores en el Log
                    };
                    lbLog.Items.Add(item);
                    lbLog.ScrollIntoView(item);
                }
            });
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtener y validar la configuración de la UI
            string host = txtHost.Text;
            string user = txtUser.Text;
            string pass = txtPass.Text;

            // >>>>>>>>>>>>>>>>> CÓDIGO AÑADIDO/CORREGIDO PARA EL PUERTO <<<<<<<<<<<<<<<<<
            if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                Log("Error: El puerto debe ser un número válido (1-65535).", Brushes.Red);
                return;
            }

            // Verificación del campo de cantidad
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                Log("Error: La cantidad debe ser un número positivo.", Brushes.Red);
                return;
            }

            string messageBody = txtMessageBody.Text;
            string targetQueue = rbCola1.IsChecked == true ? "Cola1" : "Cola2";

            btnSend.IsEnabled = false;

            // 2. Iniciar la operación en un hilo de trabajo (Task)
            // Se pasa el puerto como nuevo argumento:
            Task.Run(() => SendMessages(host, user, pass, port, targetQueue, quantity, messageBody));
        }

        // >>>>>>>>>>>>>>>>> CÓDIGO CORREGIDO PARA ACEPTAR EL PUERTO <<<<<<<<<<<<<<<<<
        private void SendMessages(string host, string user, string pass, int port, string targetQueue, int quantity, string messageBody)
        {
            Log($"Iniciando envío de {quantity} mensajes a '{targetQueue}' en puerto {port}...", Brushes.Orange);

            // Usamos el servicio compartido y la declaración 'using'
            // Se pasa el puerto al constructor:
            using (var service = new RabbitMqConnectionService(host, user, pass, port))
            {
                try
                {
                    service.Connect();
                    Log("Conexión exitosa y canal creado.", Brushes.Green);

                    service.DeclareQueue(targetQueue);
                    Log($"Cola '{targetQueue}' declarada.");

                    // Bucle de Envío
                    for (int i = 1; i <= quantity; i++)
                    {
                        var messageObject = new MessageModel
                        {
                            IdMensaje = i,
                            OrigenProceso = "Sender_UI (WPF)",
                            TimestampEnvio = DateTime.UtcNow,
                            PayloadBase = messageBody.Trim(),
                            ReplicaNum = null,
                            DistintivoHilo = null
                        };

                        var body = JsonUtil.Serialize(messageObject);

                        service.Channel!.BasicPublish(
                            exchange: string.Empty,
                            routingKey: targetQueue,
                            basicProperties: null,
                            body: body
                        );

                        if (i % 100 == 0) Log($"Enviados {i} / {quantity} mensajes.");
                    }

                    Log($"--- ¡Envío Completo! {quantity} mensajes enviados a '{targetQueue}'. ---", Brushes.DarkGreen);
                }
                catch (Exception ex)
                {
                    Log($"ERROR CRÍTICO: No se pudo conectar o enviar. Mensaje: {ex.Message}", Brushes.Red);
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