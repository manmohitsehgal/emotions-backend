namespace Emotions.Application.Pricing;

public sealed record WaitlistContext(
    bool Returning = false,
    int AttendanceStreak = 0,
    int RecentNoShows = 0,
    bool HostInvite = false);