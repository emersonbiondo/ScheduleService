using System.Data.SqlClient;
using System.Diagnostics;

namespace ScheduleService
{
    public class Service
    {
        private readonly ILogger<WindowsBackgroundService> logger;

        public Service(ILogger<WindowsBackgroundService> logger) {
            this.logger = logger;
        }

        public async Task Execute(List<ItemCommand> itemCommands)
        {
            foreach (var itemCommand in itemCommands.OrderBy(x => x.OrderId)) {
                try
                {
                    switch (itemCommand.Type)
                    {
                        case "Console":
                            await ConsoleExecute(itemCommand);
                            break;
                        case "HttpGet":
                            await HttpGetExecute(itemCommand);
                            break;
                        case "SqlServerCommand":
                            await SqlServerCommandExecute(itemCommand);
                            break;
                        default:
                            logger.LogWarning("ItemCommand {0}, Type not defined", itemCommand.OrderId);
                            await Task.Delay(1);
                            break;
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error ItemCommand {0}", itemCommand.OrderId);
                }
            }
        }

        public async Task ConsoleExecute(ItemCommand itemCommand)
        {
            Process process = new Process();
            process.StartInfo.FileName = itemCommand.Parameter1;
            
            process.StartInfo.UseShellExecute = itemCommand.UseShellExecute;

            if (itemCommand.CreateNoWindow)
            {
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
            }

            process.StartInfo.Arguments = itemCommand.Parameter2;

            process.Start();

            if (itemCommand.WaitForExit)
            {
                process.WaitForExit();
            }            

            await Task.Delay(itemCommand.Delay);

            logger.LogInformation("ItemCommand {0}, Console run successfully", itemCommand.OrderId);
        }

        public async Task HttpGetExecute(ItemCommand itemCommand)
        {
            using HttpClient client = new();
            var result = await client.GetStringAsync(itemCommand.Parameter1);

            await Task.Delay(itemCommand.Delay);

            logger.LogInformation("ItemCommand {0}, HttpGet run successfully, result: {1}", itemCommand.OrderId, result);
        }

        public async Task SqlServerCommandExecute(ItemCommand itemCommand)
        {
            var result = false;

            using (SqlConnection connection = new SqlConnection(itemCommand.Parameter1))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(itemCommand.Parameter2, connection))
                {
                    var rows = command.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        result = true;
                    }
                }
            }

            await Task.Delay(itemCommand.Delay);

            logger.LogInformation("ItemCommand {0}, SQL Command run successfully, result: {1}", itemCommand.OrderId, result);
        }
    }
}
