using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Business.DependencyResolvers.Autofac;
using Business.Mappings;
using Core.DependencyResolvers;
using Core.Extensions;
using Core.Utilities.Email;
using Core.Utilities.Exceptions;
using Core.Utilities.IoC;
using Core.Utilities.Logging;
using Core.Utilities.Security.Encryption;
using Core.Utilities.Security.JWT;
using DataAccess.Concrete.EntityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using WebApi.Filters;
using WebApi.Middleware;
using DataAccess.Concrete.EntityFramework.Interceptors;
using WebApi.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

public class Program
{
    private static async Task Main(string[] args)
    {
        var watch = Stopwatch.StartNew();

        // 1. Serilog Logger Başlangıç Ayarı
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // 2. Serilog Entegrasyonu
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console()
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

            // 3. Autofac Entegrasyonu
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterModule(new AutofacBusinessModule());
            });

            var services = builder.Services;
            var configuration = builder.Configuration;

            // 4. Veritabanı Yapılandırması (EF Core PostgreSQL)
            services.AddScoped<AuditInterceptor>();
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var connStr = configuration.GetConnectionString("DefaultConnection");
                if (connStr != null && connStr.Contains(".db", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseSqlite(connStr);
                }
                else
                {
                    options.UseNpgsql(connStr);
                }
                // Audit Interceptor otomatik tarih takibi için eklenir
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
                // Performans Optimizasyonu: Okuma işlemlerinde tracking kapalı
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            // 5. Temel Servislerin Eklenmesi
            services.AddAutoMapper(typeof(AutoMapperProfile));
            services.AddHttpClient();
            services.AddHttpContextAccessor();

            // 6. Özel Altyapı Servisleri
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddSingleton<ILoggerService, SerilogLoggerService>();

            // Redis Dağıtık Önbellekleme (İsteğe bağlı - Aktif etmek için aşağıdaki satırları yorumdan çıkarın)
            // services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis"));
            // services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp => 
            //     StackExchange.Redis.ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));

            // 7. Controller ve JSON Serileştirme Ayarları
            services.AddControllers(options =>
            {
                // Standart hata yapılandırması için Result Filtresi eklenir
                options.Filters.Add<UserFriendlyResultFilter>();
            })
            .AddJsonOptions(options =>
            {
                // Döngüsel referans hatasını önlemek için
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // 8. CORS Yapılandırması
            var corsOrigins = configuration["CorsOrigins"]?.Split(',') 
                              ?? new[] { "http://localhost:3000", "http://localhost:4200" };
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", policy =>
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // 9. JWT Kimlik Doğrulama (Authentication)
            var tokenOptions = configuration.GetSection("TokenOptions").Get<TokenOptions>()
                               ?? throw new InvalidOperationException("TokenOptions ayarları bulunamadı.");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = tokenOptions.Issuer,
                        ValidAudience = tokenOptions.Audience,
                        IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // 10. Rate Limiting Yapılandırması (DDoS & Brute Force Koruması)
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100, // 1 dakikada max 100 istek
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // Hassas endpoint'ler için özel limitler
                options.AddFixedWindowLimiter("login", opt => { opt.PermitLimit = 10; opt.Window = TimeSpan.FromMinutes(15); });
                options.AddFixedWindowLimiter("register", opt => { opt.PermitLimit = 5; opt.Window = TimeSpan.FromHours(1); });
                options.RejectionStatusCode = 429;
            });

            // 10.5. Health Checks (Sistem Sağlık Kontrolleri)
            services.AddHealthChecks()
                .AddCheck<DbContextHealthCheck>("Database");

            // 10.6. API Versiyonlama (API Versioning)
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true; // İstemciye desteklenen versiyonları header'da döner
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Swagger için grup adı formatı (örn: v1)
                options.SubstituteApiVersionInUrl = true;
            });

            // 11. Swagger ve API Dökümantasyonu
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Clean Architecture API Template", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header. Örnek: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // 11.5. Çoklu Dil Desteği (Localization)
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { "tr-TR", "en-US" };
                options.SetDefaultCulture(supportedCultures[0])
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures);
            });

            // 12. Core Katmanı Bağımlılık Modüllerini Yükle
            services.AddDependencyResolvers(new ICoreModule[] { new CoreModule() });

            // ─── UYGULAMA İNŞASI (BUILD) ───
            var app = builder.Build();

            // ⚡ ÖNEMLİ: Static Service Locator (ServiceTool) Başlatılması.
            // Bu satır olmadan constructor injection alamayan Aspect sınıfları (AOP) çalışmaz.
            ServiceTool.Create(app.Services);

            // ─── HTTP PIPELINE YAPILANDIRMASI (MIDDLEWARE) ───

            // Global Exception Yakalama (En başta olmalı)
            app.UseMiddleware<ExceptionMiddleware>();
            
            // Serilog Request Logging (İstek logları)
            app.UseSerilogRequestLogging();

            // Çoklu Dil (Localization) Middleware Aktifleştirme
            app.UseRequestLocalization();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API Template V1"));
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseStaticFiles();
            app.UseRouting();

            // Health check endpoint'ini özel JSON formatı ile eşle
            app.MapHealthChecks("/api/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description,
                            duration = entry.Value.Duration.ToString()
                        }),
                        totalDuration = report.TotalDuration.ToString()
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            });

            app.UseCors("AllowSpecificOrigins");
            app.UseRateLimiter();

            app.UseAuthentication();
            
            // Kullanıcı Ban/Askı Kontrol Middleware (Cache tabanlı)
            app.UseMiddleware<UserStatusMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            // Veritabanı otomatik migration ve seeding işlemleri
            using (var scope = app.Services.CreateScope())
            {
                var servicesProvider = scope.ServiceProvider;
                try
                {
                    var dbContext = servicesProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();
                    await DataAccess.Concrete.EntityFramework.Seed.DbSeeder.SeedAsync(dbContext);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Veritabanı migration veya seeding sırasında hata oluştu.");
                }
            }

            watch.Stop();
            Log.Information("🚀 Uygulama {ElapsedMilliseconds} ms içinde başarıyla başlatıldı.", watch.ElapsedMilliseconds);

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "💀 Uygulama beklenmeyen bir şekilde sonlandırıldı.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
