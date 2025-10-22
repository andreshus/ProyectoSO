using System;
using System.Text.Json.Serialization;

namespace RabbitMQ.Shared.Models
{
    public class MessageModel
    {
        [JsonPropertyName("id_mensaje")]
        public int IdMensaje { get; set; }

        [JsonPropertyName("timestamp_envio")]
        public DateTime TimestampEnvio { get; set; }

        [JsonPropertyName("timestamp_procesamiento")]
        public DateTime? TimestampProcesamiento { get; set; }

        public string? OrigenProceso { get; set; }

        [JsonPropertyName("payload_base")]
        public string? PayloadBase { get; set; }

        [JsonPropertyName("replica_num")]
        public int? ReplicaNum { get; set; }

        [JsonPropertyName("distintivo_hilo")]
        public string? DistintivoHilo { get; set; }
    }
}