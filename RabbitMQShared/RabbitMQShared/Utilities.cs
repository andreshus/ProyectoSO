using RabbitMQ.Shared.Models; 
using System.Text.Json;
using System.Text;

namespace RabbitMQ.Shared.Utilities
{
    public static class JsonUtil
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        public static byte[] Serialize<T>(T model)
        {
            var jsonString = JsonSerializer.Serialize(model, Options);
            return Encoding.UTF8.GetBytes(jsonString);
        }
        public static T? Deserialize<T>(byte[] bytes)
        {
            var jsonString = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(jsonString, Options);
        }
    }
}