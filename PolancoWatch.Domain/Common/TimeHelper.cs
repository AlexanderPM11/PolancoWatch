using System;

namespace PolancoWatch.Domain.Common;

public static class TimeHelper
{
    // Dominican Republic is always UTC-4 (Atlantic Standard Time)
    // No Daylight Saving Time
    private const int DR_OFFSET = -4;

    public static DateTime Now => DateTime.UtcNow.AddHours(DR_OFFSET);
}
