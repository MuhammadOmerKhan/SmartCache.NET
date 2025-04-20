using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace SmartCache.NET
{

    public class Executor
    {

        #region Singlton 
        private static readonly Executor _singleton = new Executor();
        public static Executor Instance
        {
            get { return (_singleton); }
        }
        protected Executor()
        {
        }
        #endregion

        private static ConcurrentDictionary<string, object> _cacheLocks = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<string, int> _asyncFlags = new ConcurrentDictionary<string, int>();


        /// <summary>
        /// Get Data will execute the code block and if caching is enabled it will cache for the duration specified or for 30 Second if not specified.
        /// </summary>
        /// <typeparam name="T">Code block return type</typeparam>
        /// <param name="codeBlock"></param>
        /// <param name="paramWithValues">Parameteres will values that will change the record set in the database, will be used in Cache key generation.</param>
        /// <param name="cacheDuration">0 Second will disable caching for any method, default is 30 seconds.</param>
        /// <returns></returns>
        public T GetData<T>(Func<T> codeBlock, dynamic paramWithValues, int cacheDuration = 30)
        {
            ///TODO: Should we use CallerInfo attributes, which is not reflection based but Compile time attributes ?  
            if (Executor.UseCaching && cacheDuration > 0) //Cacheing is enabled ?
            {
                var method = codeBlock.Method;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}.{1}:-", method.ReflectedType.FullName, method.Name).Append(Convert.ToString(paramWithValues));
                string cacheKey = sb.ToString();
                if (DataCache.ContainsKey(cacheKey))
                {
                    return DataCache.GetData<T>(cacheKey);
                }
                try
                {
                    T result = codeBlock();

                    if (result != null)
                    {
                        DataCache.SetData(cacheKey, result, cacheDuration);
                    }

                    return (result);
                }
                catch (Exception)
                {
                    throw;
                    //return default;
                }
            }
            else
            {
                return codeBlock();
            }
        }

        public async Task<T> GetDataAsyncOld<T>(Func<T> codeBlock, dynamic paramWithValues, int cacheDuration = 30)
        {
            if (Executor.UseCaching) //Cacheing is enabled ?
            {
                var method = codeBlock.Method;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}.{1}:-", method.ReflectedType.FullName, method.Name).Append(Convert.ToString(paramWithValues));
                string cacheKey = sb.ToString();

                if (DataCache.ContainsKey(cacheKey))
                {
                    return await DataCache.GetDataAsync<T>(cacheKey);
                }

                try
                {
                    T result;
                    result = await Task.Run(() => {
                        return codeBlock();
                    });
                    if (result == null)
                    {
                        return result;
                    }
                    await DataCache.SetDataAsync(cacheKey, result, cacheDuration);
                    return (result);
                }
                catch (Exception)
                {
                    return default;
                }
            }
            else
            {
                return await Task.Run(() => {
                    return codeBlock();
                });
            }
        }

        public async Task<T> GetDataAsync<T>(Func<T> codeBlock, dynamic paramWithValues, int cacheDuration = 30, int asyncRefreshAfterSecs = 120)
        {
            var method = codeBlock.Method;
            string cacheKey = $"{method.ReflectedType.FullName}.{method.Name}:-{paramWithValues}";
            string asyncKey = $"{cacheKey}_ASYNC";
            Console.WriteLine("Cache Key:" + cacheKey);
            if (!UseCaching || cacheDuration <= 0)
                return await Task.Run(codeBlock);

            object lockObj = _cacheLocks.GetOrAdd(cacheKey, new object());

            // If cache exists, return it and optionally trigger async refresh
            if (DataCache.ContainsKey(cacheKey))
            {
                T cachedData = await DataCache.GetDataAsync<T>(cacheKey);

                // If async refresh is due
                if (!DataCache.ContainsKey(asyncKey) && _asyncFlags.TryGetValue(cacheKey, out int flag) && flag == 0)
                {
                    _asyncFlags[cacheKey] = 1;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            T freshData = codeBlock();
                            if (freshData != null)
                            {
                                await DataCache.SetDataAsync(cacheKey, freshData, cacheDuration);
                                await DataCache.SetDataAsync(asyncKey, "1", asyncRefreshAfterSecs);
                                Console.WriteLine("Fresh Data");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error
                        }
                        finally
                        {
                            _asyncFlags[cacheKey] = 0;
                        }
                    });
                }

                return cachedData;
            }

            // Critical section — ensure only one thread populates the cache
            lock (lockObj)
            {
                if (DataCache.ContainsKey(cacheKey))
                {
                    return DataCache.GetData<T>(cacheKey);
                }

                T result = codeBlock();
                if (result != null)
                {
                    DataCache.SetData(cacheKey, result, cacheDuration);
                    DataCache.SetData(asyncKey, "1", asyncRefreshAfterSecs);
                    _asyncFlags[cacheKey] = 0;
                }

                return result;
            }
        }
        public async Task<T> GetAutoDataAsync<T>(Func<T> codeBlock, dynamic paramWithValues, int cacheDuration = 30)
        {
            var method = codeBlock.Method;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}.{1}:-", method.ReflectedType.FullName, method.Name)
              .Append(Convert.ToString(paramWithValues));
            string cacheKey = sb.ToString();

            ObjectCache cache = MemoryCache.Default;
            if (cache.Contains(cacheKey))
            {
                return (T)cache.Get(cacheKey);
            }

            T data = await Task.Run(() => codeBlock());
            if (data != null)
            {
                CacheEntryRemovedCallback refreshCallback = null;
                refreshCallback = arguments =>
                {
                    try
                    {
                        T refreshedData = codeBlock();
                        if (refreshedData != null)
                        {
                            CacheItemPolicy newPolicy = new CacheItemPolicy
                            {
                                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(cacheDuration),
                                RemovedCallback = refreshCallback  // use the captured callback
                            };
                            cache.Set(cacheKey, refreshedData, newPolicy);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Will need to do something about
                    }
                };

                CacheItemPolicy policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(cacheDuration),
                    RemovedCallback = refreshCallback
                };

                cache.Add(cacheKey, data, policy);
            }
            return data;
        }

        #region Propertheees
        private static bool UseCaching
        {
            get
            {
                return true;//bool.Parse(AppConfigs.AppSettingsJson.GetSection("AppSettings:UseCaching").Value);
            }
        }
        private static IDataCache DataCache
        {
            get { return InMemoryDataCache.Instance; }
        }

        #endregion
    }
    public interface IDataCache
    {
        bool ContainsKey(string key);
        T GetData<T>(string key);
        void SetData<T>(string key, T data, int cacheDuration);
        Task<T> GetDataAsync<T>(string key);
        Task SetDataAsync<T>(string key, T data, int cacheDuration);
    }
    public class InMemoryDataCache : IDataCache
    {
        private static readonly InMemoryDataCache _instance = new InMemoryDataCache();
        public static InMemoryDataCache Instance => _instance;

        private readonly ConcurrentDictionary<string, (object value, DateTime expiry)> _cache = new();

        public bool ContainsKey(string key)
        {
            if (_cache.TryGetValue(key, out var val))
            {
                if (DateTime.UtcNow < val.expiry)
                    return true;

                _cache.TryRemove(key, out _);
            }
            return false;
        }

        public T GetData<T>(string key)
        {
            return (T)_cache[key].value;
        }

        public void SetData<T>(string key, T data, int cacheDuration)
        {
            _cache[key] = (data, DateTime.UtcNow.AddSeconds(cacheDuration));
        }

        public async Task<T> GetDataAsync<T>(string key)
        {
            return await Task.FromResult(GetData<T>(key));
        }

        public async Task SetDataAsync<T>(string key, T data, int cacheDuration)
        {
            SetData(key, data, cacheDuration);
            await Task.CompletedTask;
        }
    }
}
