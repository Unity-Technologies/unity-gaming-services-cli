using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.Model;

public class Wait
{
    public static async Task<bool> ForConditionAsync(Func<bool> condition, TimeSpan timeout, TimeSpan pollInterval)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.Token.IsCancellationRequested)
        {
            if (condition())
            {
                return true; // Condition met
            }

            try
            {
                await Task.Delay(pollInterval, cts.Token); // Wait before checking again
            }
            catch (TaskCanceledException)
            {
                break; // Timeout reached
            }
        }

        return false; // Timeout occurred
    }
}
