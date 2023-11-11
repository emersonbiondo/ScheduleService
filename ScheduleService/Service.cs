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
            if (itemCommands.Count == 0)
            {
                logger.LogWarning("ItemCommands is Empty");
            }

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
            var processStartInfo = new ProcessStartInfo(itemCommand.Parameter1, itemCommand.Parameter2)
            {
                CreateNoWindow = itemCommand.CreateNoWindow,
                UseShellExecute = itemCommand.UseShellExecute,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = itemCommand.Parameter3
            };

            var output = new List<string>();

            var process = Process.Start(processStartInfo);

            if (process != null)
            {
                process.Start();

                if (itemCommand.WaitForExit)
                {
                    process.OutputDataReceived += (sender, args1) => {
                        if (args1.Data != null)
                        {
                            output.Add(args1.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.WaitForExit();
                }

                await Task.Delay(itemCommand.Delay);

                logger.LogInformation("ItemCommand {0}, Console run successfully", itemCommand.OrderId);

                logger.LogInformation("ItemCommand {0}, result: {1}", itemCommand.OrderId, string.Join(Environment.NewLine, output));
            }
        }

        public async Task HttpGetExecute(ItemCommand itemCommand)
        {
            using HttpClient client = new();
            var result = await client.GetStringAsync(itemCommand.Parameter1);

            var output = new List<string>(result
                .Replace("\r", string.Empty)
                .Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));

            await Task.Delay(itemCommand.Delay);

            logger.LogInformation("ItemCommand {0}, HttpGet run successfully", itemCommand.OrderId);

            logger.LogInformation("ItemCommand {0}, result: {1}", itemCommand.OrderId, result);
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

            logger.LogInformation("ItemCommand {0}, SQL Command run successfully", itemCommand.OrderId);

            logger.LogInformation("ItemCommand {0}, result: {1}", itemCommand.OrderId, result);
        }
    }
}
