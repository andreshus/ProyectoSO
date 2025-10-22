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
        private const int AmqpPort = 5672;

        public RabbitMqConnectionService(string host, string user, string pass)
        {
            _host = host;
            _user = user;
            _pass = pass;
        }

        public void Connect()
        {
            Dispose();

            var factory = new ConnectionFactory()
            {
                HostName = _host,
                Port = AmqpPort,
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