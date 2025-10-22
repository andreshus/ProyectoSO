using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using RabbitMQ.Shared.Models;
using RabbitMQ.Shared.Utilities;
using RabbitMQ.Shared.Services;

namespace RabbitMQReceiverWPF
{
    public partial class MainWindow : Window
    {
        private RabbitMqConnectionService? _connectionService;
        private const string Cola1 = "Cola1";
        private const string Cola2 = "Cola2";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(ListBox logBox, string message, Brush? color = null)
        {
            Dispatcher.Invoke(() =>
            {
                var item = new ListBoxItem
                {
                    Content = $"[{DateTime.Now:HH:mm:ss}] {message}",
                    Foreground = color ?? Brushes.Black
                };
                logBox.Items.Add(item);
                logBox.ScrollIntoView(item);
                if (lblStatus != null)
                {
                    lblStatus.Content = message;
                }
            });
        }

        private void btnStartReceivers_Click(object sender, RoutedEventArgs e)
        {
            string host = txtHostReceiver.Text;
            if (string.IsNullOrEmpty(host))
            {
                Log(lbLogHilo1, "Error: Debe ingresar la IP del Host.");
                return;
            }

            try
            {
                _connectionService?.Dispose();
                _connectionService = new RabbitMqConnectionService(host, "proyecto", "admin123");
                _connectionService.Connect();

                _connectionService.DeclareQueue(Cola1);
                _connectionService.DeclareQueue(Cola2);

                Log(lbLogHilo1, "Conexión establecida. Iniciando hilos de consumo...");

                Task.Run(() => ProcesadorCola1_to_Cola2());
                Task.Run(() => ConsumidorFinalCola2());

                btnStartReceivers.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Log(lbLogHilo1, $"Error al conectar o declarar: {ex.Message}", Brushes.Red);
                if (lblStatus != null) lblStatus.Content = "FALLÓ LA CONEXIÓN.";
            }
        }

        private void ProcesadorCola1_to_Cola2()
        {
            using (var channel = _connectionService!.Connection!.CreateModel())
            {
                channel.QueueDeclare(queue: Cola1, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var receivedModel = JsonUtil.Deserialize(body);

                    Log(lbLogHilo1, $"[CONSUMO C1] Recibido ID: {receivedModel.IdMensaje}. Generando réplicas...", Brushes.DarkRed);

                    for (int i = 1; i <= 8; i++)
                    {
                        var replicaModel = new MessageModel
                        {
                            IdMensaje = receivedModel.IdMensaje,
                            OrigenProceso = "Generado_Hilo_1",
                            TimestampEnvio = DateTime.UtcNow,
                            PayloadBase = receivedModel.PayloadBase,
                            ReplicaNum = i,
                            DistintivoHilo = "CREADO_DESDE_COLA1"
                        };

                        var bodyNuevo = JsonUtil.Serialize(replicaModel);
                        channel.BasicPublish(exchange: string.Empty, routingKey: Cola2, basicProperties: null, body: bodyNuevo);
                        Log(lbLogHilo1, $">>>>> [GENERADO A C2] Réplica #{i}", Brushes.Orange);
                    }

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                Log(lbLogHilo1, $"Hilo 1 esperando mensajes en '{Cola1}'...");
                channel.BasicConsume(queue: Cola1, autoAck: false, consumer: consumer);

                Thread.Sleep(Timeout.Infinite);
            }
        }

        private void ConsumidorFinalCola2()
        {
            using (var channel = _connectionService!.Connection!.CreateModel())
            {
                channel.QueueDeclare(queue: Cola2, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var receivedModel = JsonUtil.Deserialize(body);

                    receivedModel.TimestampProcesamiento = DateTime.UtcNow;
                    TimeSpan latencia = receivedModel.TimestampProcesamiento.Value - receivedModel.TimestampEnvio;
                    string latenciaStr = $"Latencia: {latencia.TotalMilliseconds:N2} ms";

                    var isGenerated = receivedModel.DistintivoHilo == "CREADO_DESDE_COLA1";
                    var color = isGenerated ? Brushes.DarkGreen : Brushes.Blue;
                    var source = isGenerated ? "GENERADO POR HILO 1" : "ORIGINAL SENDER";

                    string payloadContent = receivedModel.PayloadBase ?? "[Payload Vacío]";

                    string displayMsg = $"[PROCESADO C2] {source} | ID: {receivedModel.IdMensaje} | {latenciaStr} | Payload: {payloadContent}";

                    Log(lbLogHilo2, displayMsg, color);

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                Log(lbLogHilo2, $"Hilo 2 esperando mensajes en '{Cola2}' (Consumidor Final)...");
                channel.BasicConsume(queue: Cola2, autoAck: false, consumer: consumer);

                Thread.Sleep(Timeout.Infinite);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _connectionService?.Dispose();
            base.OnClosed(e);
        }
    }
}