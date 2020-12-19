using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace Deployer
{
    public class KnownValueConverters : Dictionary<Type, IValueConverter>
    {
        public static KnownValueConverters Instance { get; } = new KnownValueConverters
        {
            {typeof(Enum), new EnumDescriptionConverter()},
            {typeof(GuidInfo), new GuidInfoConverter()}
        };
    }
}
