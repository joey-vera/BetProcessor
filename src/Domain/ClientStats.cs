using System.Runtime.CompilerServices;

namespace Domain;

public class ClientStats
{
    private double _totalProfit;
    private double _totalLoss;

    public double TotalProfit => ReadDouble(ref _totalProfit);
    public double TotalLoss => ReadDouble(ref _totalLoss);

    public void AddProfit(double amount) => AddDouble(ref _totalProfit, amount);
    public void AddLoss(double amount) => AddDouble(ref _totalLoss, amount);

    private static void AddDouble(ref double target, double value)
    {
        Interlocked.Add(ref Unsafe.As<double, long>(ref target), BitConverter.DoubleToInt64Bits(value));
    }

    private static double ReadDouble(ref double target)
    {
        return BitConverter.Int64BitsToDouble(Interlocked.Read(ref Unsafe.As<double, long>(ref target)));
    }
}