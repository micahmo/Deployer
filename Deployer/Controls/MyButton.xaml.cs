#region Usings

using System.Windows;
using GalaSoft.MvvmLight;
using System.Windows.Controls;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for MyButton.xaml
    /// </summary>
    public partial class MyButton : Button
    {
        public MyButton()
        {
            InitializeComponent();
            DataContext = Model;
        }

        public bool ShowBorder
        {
            get => Model.BorderThickness.Left > 0;
            set => Model.BorderThickness = value ? new Thickness(1) : new Thickness(0);
        }

        private MyButtonModel Model { get; } = new MyButtonModel();
    }

    internal class MyButtonModel : ObservableObject
    {
        public Thickness BorderThickness
        {
            get => _borderThickness;
            set => Set(nameof(BorderThickness), ref _borderThickness, value);
        }
        private Thickness _borderThickness = new Thickness(0);
    }
}
