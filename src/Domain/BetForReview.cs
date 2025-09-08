using Domain.Enums;

namespace Domain;

public record BetForReview(int Id, BetStatus ReceivedStatus, string Reason, DateTimeOffset AtUtc);