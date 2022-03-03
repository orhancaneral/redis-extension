using DistributedCache.Redis;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ICache _cache;

        public ValuesController(ICache cache)
        {
            _cache = cache;
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            var key1 = await _cache.HashGetOrSetAsync("key1", "Movies", async () => { return await Task.FromResult("LordOfTheRings"); });
            await _cache.HashSetAsync("key2", "Albums", await GetData());
            await _cache.HashSetAsync("key3", "Persons", "test data");
            var key2 = await _cache.HashGetAsync<string>("key2", "Albums");
            var key3 = await _cache.HashExistsAsync("key3", "Persons");
            var key4 = await _cache.HashDeleteAsync("key3", "Persons");

            return new[] { $"HashGetOrSetAsync:{key1}", $"HashGetAsync:{key2}", $"HashExistsAsync:{key3}", $"HashDeleteAsync:{key4}" };
        }

        private async Task<string> GetData()
        {
            return await Task.FromResult("example");
        }
    }
}
