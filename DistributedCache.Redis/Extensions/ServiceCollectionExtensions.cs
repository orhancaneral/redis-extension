using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DistributedCache.Redis.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRedisCache(this IServiceCollection services, string environmentName, int databaseNumber = 0)
        {
            var redisOptions = new RedisOptions();
            RedisConfiguration config = new RedisConfiguration()
            {
                AllowAdmin = true,
                ConnectRetry = 3,
                ConnectTimeout = 10000,
                IsCluster = false,
                HostAndPorts = "localhost:6379"
            };
            redisOptions.Configuration = JsonSerializer.Serialize(config);
            redisOptions.Namespace = string.Concat(environmentName.ToLower(), ".ares.com");
            redisOptions.DatabaseNumber = databaseNumber;
            if (string.IsNullOrEmpty(redisOptions.Configuration))
            {
                throw new ArgumentException("Value cannot be null or empty.", redisOptions.Configuration);
            }

            services.AddSingleton<ICache>(so => new RedisCache(redisOptions));
        }

        public static void AddRedisCache(this IServiceCollection services, Action<RedisOptions> options)
        {
            var redisOptions = new RedisOptions();
            options(redisOptions);

            if (string.IsNullOrEmpty(redisOptions.Configuration))
            {
                throw new ArgumentException("Value cannot be null or empty.", redisOptions.Configuration);
            }

            services.AddSingleton<ICache>(so => new RedisCache(redisOptions));
        }
    }
}
