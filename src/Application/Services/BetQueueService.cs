using Domain;
using System.Threading.Channels;

namespace Application.Services;

public class BetQueueService
{
    private readonly Channel<Bet> _channel;
    private volatile bool _completed;

    public BetQueueService(Microsoft.Extensions.Options.IOptions<WorkerOptions> options)
    {
        var opts = options.Value;
        var channelOptions = new BoundedChannelOptions(opts.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<Bet>(channelOptions);
    }

    public ChannelReader<Bet> Reader => _channel.Reader;

    /// <summary>
    /// Attempts to enqueue a bet into the channel asynchronously.
    /// </summary>
    /// <remarks>This method returns false if the operation cannot proceed because the
    /// channel is completed.</remarks>
    /// <param name="bet">The bet to enqueue. Cannot be null.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting to enqueue the bet.</param>
    /// <returns>true if the bet was successfully enqueued; otherwise, false/>.</returns>
    public async Task<bool> TryEnqueueAsync(Bet bet, CancellationToken ct)
    {
        if (_completed) return false;
        return await _channel.Writer.WaitToWriteAsync(ct) && _channel.Writer.TryWrite(bet);
    }

    /// <summary>
    /// Marks the operation as complete and signals the associated channel that no more data will be written.
    /// </summary>
    /// <remarks>This method sets the internal completion state and attempts to complete the writer for the
    /// associated channel.  Once called, no further data should be written to the channel.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task CompleteAsync()
    {
        _completed = true;
        _channel.Writer.TryComplete();
        await Task.CompletedTask;
    }
}