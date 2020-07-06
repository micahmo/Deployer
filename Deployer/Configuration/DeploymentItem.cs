using System.Collections.Generic;

namespace Deployer
{
    public class DeploymentItem
    {
        public DeploymentItem(ConfigurationItem configurationItem, List<FileCopyPair> filesToCopy)
            => (ConfigurationItem, FilesToCopy) = (configurationItem, filesToCopy);

        public List<FileCopyPair> FilesToCopy { get; }

        public ConfigurationItem ConfigurationItem { get; }
    }
}
