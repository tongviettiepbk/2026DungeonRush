using System;

public static class Extensions
{
    public static double ToMiliseconds(this DateTime time)
    {
        DateTime beginDate = new DateTime(1970, 1, 1);
        TimeSpan ts = time - beginDate;
        return ts.TotalMilliseconds;
    }

    public static DateTime ToDateTime(this double miliseconds)
    {
        return new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(miliseconds);
    }
}
