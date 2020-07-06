using System;

namespace Deployer
{
    public class DeployError
    {
        public DeployError(string details, Exception exception)
            => (Details, Exception) = (details, exception);

        public string Details { get; }

        public Exception Exception { get; }
    }
}
