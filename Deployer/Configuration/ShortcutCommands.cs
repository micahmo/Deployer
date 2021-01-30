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
            PreviousConfigurationCommand.InputGestures.Add(new KeyGesture(Key.PageUp, ModifierKeys.Control));
            NextConfigurationCommand.InputGestures.Add(new KeyGesture(Key.PageDown, ModifierKeys.Control));
            MoveConfigurationUpCommand.InputGestures.Add(new KeyGesture(Key.PageUp, ModifierKeys.Control | ModifierKeys.Shift));
            MoveConfigurationDownCommand.InputGestures.Add(new KeyGesture(Key.PageDown, ModifierKeys.Control | ModifierKeys.Shift));
            ShowAboutCommand.InputGestures.Add(new KeyGesture(Key.F1));
        }

        public static RoutedCommand ReloadCurrentConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand HardReloadCurrentConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand DeployCurrentConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand PreviousConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand NextConfigurationCommand { get; } = new RoutedCommand();

        public static RoutedCommand MoveConfigurationUpCommand { get; } = new RoutedCommand();

        public static RoutedCommand MoveConfigurationDownCommand { get; } = new RoutedCommand();

        public static RoutedCommand ShowAboutCommand { get; } = new RoutedCommand();

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
                    shortcutKeys.Add($"{modifierKeys}+{ToString(keyGesture.Key)}");
                }
                else
                {
                    shortcutKeys.Add(ToString(keyGesture.Key));
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

        /// <summary>
        /// <see cref="Key"/> has duplicate values.
        /// This method returns the preferred string representation for duplicate values, if any. Otherwise, it just returns <see cref="Key.ToString()"/>.
        /// </summary>
        /// <remarks>
        /// Key.ToString always returns the name of the first enum for a given value. Use nameof to get the preferred name.
        /// </remarks>
        private static string ToString(Key key)
        {
            string result = key.ToString();
            
            switch (key)
            {
                case Key.Next:
                    result = nameof(Key.PageDown);
                    break;
            }

            return result;
        }

		#endregion
	}
}
