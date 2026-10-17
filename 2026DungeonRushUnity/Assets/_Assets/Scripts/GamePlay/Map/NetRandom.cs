using System;

// System.Random bản Mono/.NET Framework (subtractive PRNG) — chép nguyên thuật toán để các phép
// random CÓ SEED của game gốc (shuffle preset quái, sinh chướng ngại rw.itr...) ra đúng y kết quả,
// không phụ thuộc runtime đang chạy.
public sealed class NetRandom
{
    private const int MBIG = int.MaxValue;
    private const int MSEED = 161803398;
    private readonly int[] seedArray = new int[56];
    private int inext, inextp;

    public NetRandom(int seed)
    {
        int subtraction = (seed == int.MinValue) ? int.MaxValue : Math.Abs(seed);
        int mj = MSEED - subtraction;
        seedArray[55] = mj;
        int mk = 1;
        for (int i = 1; i < 55; i++)
        {
            int ii = (21 * i) % 55;
            seedArray[ii] = mk;
            mk = mj - mk;
            if (mk < 0) mk += MBIG;
            mj = seedArray[ii];
        }
        for (int k = 1; k < 5; k++)
        {
            for (int i = 1; i < 56; i++)
            {
                seedArray[i] -= seedArray[1 + (i + 30) % 55];
                if (seedArray[i] < 0) seedArray[i] += MBIG;
            }
        }
        inext = 0;
        inextp = 21;
    }

    private int InternalSample()
    {
        int locINext = inext, locINextp = inextp;
        if (++locINext >= 56) locINext = 1;
        if (++locINextp >= 56) locINextp = 1;
        int retVal = seedArray[locINext] - seedArray[locINextp];
        if (retVal == MBIG) retVal--;
        if (retVal < 0) retVal += MBIG;
        seedArray[locINext] = retVal;
        inext = locINext;
        inextp = locINextp;
        return retVal;
    }

    public int Next(int minValue, int maxValue)
    {
        long range = (long)maxValue - minValue;
        return (int)((InternalSample() * (1.0 / MBIG)) * range) + minValue;
    }
}
