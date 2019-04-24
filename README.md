# Delete Temporary Files older than x-days Demo

The idea is to build Windows service which recursive search all temporary files, check their file timestamps and only delete those that are older than x-days.
For those purpose I've developed Topshelf application which could be run as both a console application and/or as a Windows service. The background process is controlled with powerful Quartz.NET job Scheduler, logging was done by Serilog and for IOC I've used Autofac.

## Topshelf
[Topshelf](https://github.com/Topshelf/Topshelf) is a framework that makes it easy to develop Windows Services written in .NET. It gives the ability to run it as a console application while developing it and easily deploy it as a service. Setting up Topshelf is straight forward - all you need is a console application (targetting the .Net Framework) and add reference to Topshelf Nuget package. To set up the service, you need to modify the Program.cs with some setup code for creating the windows services and setting service metadata.
Thanks to TopShelf, a developer working on a Windows Service can focus solely on building business logic instead of complex configuration service.

## Autofac
With Topshelf setup, we have a running windows service application. To add in dependency injection so that you do not have to wire up all your dependencies manually, you can use [Autofac](https://github.com/autofac/Autofac). The **Topshelf.Autofac** library helps integrate Topshelf and Autofac DI container. Autofac can be easily integrated with Topshelf using the library and passing in the container instance to the **UseAutofacContainer** extension method on **HostConfigurator**.

## Quartz.NET
[Quartz.NET](https://github.com/quartznet/quartznet) is a fully functional framework for creating jobs in time. It is written from scratch in .NET based on a popular framework written in Java – Quartz.
The SchedulerService will now be instantiated using the Autofac container and makes it easy to inject dependencies into it. We need to be able to schedule jobs within the SchedulerService hence inject an IScheduler from Quartz.Net. You can add a reference to Quartz Nuget package, and you are all set to run jobs on schedule. To integrate Quartz with Autofac so that job dependencies can also be injected in via the container we need to use [Autofac.Extras.Quartz](https://www.nuget.org/packages/Autofac.Extras.Quartz/) Nuget package.
A great help is also [Topshelf.Quartz](https://www.nuget.org/packages/Topshelf.Quartz/) Nuget which provides extensions to schedule Quartz jobs.

## Logging
By default Topshelf will log using TraceSource, but if you want to use one of more advanced logging libraries such as [Serilog](https://github.com/serilog/serilog), NLog or log4net. Then thankfully Topshelf allows us to easily integrate these.
I'm going to use [Serilog](https://github.com/serilog/serilog). So from Nuget find a version of [Topshelf.Serilog](https://www.nuget.org/packages/Topshelf.Serilog/) compatible with the version of Topshelf you’re using and add this package to your project.

## Wiring it Up
Below is Program.cs with Main entry point:

```csharp
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
```

If you run this application from Visual Studio or from the command prompt, you'll see the application output text on each elapsed tick. So we can now use our Windows service as a standalone console application and debug it easily like this. If we want to use the application in a Windows service scenario we can simply run a command prompt as administrator and then use any/all of the following to install the service (once installed it will be visible in services.msc), start the service manually, stop it and uninstall it, like this:

```csharp
CleanTempFolder.WindowsService.exe install
CleanTempFolder.WindowsService.exe start
CleanTempFolder.WindowsService.exe stop
CleanTempFolder.WindowsService.exe uninstall
```

## Summary
Topshelf and Quartz.NET allows us to rapidly build Windows services to execute scheduled jobs in .NET. As shown in this demo, developers don't need to understand the complexity of setting up Windows services and installing them on the machine via InstallUtil.

## Prerequisites
- [Visual Studio](https://www.visualstudio.com/vs/community) 2017 15.9 or greater

## Tags & Technologies
- [Topshelf](https://github.com/Topshelf/Topshelf)
- [Quartz.NET](https://github.com/quartznet/quartznet)
- [Autofac](https://github.com/autofac/Autofac)
- [Serilog](https://github.com/serilog/serilog)

Enjoy!

## Licence

Licenced under [MIT](http://opensource.org/licenses/mit-license.php).
Contact me on [LinkedIn](https://si.linkedin.com/in/matjazbravc)