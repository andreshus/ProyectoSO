using RabbitMQ.Client;
using System;

namespace RabbitMQ.Shared.Services
{
    public class RabbitMqConnectionService : IDisposable
    {
        private IConnection? _connection = null;
        private IModel? _channel = null;

        public IModel? Channel => _channel;
        public IConnection? Connection => _connection;

        private readonly string _host;
        private readonly string _user;
        private readonly string _pass;
        // Se añade la variable para almacenar el puerto
        private readonly int _port;

        /// <summary>
        /// Constructor modificado para incluir el puerto, con 5672 como valor por defecto.
        /// </summary>
        public RabbitMqConnectionService(string host, string user, string pass, int port = 5672)
        {
            _host = host;
            _user = user;
            _pass = pass;
            // Se almacena el puerto
            _port = port;
        }

        public void Connect()
        {
            // Limpia cualquier conexión o canal anterior
            Dispose();

            var factory = new ConnectionFactory()
            {
                HostName = _host,
                // ¡Usa la variable _port para conectarse!
                Port = _port,
                UserName = _user ?? string.Empty,
                Password = _pass ?? string.Empty
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public void DeclareQueue(string queueName)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                throw new InvalidOperationException("El canal de RabbitMQ no está inicializado o está cerrado. Llama a Connect() primero.");
            }
            // Declaración de cola (durable: true)
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        public void Dispose()
        {
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
                _channel.Dispose();
                _channel = null;
            }
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}