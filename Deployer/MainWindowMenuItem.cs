using System;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace Deployer
{
    public class MainWindowMenuItem
    {
        #region Public properties

        public string ImageSource { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Action Command { get; set; }

        public bool IsSeparator { get; private set; }

        public ICommand ClickedCommand => _clickedCommand ??= new RelayCommand(() =>
        {
            Command?.Invoke();
        });
        private RelayCommand _clickedCommand;

        #endregion

        #region Public static

        public static MainWindowMenuItem Separator { get; } = new MainWindowMenuItem {IsSeparator = true};

        #endregion
    }
}
