using Castle.DynamicProxy;
using Core.Utilities.IoC;
using Core.Utilities.Security.UserContext;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Aspects.Autofac.Caching
{
    /// <summary>
    /// Metod çağrılarına göre benzersiz cache key üreten yardımcı sınıf.
    /// Parametre tiplerini ve değerlerini hash'leyerek çakışmaları önler.
    /// </summary>
    internal static class CacheKeyBuilder
    {
        private static readonly JsonSerializerOptions KeySerializerOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };

        /// <summary>
        /// Metodun adını, parametrelerini ve kullanıcı bağlamını birleştirerek benzersiz bir cache key üretir.
        /// </summary>
        public static string BuildCacheKey(IInvocation invocation)
        {
            var prefix = BuildMethodPrefix(invocation);
            var arguments = invocation.Arguments;

            if (arguments == null || arguments.Length == 0)
            {
                return prefix;
            }

            var parameterInfos = invocation.Method.GetParameters();
            var keyBuilder = new StringBuilder(prefix);

            for (var i = 0; i < arguments.Length; i++)
            {
                var parameterName = parameterInfos.Length > i ? parameterInfos[i].Name : $"arg{i}";
                keyBuilder.Append('|')
                          .Append(parameterName)
                          .Append(':')
                          .Append(HashArgument(arguments[i]));
            }

            // Kullanıcıya özel cache alanı oluşturmak için UserId'yi anahtara ekle
            var contextKey = BuildUserContextKey();
            if (!string.IsNullOrWhiteSpace(contextKey))
            {
                keyBuilder.Append("|ctx:")
                          .Append(contextKey);
            }

            return keyBuilder.ToString();
        }

        public static string BuildMethodPrefix(IInvocation invocation)
        {
            var typeName = invocation.Method.ReflectedType?.FullName
                           ?? invocation.Method.DeclaringType?.FullName
                           ?? invocation.TargetType?.FullName;

            return typeName != null
                ? $"{typeName}.{invocation.Method.Name}"
                : invocation.Method.Name;
        }

        private static string? BuildUserContextKey()
        {
            try
            {
                var serviceProvider = ServiceTool.ServiceProvider;
                if (serviceProvider == null) return null;

                var userContext = serviceProvider.GetService<IUserContextService>();
                if (userContext == null) return null;

                // Kullanıcı kimliği varsa cache key'e ekle
                var userId = userContext.GetUserId();
                return $"uid:{userId}";
            }
            catch
            {
                // Kullanıcı giriş yapmamışsa veya context yoksa null döner (global cache)
                return null;
            }
        }

        private static string HashArgument(object? argument)
        {
            var normalized = SerializeArgument(argument);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hashBytes);
        }

        private static string SerializeArgument(object? argument)
        {
            if (argument == null) return "null";

            switch (argument)
            {
                case string str:
                    return $"string:{str}";
                case DateTime dateTime:
                    return $"datetime:{dateTime:O}";
                case DateTimeOffset dateTimeOffset:
                    return $"datetimeoffset:{dateTimeOffset:O}";
                case Guid guid:
                    return $"guid:{guid}";
                case Enum enumValue:
                    return $"enum:{Convert.ToInt64(enumValue)}";
            }

            var type = argument.GetType();

            if (type.IsPrimitive || argument is decimal)
            {
                return $"{type.FullName}:{argument}";
            }

            if (argument is IEnumerable enumerable)
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                {
                    items.Add(SerializeArgument(item));
                }
                return $"{type.FullName}:[{string.Join(",", items)}]";
            }

            try
            {
                var json = JsonSerializer.Serialize(argument, KeySerializerOptions);
                return $"{type.FullName}:{json}";
            }
            catch
            {
                return $"{type.FullName}:{argument}";
            }
        }
    }
}
