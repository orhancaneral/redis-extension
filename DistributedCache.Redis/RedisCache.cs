using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DistributedCache.Redis
{
    public class RedisCache : ICache
    {
        #region Fields

        private static RedisOptions _redisOptions;
        private readonly IDatabase _database;
        private ConnectionMultiplexer _connection;

        private static readonly object Lock = new object();

        public RedisCache(RedisOptions redisOptions)
        {
            _redisOptions = redisOptions;
            var redisConfiguration = JsonSerializer.Deserialize<RedisConfiguration>(_redisOptions.Configuration, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var options = GetOptions(redisConfiguration);

            var lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                if (_connection != null && _connection.IsConnected)
                {
                    return _connection;
                }

                lock (Lock)
                {
                    if (_connection != null && _connection.IsConnected)
                    {
                        return _connection;
                    }

                    _connection?.Dispose();
                    _connection = ConnectionMultiplexer.Connect(options);
                    return _connection;
                }
            });

            if (_connection == null || !_connection.IsConnected)
            {
                _connection = lazyConnection.Value;
            }

            _database = redisConfiguration.IsCluster ? _connection.GetDatabase() : _connection.GetDatabase(_redisOptions.DatabaseNumber);
        }

        #endregion

        #region Hash Methods

        public async Task<T> HashGetOrSetAsync<T>(string key, string hashField, Func<Task<T>> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var result = await HashGetAsync<T>(key, hashField, serializerOptions);
            if (result != null)
            {
                return result;
            }

            var lazyValue = new Lazy<Task<T>>(value);
            var newValue = await lazyValue.Value;
            await HashSetAsync(key, hashField, newValue, expirationMinutes, serializerOptions);
            return newValue;
        }

        public async Task<T> HashGetAsync<T>(string key, string hashField, JsonSerializerOptions serializerOptions = null)
        {
            var item = await _database.HashGetAsync(AddNamespaceToKey(key), hashField);

            return item.IsNullOrEmpty ? default(T) : JsonSerializer.Deserialize<T>(item, serializerOptions ?? _redisOptions.SerializerOptions);
        }

        public async Task<bool> HashSetAsync<T>(string key, string hashField, T value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var stringValue = JsonSerializer.Serialize(value, serializerOptions ?? _redisOptions.SerializerOptions);
            if (string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            var isSuccess = await _database.HashSetAsync(AddNamespaceToKey(key), hashField, stringValue);
            if (isSuccess)
            {
                await _database.KeyExpireAsync(AddNamespaceToKey(key), TimeSpan.FromMinutes(expirationMinutes));
            }

            return isSuccess;
        }

        public async Task<bool> HashDeleteAsync(string key, string hashField)
        {
            return await _database.HashDeleteAsync(AddNamespaceToKey(key), hashField);
        }

        public async Task<bool> HashExistsAsync(string key, string hashField)
        {
            return await _database.HashExistsAsync(AddNamespaceToKey(key), hashField);
        }

        public T HashGetOrSet<T>(string key, string hashField, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var result = HashGet(key, hashField, value, serializerOptions);
            if (result != null)
            {
                return result;
            }

            var newValue = new Lazy<T>(value);
            HashSet(key, hashField, value, expirationMinutes, serializerOptions);
            return newValue.Value;
        }

        public T HashGet<T>(string key, string hashField, Func<T> value, JsonSerializerOptions serializerOptions = null)
        {
            var item = _database.HashGet(AddNamespaceToKey(key), hashField);
            return item.IsNullOrEmpty ? default(T) : JsonSerializer.Deserialize<T>(item, serializerOptions ?? _redisOptions.SerializerOptions);
        }

        public bool HashSet<T>(string key, string hashField, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var stringValue = JsonSerializer.Serialize(value, serializerOptions ?? _redisOptions.SerializerOptions);
            if (string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            var isSuccess = _database.HashSet(AddNamespaceToKey(key), hashField, stringValue);
            if (isSuccess)
            {
                _database.KeyExpire(AddNamespaceToKey(key), TimeSpan.FromMinutes(expirationMinutes));
            }

            return isSuccess;
        }

        public bool HashDelete(string key, string hashField)
        {
            return _database.HashDelete(key, hashField);
        }

        public bool HashExists(string key, string hashField)
        {
            return _database.HashExists(key, hashField);
        }

        #endregion

        #region String Key Methods

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var result = await GetAsync<T>(key, serializerOptions);
            if (result != null)
            {
                return result;
            }

            var lazyValue = new Lazy<Task<T>>(value);
            var newValue = await lazyValue.Value;
            await SetAsync(key, newValue, expirationMinutes, serializerOptions);
            return newValue;
        }

        public async Task<T> GetAsync<T>(string key, JsonSerializerOptions serializerOptions = null)
        {
            var item = await _database.StringGetAsync(AddNamespaceToKey(key));
            return item.IsNullOrEmpty ? default(T) : JsonSerializer.Deserialize<T>(item, serializerOptions ?? _redisOptions.SerializerOptions);
        }

        public async Task<bool> SetAsync<T>(string key, T value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var stringValue = JsonSerializer.Serialize(value, serializerOptions ?? _redisOptions.SerializerOptions);
            if (string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            return await _database.StringSetAsync(AddNamespaceToKey(key), stringValue, TimeSpan.FromMinutes(expirationMinutes));
        }

        public async Task<bool> DeleteAsync(string key)
        {
            return await _database.KeyDeleteAsync(AddNamespaceToKey(key));
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(AddNamespaceToKey(key));
        }

        public T GetOrSet<T>(string key, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var result = Get<T>(key, serializerOptions);
            if (result != null)
            {
                return result;
            }

            var newValue = new Lazy<T>(value);
            Set(key, value, expirationMinutes, serializerOptions);
            return newValue.Value;
        }

        public T Get<T>(string key, JsonSerializerOptions serializerOptions = null)
        {
            var item = _database.StringGet(AddNamespaceToKey(key));
            return item.IsNullOrEmpty ? default(T) : JsonSerializer.Deserialize<T>(item, serializerOptions ?? _redisOptions.SerializerOptions);
        }

        public bool Set<T>(string key, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null)
        {
            var stringValue = JsonSerializer.Serialize(value, serializerOptions ?? _redisOptions.SerializerOptions);
            if (string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            return _database.StringSet(AddNamespaceToKey(key), stringValue, TimeSpan.FromMinutes(expirationMinutes));
        }

        public bool Delete(string key)
        {
            return _database.KeyDelete(AddNamespaceToKey(key));
        }

        public bool Exists(string key)
        {
            return _database.KeyExists(AddNamespaceToKey(key));
        }

        #endregion

        #region Private Methods

        private string AddNamespaceToKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Value cannot be null or empty", key);
            }

            return $"{_redisOptions.Namespace}:{key}";
        }

        private ConfigurationOptions GetOptions(RedisConfiguration configuration)
        {
            var options = new ConfigurationOptions
            {
                Password = configuration.Password,
                AllowAdmin = configuration.AllowAdmin,
                ConnectRetry = configuration.ConnectRetry,
                ConnectTimeout = configuration.ConnectTimeout,
                KeepAlive = configuration.KeepAlive,
                SyncTimeout = configuration.SyncTimeout,
                ConfigCheckSeconds = 10
            };

            if (configuration.IsCluster)
            {
                options.CommandMap = CommandMap.Create(new Dictionary<string, string> { { "$CLUSTER", "cluster" } });
            }

            foreach (var hostAndPort in configuration.HostAndPorts.Split(','))
                options.EndPoints.Add(hostAndPort.Trim());

            return options;
        }

        #endregion
    }
}
