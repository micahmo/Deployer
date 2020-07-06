#region Usings

using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for SectionHeader.xaml
    /// </summary>
    public partial class SectionHeader : UserControl
    {
        #region Constructor

        public SectionHeader()
        {
            InitializeComponent();
            DataContext = Model;
        }

        #endregion

        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(SectionHeader), new PropertyMetadata(null, HeaderTextChangedCallback));

        #region Public properties

        /// <summary>
        /// Defines the text that is shown in the section header
        /// </summary>
        public string HeaderText
        {
            get => Model.HeaderText;
            set => Model.HeaderText = value;
        }

        #endregion

        #region Event handlers

        private static void HeaderTextChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SectionHeader control)
            {
                control.Model.HeaderText = e.NewValue?.ToString();
            }
        }

        #endregion

        #region Private properties

        private SectionHeaderModel Model { get; } = new SectionHeaderModel();

        #endregion
    }

    internal class SectionHeaderModel : ObservableObject
    {
        public string HeaderText
        {
            get => _headerText;
            set => Set(nameof(HeaderText), ref _headerText, value);
        }
        private string _headerText;
    }
}
