using System.Text.Json;

namespace DistributedCache.Redis
{
    public class RedisOptions
    {
        public string Configuration { get; set; }

        public string Namespace { get; set; }

        public int DatabaseNumber { get; set; }

        public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions { IgnoreNullValues = true };
    }

    public class RedisConfiguration
    {
        public string HostAndPorts { get; set; }
        public string Password { get; set; }
        public bool AllowAdmin { get; set; }
        public int ConnectRetry { get; set; }
        public int ConnectTimeout { get; set; }
        public int KeepAlive { get; set; }
        public int SyncTimeout { get; set; }
        public bool IsCluster { get; set; }
    }
}
