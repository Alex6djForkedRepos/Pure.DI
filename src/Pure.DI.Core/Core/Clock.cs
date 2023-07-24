// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class Clock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}