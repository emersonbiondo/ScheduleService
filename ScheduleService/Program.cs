using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Newtonsoft.Json;
using ScheduleService;

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = configurationBuilder.Build();

var serviceName = configuration["Schedule:ServiceName"];

if (serviceName == null)
{
    serviceName = "Schedule Service";
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = serviceName;
});

LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddLogging(loggingBuilder => { 
    var loggingSection = builder.Configuration.GetSection("Logging");
    loggingBuilder.AddFile(loggingSection, fileLoggerOptions =>
    {
        fileLoggerOptions.FormatLogEntry = (msg) =>
        {
            var stringBuilder = new System.Text.StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var jsonTextWriter = new JsonTextWriter(stringWriter);

            jsonTextWriter.WriteStartArray();
            jsonTextWriter.WriteValue(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            jsonTextWriter.WriteValue(msg.LogLevel.ToString());
            jsonTextWriter.WriteValue(msg.LogName);
            jsonTextWriter.WriteValue(msg.Message);
            jsonTextWriter.WriteValue(msg.Exception?.ToString());
            jsonTextWriter.WriteEndArray();

            return stringBuilder.ToString();
        };
    });
});

builder.Services.AddSingleton(configuration);
builder.Services.AddSingleton<Service>();
builder.Services.AddHostedService<WindowsBackgroundService>();

IHost host = builder.Build();
host.Run();