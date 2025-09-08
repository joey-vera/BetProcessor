namespace Domain.Enums;

/// <summary>
/// Possible states of a bet.
/// </summary>
public enum BetStatus
{
    /// <summary>Bet is placed and open.</summary>
    OPEN = 0,
    /// <summary>Bet settled as a winner.</summary>
    WINNER = 1,
    /// <summary>Bet settled as a loser.</summary>
    LOSER = 2,
    /// <summary>Bet voided and refunded.</summary>
    VOID = 3
}