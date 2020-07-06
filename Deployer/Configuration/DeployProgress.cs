namespace Deployer
{
    public class DeployProgress
    {
        public DeployProgress(string currentStep, string details, double? percentComplete = null)
            => (CurrentStep, Details, PercentComplete) = (currentStep, details, percentComplete);

        public string CurrentStep { get; }

        public string Details { get; }

        public double? PercentComplete { get; }
    }
}