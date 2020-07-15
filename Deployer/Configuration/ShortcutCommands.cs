using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Deployer
{
    public static class ShortcutCommands
    {
        #region Commands

        static ShortcutCommands()
        {
            ReloadCurrentConfigurationCommand.InputGestures.Add(new KeyGesture(Key.F5));
            HardReloadCurrentConfigurationCommand.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
            DeployCurrentConfigurationCommand.InputGestures.Add(new KeyGesture(Key.F6));
        }

        public static RoutedCommand ReloadCurrentConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand HardReloadCurrentConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand DeployCurrentConfigurationCommand { get; } = new RoutedCommand();

        #endregion

        #region Shortcut key members

        public static IEnumerable<string> GetShortcutKey(RoutedCommand routedCommand)
        {
            List<string> shortcutKeys = new List<string>();
            foreach (KeyGesture keyGesture in routedCommand.InputGestures.OfType<KeyGesture>())
            {
                if (keyGesture.Modifiers != ModifierKeys.None)
                {
                    string modifierKeys = string.Join("+", GetFlagsWithExclusion((ModifierKeysMapping)keyGesture.Modifiers, ModifierKeysMapping.None));
                    shortcutKeys.Add($"{modifierKeys}+{keyGesture.Key}");
                }
                else
                {
                    shortcutKeys.Add(keyGesture.Key.ToString());
                }
            }

            return shortcutKeys;
        }

        static IEnumerable<T> GetFlagsWithExclusion<T>(T input, T valueToExclude) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)).OfType<T>())
            {
                if (!value.Equals(valueToExclude) && input.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        static IEnumerable<T> GetFlags<T>(T input) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)).OfType<T>())
            {
                if (input.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        [Flags]
        private enum ModifierKeysMapping
        {
            None = 0x0,
            Alt = 0x1,
            Ctrl = 0x2,
            Shift = 0x4,
            Win = 0x8
        }

		#endregion
	}
}
