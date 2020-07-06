#region Usings

using System.Collections.Generic;

#endregion

namespace Deployer
{
    public class SettingsGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IList<Setting> Settings { get; } = new List<Setting>();
    }
}
