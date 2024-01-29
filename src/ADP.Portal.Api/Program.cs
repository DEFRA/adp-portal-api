
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Mapster;
using ADP.Portal.Api.Providers;
using ADP.Portal.Core.Ado.Infrastructure;
using ADP.Portal.Core.Ado.Services;
using Microsoft.Extensions.Options;
using Mapster;

namespace ADP.Portal.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLogging();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.Configure<AdoConfig>(builder.Configuration.GetSection("Ado"));
            builder.Services.Configure<AdpAdoProjectConfig>(builder.Configuration.GetSection("AdpAdoProject"));
            builder.Services.AddScoped(async provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var keyVaultName = config["KeyVaultName"]?? string.Empty;

                var adoAzureAdConfig = provider.GetRequiredService<IOptions<AdoConfig>>().Value;
                var vssConnectionProvider = new VssConnectionProvider(keyVaultName, adoAzureAdConfig);
                var connection = await vssConnectionProvider.GetConnectionAsync();
                return connection;
            });
            builder.Services.AddScoped<IAdoProjectService, AdoProjectService>();
            builder.Services.AddScoped<IAdoService, AdoService>();
            builder.Services.EntitiesConfigure();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            if (app.Environment.IsProduction())
            {
                app.UseExceptionHandler();
            }
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
