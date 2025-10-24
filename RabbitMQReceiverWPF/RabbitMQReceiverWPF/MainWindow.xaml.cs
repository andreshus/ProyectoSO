using System;
<<<<<<< HEAD
=======
using System.Text;
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

<<<<<<< HEAD
// Asumo que estas referencias son correctas
=======
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
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

<<<<<<< HEAD
        private IModel? _channelHilo1;
        private IModel? _channelHilo2;

=======
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
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
<<<<<<< HEAD

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
=======
            if (string.IsNullOrEmpty(host))
            {
                Log(lbLogHilo1, "Error: Debe ingresar la IP del Host.");
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
                return;
            }

            try
            {
<<<<<<< HEAD
                // Limpiar recursos anteriores para una reconexión limpia
                _channelHilo1?.Dispose();
                _channelHilo2?.Dispose();
                _connectionService?.Dispose();

                // 1. Conectar y declarar colas
                _connectionService = new RabbitMqConnectionService(host, user, pass, port);
=======
                _connectionService?.Dispose();
                _connectionService = new RabbitMqConnectionService(host, "proyecto", "admin123");
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
                _connectionService.Connect();

                _connectionService.DeclareQueue(Cola1);
                _connectionService.DeclareQueue(Cola2);

<<<<<<< HEAD
                Log(lbLogHilo1, $"Conexión establecida como '{user}' en puerto {port}. Iniciando hilos de consumo...", Brushes.Green);

                // 2. Iniciar los dos procesos (hilos) en paralelo
=======
                Log(lbLogHilo1, "Conexión establecida. Iniciando hilos de consumo...");

>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
                Task.Run(() => ProcesadorCola1_to_Cola2());
                Task.Run(() => ConsumidorFinalCola2());

                btnStartReceivers.IsEnabled = false;
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                Log(lbLogHilo1, $"Error al conectar o declarar: {ex.Message} (Host: {host})", Brushes.Red);
=======
                Log(lbLogHilo1, $"Error al conectar o declarar: {ex.Message}", Brushes.Red);
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
                if (lblStatus != null) lblStatus.Content = "FALLÓ LA CONEXIÓN.";
            }
        }

<<<<<<< HEAD
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
=======
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
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959

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
<<<<<<< HEAD

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
=======
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
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959

                    receivedModel.TimestampProcesamiento = DateTime.UtcNow;
                    TimeSpan latencia = receivedModel.TimestampProcesamiento.Value - receivedModel.TimestampEnvio;
                    string latenciaStr = $"Latencia: {latencia.TotalMilliseconds:N2} ms";

                    var isGenerated = receivedModel.DistintivoHilo == "CREADO_DESDE_COLA1";
                    var color = isGenerated ? Brushes.DarkGreen : Brushes.Blue;
                    var source = isGenerated ? "GENERADO POR HILO 1" : "ORIGINAL SENDER";

                    string payloadContent = receivedModel.PayloadBase ?? "[Payload Vacío]";

                    string displayMsg = $"[PROCESADO C2] {source} | ID: {receivedModel.IdMensaje} | {latenciaStr} | Payload: {payloadContent}";

                    Log(lbLogHilo2, displayMsg, color);

<<<<<<< HEAD
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
=======
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                Log(lbLogHilo2, $"Hilo 2 esperando mensajes en '{Cola2}' (Consumidor Final)...");
                channel.BasicConsume(queue: Cola2, autoAck: false, consumer: consumer);

                Thread.Sleep(Timeout.Infinite);
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
            }
        }

        protected override void OnClosed(EventArgs e)
        {
<<<<<<< HEAD
            _channelHilo1?.Dispose();
            _channelHilo2?.Dispose();
=======
>>>>>>> 79d5d94d23df44bffe193f76d0c44c1986573959
            _connectionService?.Dispose();
            base.OnClosed(e);
        }
    }
}