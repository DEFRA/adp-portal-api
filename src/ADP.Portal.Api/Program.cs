
using ADP.Portal.Api.Config;
using ADP.Portal.Api.Providers;
using ADP.Portal.Core.AdoProject;
using ADP.Portal.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ADP.Portal.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLogging();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.Configure<AdoConfig>(builder.Configuration.GetSection("Ado"));
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

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseExceptionHandler();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
