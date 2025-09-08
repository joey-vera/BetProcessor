using Domain.Enums;

namespace Domain;

/// <summary>
/// Represents a placed bet.
/// </summary>
public record Bet
{
    /// <summary>Unique ID of the bet.</summary>
    public int Id { get; init; }

    /// <summary>Amount bet.</summary>
    public double Amount { get; init; }

    /// <summary>Odds of the bet.</summary>
    public double Odds { get; init; }

    /// <summary>Client placing the bet.</summary>
    public string Client { get; init; } = string.Empty;

    /// <summary>Event being on the bet.</summary>
    public string Event { get; init; } = string.Empty;

    /// <summary>Market of the event.</summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>Selection associated with the bet.</summary>
    public string Selection { get; init; } = string.Empty;

    /// <summary>Status of the bet.</summary>
    public BetStatus Status { get; init; }
}