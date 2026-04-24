using System.Threading;

namespace OrderApi.Services;

public interface IOrderNumberGenerator
{
    int Next();
}

public class OrderNumberGenerator : IOrderNumberGenerator
{
    private int _currentValue;

    public int Next()
    {
        return Interlocked.Increment(ref _currentValue);
    }
}
