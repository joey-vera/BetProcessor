using Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Application.Services;

public class WorkerService : BackgroundService
{
    private readonly BetQueueService _queueService;
    private readonly IBetProcessorService _processorService;
    private readonly ILogger<WorkerService> _logger;
    private readonly int _workerCount;

    public WorkerService(BetQueueService queueService, IBetProcessorService processorService, ILogger<WorkerService> logger, Microsoft.Extensions.Options.IOptions<WorkerOptions> options)
    {
        _queueService = queueService;
        _processorService = processorService;
        _logger = logger;
        _workerCount = Math.Max(1, options.Value.WorkerCount);
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(_workerCount);
        for (int i = 0; i < _workerCount; i++)
            tasks.Add(RunWorkerLoop(i, cancellationToken));
        return Task.WhenAll(tasks);
    }

    private async Task RunWorkerLoop(int workerId, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var bet in _queueService.Reader.ReadAllAsync(cancellationToken))
                await _processorService.ProcessAsync(bet, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker {WorkerId} canceled.", workerId);
        }
        catch (ChannelClosedException)
        {
            _logger.LogInformation("Worker {WorkerId} channel closed.", workerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker {WorkerId} error.", workerId);
        }
        finally
        {
            // Signal idle if channel is closed and no bets are being processed
            _processorService.ForceIdle();
        }
    }
}