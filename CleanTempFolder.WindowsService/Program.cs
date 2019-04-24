using Autofac;
using DeleteTempFiles.WindowsService.Services.Quartz;
using DeleteTempFiles.WindowsService.Services.Helpers;
using DeleteTempFiles.WindowsService.Services;
using Quartz;
using Serilog;
using System;
using Topshelf.Autofac;
using Topshelf.Quartz;
using Topshelf;
using Serilog.Core;

namespace DeleteTempFiles.WindowsService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Read App.config settings
            var serviceName = ConfigHelper.GetSetting<string>("ServiceName");
            var serviceDisplayName = ConfigHelper.GetSetting<string>("ServiceDisplayName");
            var serviceDescription = ConfigHelper.GetSetting<string>("ServiceDescription");
            var intervalInHours = ConfigHelper.GetSetting("IntervalInHours", 36);
            var daysAgo = ConfigHelper.GetSetting("DaysAgo", 1);

            // Setup Serilog
            var loggerConfig = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            //.WriteTo.File($"logs/CleanTempFolder.txt", rollingInterval: RollingInterval.Day)
                            .WriteTo.ColoredConsole();
            Logger logger = loggerConfig.CreateLogger();
            Log.Logger = logger;

            // Create IOC container
            var builder = new ContainerBuilder();
            builder.RegisterInstance(logger).As<ILogger>();
            builder.RegisterType<CleanTempFolderService>();
            var container = builder.Build();

            // Run Service Host
            var serviceRunner = HostFactory.Run(config =>
            {
                config.UseSerilog();
                config.UseAutofacContainer(container);

                config.Service<CleanTempFolderService>(service =>
                {
                    // Let Topshelf use it
                    service.ConstructUsingAutofacContainer();
                    service.WhenContinued((tc, hostControl) => tc.Continue(hostControl));
                    service.WhenPaused((tc, hostControl) => tc.Pause(hostControl));
                    service.WhenStarted((tc, hostControl) => tc.Start(hostControl));
                    service.WhenStopped((tc, hostControl) => tc.Stop(hostControl));

                    // Schedule Job
                    var jobData = new JobDataMap
                    {
                        { "DaysAgo", daysAgo }
                    };
                    service.ScheduleQuartzJob(q => q
                    .WithJob(() => JobBuilder.Create<CleanTempFolderJob>()
                    .WithIdentity("CleanTempFolderJob", "group1")
                    .SetJobData(jobData)
                    .Build())
                    .AddTrigger(() => TriggerBuilder.Create()
                    .WithIdentity("CleanTempFolderJobTrigger", "group1")
                    .WithSimpleSchedule(b => b
                        .WithIntervalInHours(intervalInHours)
                        //.WithIntervalInSeconds(10) // Debug Mode
                        .RepeatForever())
                    .Build()));
                });

                config.SetServiceName(serviceName);
                config.SetDisplayName(serviceDisplayName);
                config.SetDescription(serviceDescription);
                config.RunAsLocalSystem();
                config.DependsOnEventLog();
                config.StartAutomatically();
                config.StartAutomaticallyDelayed();
                config.EnableServiceRecovery(r =>
                {
                    r.RestartService(3);
                    r.OnCrashOnly();
                    r.SetResetPeriod(2);
                });
                config.OnException((exception) =>
                {
                    Log.Error($"Exception thrown - {exception.Message}");
                });
            });

            Environment.ExitCode = (int)Convert.ChangeType(serviceRunner, serviceRunner.GetTypeCode());
        }
    }
}
