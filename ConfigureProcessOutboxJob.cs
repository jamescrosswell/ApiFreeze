using Microsoft.Extensions.Options;
using Quartz;

namespace ApiFreeze;

internal sealed class ConfigureProcessOutboxJob() : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobName = typeof(ProcessOutboxJob).FullName!;

        options
            .AddJob<ProcessOutboxJob>(configure => configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    // Below is the solution I found to get things working
                    // .StartAt(DateTimeOffset.UtcNow.AddSeconds(15))
                    .WithSimpleSchedule(schedule =>
                        schedule.WithIntervalInSeconds(1).RepeatForever()));
    }
}

[DisallowConcurrentExecution]
public class ProcessOutboxJob(ILogger<ProcessOutboxJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Beginning to process outbox messages");
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        logger.LogDebug("Finished processing outbox messages");
    }
}