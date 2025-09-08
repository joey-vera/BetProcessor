namespace Domain;

public record WorkerOptions
{
    public int WorkerCount { get; set; }
    public int ChannelCapacity { get; set; }
    public int ProcessingDelayMs { get; set; }
}