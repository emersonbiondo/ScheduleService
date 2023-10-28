using NCrontab;
using static NCrontab.CrontabSchedule;

namespace ScheduleService
{
    public class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger<WindowsBackgroundService> logger;
        private List<ItemCommand>? itemCommands;
        private bool useCrontab = true;
        private CrontabSchedule crontabSchedule;
        private Service service;

        public WindowsBackgroundService(Service service, ILogger<WindowsBackgroundService> logger, IConfiguration configuration)
        {
            itemCommands = configuration.GetSection("ItemCommands").Get<List<ItemCommand>>();

            bool.TryParse(configuration["Schedule:UseCrontab"], out useCrontab);

            var crontab = configuration["Schedule:Crontab"];

            if (crontab == null)
            {
                crontab = "*/10 * * * * *";
            }

            crontabSchedule = CrontabSchedule.Parse(crontab, new ParseOptions(){ IncludingSeconds = true });
            this.service = service;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (useCrontab)
                    {
                        var now = DateTime.Now;
                        DateTime nextExecutionTime = crontabSchedule.GetNextOccurrence(now);
                        var between = nextExecutionTime - now;
                        logger.LogInformation("Waiting {0} seconds...", (int)between.TotalSeconds);
                        await Task.Delay((int)between.TotalMilliseconds, stoppingToken);
                    }

                    logger.LogInformation("Running...");
                    await service.Execute(itemCommands != null ? itemCommands : new List<ItemCommand>());
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error Execute");
                Environment.Exit(1);
            }
        }
    }
}