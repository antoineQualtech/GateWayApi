
namespace ProxyApiQualtech.Services.BackGroundServices
{
    public class ServiceWorker : BackgroundService
    {
        public ILogger Logger { get; }
        public ServiceWorker(ILoggerFactory loggerFactory) {
            Logger = loggerFactory.CreateLogger<ServiceWorker>();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Serviceworker est lancé");

            stoppingToken.Register(() => Logger.LogInformation("Serviceworker est entrain d'arrêté"));

            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("Serviceworker travaille");

                await Task.Delay(TimeSpan.FromSeconds(1000000), stoppingToken);
            }

            Logger.LogInformation("Serviceworker est arrêté");
        }
    }
}
