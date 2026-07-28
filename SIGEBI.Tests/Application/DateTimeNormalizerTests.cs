using SIGEBI.Application.Common;

namespace SIGEBI.Tests.Application;

public sealed class DateTimeNormalizerTests
{
    [Fact]
    public void ToUtc_ConvierteFechaSinZonaEnUtc()
    {
        var localValue = new DateTime(2026, 7, 28, 16, 30, 0, DateTimeKind.Unspecified);

        var result = DateTimeNormalizer.ToUtc(localValue);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(
            DateTime.SpecifyKind(localValue, DateTimeKind.Local).ToUniversalTime(),
            result);
    }

    [Fact]
    public void ToUtc_MantieneFechaUtc()
    {
        var utcValue = new DateTime(2026, 7, 28, 20, 30, 0, DateTimeKind.Utc);

        var result = DateTimeNormalizer.ToUtc(utcValue);

        Assert.Equal(utcValue, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }
}
