using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// Asumo que estas referencias son correctas
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

        private IModel? _channelHilo1;
        private IModel? _channelHilo2;

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

            if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                Log(lbLogHilo1, "Error: El puerto debe ser un número válido (1-65535).", Brushes.Red);
                return;
            }

            string user = txtUserReceiver.Text;
            string pass = txtPassReceiver.Text;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Log(lbLogHilo1, "Error: Debe ingresar Host, Usuario y Contraseña.", Brushes.Red);
                return;
            }

            try
            {
                // Limpiar recursos anteriores para una reconexión limpia
                _channelHilo1?.Dispose();
                _channelHilo2?.Dispose();
                _connectionService?.Dispose();

                // 1. Conectar y declarar colas
                _connectionService = new RabbitMqConnectionService(host, user, pass, port);
                _connectionService.Connect();

                _connectionService.DeclareQueue(Cola1);
                _connectionService.DeclareQueue(Cola2);

                Log(lbLogHilo1, $"Conexión establecida como '{user}' en puerto {port}. Iniciando hilos de consumo...", Brushes.Green);

                // 2. Iniciar los dos procesos (hilos) en paralelo
                Task.Run(() => ProcesadorCola1_to_Cola2());
                Task.Run(() => ConsumidorFinalCola2());

                btnStartReceivers.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Log(lbLogHilo1, $"Error al conectar o declarar: {ex.Message} (Host: {host})", Brushes.Red);
                if (lblStatus != null) lblStatus.Content = "FALLÓ LA CONEXIÓN.";
            }
        }

        private async Task ProcesadorCola1_to_Cola2()
        {
            try
            {
                _channelHilo1 = _connectionService!.Connection!.CreateModel();

                // Manejo de eventos de canal cerrado para diagnóstico
                _channelHilo1.CallbackException += (sender, args) => Log(lbLogHilo1, $"Error en Callback de Hilo 1: {args.Exception.Message}", Brushes.OrangeRed);
                _channelHilo1.ModelShutdown += (sender, args) => Log(lbLogHilo1, $"Canal Hilo 1 cerrado. Motivo: {args.Cause}", Brushes.Red);

                _channelHilo1.QueueDeclare(queue: Cola1, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(_channelHilo1);

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();

                    // SOLUCIÓN AL ERROR CS0308: Casting explícito
                    var receivedModel = (MessageModel)JsonUtil.Deserialize(body);

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

                        _channelHilo1.BasicPublish(exchange: string.Empty, routingKey: Cola2, basicProperties: null, body: bodyNuevo);
                        Log(lbLogHilo1, $">>>>> [GENERADO A C2] Réplica #{i}", Brushes.Orange);
                    }

                    _channelHilo1.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                Log(lbLogHilo1, $"Hilo 1 esperando mensajes en '{Cola1}'...");
                _channelHilo1.BasicConsume(queue: Cola1, autoAck: false, consumer: consumer);

                await Task.Delay(Timeout.Infinite, default);
            }
            catch (OperationCanceledException)
            {
                // Se espera si la aplicación cierra la tarea.
            }
            catch (Exception ex)
            {
                Log(lbLogHilo1, $"Hilo 1 falló inesperadamente: {ex.Message}", Brushes.Red);
            }
        }

        private async Task ConsumidorFinalCola2()
        {
            try
            {
                _channelHilo2 = _connectionService!.Connection!.CreateModel();

                // Manejo de eventos de canal cerrado para diagnóstico
                _channelHilo2.CallbackException += (sender, args) => Log(lbLogHilo2, $"Error en Callback de Hilo 2: {args.Exception.Message}", Brushes.OrangeRed);
                _channelHilo2.ModelShutdown += (sender, args) => Log(lbLogHilo2, $"Canal Hilo 2 cerrado. Motivo: {args.Cause}", Brushes.Red);

                _channelHilo2.QueueDeclare(queue: Cola2, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(_channelHilo2);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();

                    // SOLUCIÓN AL ERROR CS0308: Casting explícito
                    var receivedModel = (MessageModel)JsonUtil.Deserialize(body);

                    receivedModel.TimestampProcesamiento = DateTime.UtcNow;
                    TimeSpan latencia = receivedModel.TimestampProcesamiento.Value - receivedModel.TimestampEnvio;
                    string latenciaStr = $"Latencia: {latencia.TotalMilliseconds:N2} ms";

                    var isGenerated = receivedModel.DistintivoHilo == "CREADO_DESDE_COLA1";
                    var color = isGenerated ? Brushes.DarkGreen : Brushes.Blue;
                    var source = isGenerated ? "GENERADO POR HILO 1" : "ORIGINAL SENDER";

                    string payloadContent = receivedModel.PayloadBase ?? "[Payload Vacío]";

                    string displayMsg = $"[PROCESADO C2] {source} | ID: {receivedModel.IdMensaje} | {latenciaStr} | Payload: {payloadContent}";

                    Log(lbLogHilo2, displayMsg, color);

                    _channelHilo2.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                Log(lbLogHilo2, $"Hilo 2 esperando mensajes en '{Cola2}' (Consumidor Final)...");
                _channelHilo2.BasicConsume(queue: Cola2, autoAck: false, consumer: consumer);

                await Task.Delay(Timeout.Infinite, default);
            }
            catch (OperationCanceledException)
            {
                // Se espera si la aplicación cierra la tarea.
            }
            catch (Exception ex)
            {
                Log(lbLogHilo2, $"Hilo 2 falló inesperadamente: {ex.Message}", Brushes.Red);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _channelHilo1?.Dispose();
            _channelHilo2?.Dispose();
            _connectionService?.Dispose();
            base.OnClosed(e);
        }
    }
}