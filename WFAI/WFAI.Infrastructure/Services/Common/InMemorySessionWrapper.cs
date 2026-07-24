
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Common
{
    public class InMemorySessionWrapper : ISessionWrapper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public InMemorySessionWrapper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public T GetFromSession<T>(string key)
        {
            var value = _httpContextAccessor?.HttpContext?.Session?.GetString(key);

            if (string.IsNullOrWhiteSpace(value))
                return default!;

            return JsonSerializer.Deserialize<T>(value, _jsonOptions)!;
        }

        public void RemoveFromSession(string key)
        {
            _httpContextAccessor?.HttpContext?.Session?.Remove(key);
        }

        public void SetInSession<T>(string key, T value)
        {
            if (value is null)
                return;

            var json = JsonSerializer.Serialize(value, _jsonOptions);
            _httpContextAccessor?.HttpContext?.Session?.SetString(key, json);
        }
    }
}