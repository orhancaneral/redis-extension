using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DistributedCache.Redis
{
    public interface ICache
    {
        Task<T> HashGetOrSetAsync<T>(string key, string hashField, Func<Task<T>> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        Task<T> HashGetAsync<T>(string key, string hashField, JsonSerializerOptions serializerOptions = null);
        Task<bool> HashSetAsync<T>(string key, string hashField, T value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        Task<bool> HashDeleteAsync(string key, string hashField);
        Task<bool> HashExistsAsync(string key, string hashField);
        T HashGetOrSet<T>(string key, string hashField, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        T HashGet<T>(string key, string hashField, Func<T> value, JsonSerializerOptions serializerOptions = null);
        bool HashSet<T>(string key, string hashField, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        bool HashDelete(string key, string hashField);
        bool HashExists(string key, string hashField);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        Task<T> GetAsync<T>(string key, JsonSerializerOptions serializerOptions = null);
        Task<bool> SetAsync<T>(string key, T value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
        T GetOrSet<T>(string key, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        T Get<T>(string key, JsonSerializerOptions serializerOptions = null);
        bool Set<T>(string key, Func<T> value, int expirationMinutes = 60, JsonSerializerOptions serializerOptions = null);
        bool Delete(string key);
        bool Exists(string key);
    }
}
