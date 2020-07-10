#region Usings

using System.Windows;
using GalaSoft.MvvmLight;
using System.Windows.Controls;
using System.Windows.Media;

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

        public string ImageSource
        {
            get => Model.ImageSource;
            set => Model.ImageSource = value;
        }

        public string LightImageSource
        {
            get => Model.LightImageSource;
            set => Model.LightImageSource = value;
        }

        public SolidColorBrush HoverColor
        {
            get => Model.HoverColor;
            set => Model.HoverColor = value;
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

        public string ImageSource
        {
            get => _imageSource;
            set
            {
                Set(nameof(ImageSource), ref _imageSource, value);

                if (_lightImageSource is null)
                {
                    RaisePropertyChanged(nameof(LightImageSource));
                }
            }
        }
        private string _imageSource;

        public string LightImageSource
        {
            get => _lightImageSource ?? _imageSource;
            set => Set(nameof(LightImageSource), ref _lightImageSource, value);
        }
        private string _lightImageSource;

        public SolidColorBrush HoverColor
        {
            get => _hoverColor;
            set => Set(nameof(HoverColor), ref _hoverColor, value);
        }
        private SolidColorBrush _hoverColor = new SolidColorBrush(Color.FromRgb(190, 230, 253)); // This is the default hover color in WPF
    }
}
