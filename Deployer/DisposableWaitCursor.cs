using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Deployer
{
    // https://stackoverflow.com/a/3481274/4206279
    public class WaitCursor : IDisposable
    {
        private readonly Cursor _previousCursor;
        private readonly DispatcherPriority _dispatcherPriority;

        public WaitCursor(DispatcherPriority dispatcherPriority = DispatcherPriority.Normal)
        {
            _previousCursor = Mouse.OverrideCursor;
            _dispatcherPriority = dispatcherPriority;

            Mouse.OverrideCursor = Cursors.Wait;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = _previousCursor;
            }, _dispatcherPriority);
        }

        #endregion
    }
}
