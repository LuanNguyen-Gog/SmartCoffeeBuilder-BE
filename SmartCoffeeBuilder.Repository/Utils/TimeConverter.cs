namespace SmartCoffeeBuilder.Repository.Utils;

public static class TimeConverter
{
    public static DateTimeOffset GetCurrentVietNamTime()
    {
        var createdAtUtc = DateTimeOffset.UtcNow;

        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var createdAtInVietnam = TimeZoneInfo.ConvertTime(createdAtUtc, vietnamTimeZone);

        return createdAtInVietnam;
    }

    public static DateTimeOffset ToVietNamTime(DateTimeOffset time)
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var timeAtVietNam = TimeZoneInfo.ConvertTime(time, vietnamTimeZone);

        return timeAtVietNam;
    }
}
