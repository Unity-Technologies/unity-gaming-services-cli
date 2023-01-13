using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;

namespace Unity.Services.Cli.CloudCode.Deploy;

class NoopDeploymentAnalytics : IDeploymentAnalytics
{
    sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
            // Do nothing as it is a No Op implementation
        }
    }

    public IDisposable Scope()
    {
        return new NoopDisposable();
    }

    public IDisposable BeginDeploySend(int fileSize)
    {
        return new NoopDisposable();
    }

    public void SendFailureDeploymentEvent(string exceptionType)
    {
        // Do nothing as it is a No Op implementation
    }

    public void SendSuccessfulPublishEvent()
    {
        // Do nothing as it is a No Op implementation
    }

    public void SendFailurePublishEvent(string exceptionType)
    {
        // Do nothing as it is a No Op implementation
    }
}
