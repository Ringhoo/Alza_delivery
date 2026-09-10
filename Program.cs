
using Alza_delivery.Databases.Contexts;
using Alza_delivery.Seeders;
using Alza_delivery.Services.Contracts;
using Alza_delivery.Services.Planning;
using Microsoft.EntityFrameworkCore;

namespace Alza_delivery
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            RegisterServices(builder);


            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            );

            var app = builder.Build();

            if (await HandleSeed(app, args))
            {
                return;
            }
            

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IPlanningService, PlanningService>();
            builder.Services.AddScoped<WarehouseSeeder>();
            builder.Services.AddScoped<VehicleSeeder>();
            builder.Services.AddScoped<PackageSeeder>();
            builder.Services.AddScoped<DatabaseSeeder>();
        }

        private static async Task<bool> HandleSeed(WebApplication app, string[] args)
        {
            if (!args.Contains("seed", StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;

            try
            {
                await SeedDatabaseAsync(app, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }

            return true;
        }

        private static async Task SeedDatabaseAsync(
            WebApplication app,
            CancellationToken cancellationToken)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var databaseSeeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

            await databaseSeeder.SeedAsync(cancellationToken);
        }
    }
}
