//using System;
//using System.Text;
//using RabbitMQ.Client;
//using System.Text.Json;
//using Microsoft.EntityFrameworkCore.Metadata;


//namespace Infrastructure.Messaging
//{
//    public class RabbitMqPublisher : IMessagePublisher, IDisposable
//    {
//        private readonly IConnection _connection;
//        private readonly IModel _channel;


//        public RabbitMqPublisher(string hostName, int port, string user, string pass)
//        {
//            var factory = new ConnectionFactory { HostName = hostName, Port = port, UserName = user, Password = pass };
//            _connection = factory.CreateConnection();
//            _channel = _connection.CreateModel();
//        }


//        public Task PublishAsync<T>(string exchange, string routingKey, T message)
//        {
//            var body = JsonSerializer.SerializeToUtf8Bytes(message);
//            _channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: null, body: body);
//            return Task.CompletedTask;
//        }


//        public void Dispose()
//        {
//            _channel?.Close();
//            _connection?.Close();
//        }
//    }
//}
