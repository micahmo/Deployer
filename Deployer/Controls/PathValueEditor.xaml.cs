using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Deployer
{
    /// <summary>
    /// Interaction logic for PathValueEditor.xaml
    /// </summary>
    public partial class PathValueEditor : UserControl
    {
        public PathValueEditor()
        {
            InitializeComponent();
        }

        #region Dependency properties

        public static readonly DependencyProperty TopValueProperty = DependencyProperty.Register(nameof(TopValue), typeof(object), typeof(PathValueEditor), new PropertyMetadata(null, TopValueChangedCallback));

        public static readonly DependencyProperty ValueCountProperty = DependencyProperty.Register(nameof(ValueCount), typeof(int), typeof(PathValueEditor), new PropertyMetadata(0, ValueCountChanged));

        #endregion

        #region Public properties

        public object TopValue
        {
            get => GetValue(TopValueProperty);
            set => SetValue(TopValueProperty, value);
        }

        public int ValueCount
        {
            get => (int) GetValue(ValueCountProperty);
            set => SetValue(ValueCountProperty, value);
        }


        #endregion

        internal PossiblePathVariable DataModel => DataContext as PossiblePathVariable;

        public PathValueEditorModel ViewModel { get; } = new PathValueEditorModel();

        #region Public events

        public event EventHandler<ValueRemovedEventArgs> ValueRemoved;

        public event EventHandler ValueAdded;

        #endregion

        #region Event handlers

        private static void TopValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathValueEditor control)
            {
                control.ViewModel.ShowAddValueButton = control.DataModel == control.TopValue;
            }
        }

        private static void ValueCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PathValueEditor control)
            {
                control.ViewModel.ShowRemoveValueButton = control.ValueCount > 1;
            }
        }

        #endregion

        #region Internal methods

        internal void OnValueRemoved()
        {
            ValueRemoved?.Invoke(this, new ValueRemovedEventArgs(DataModel));
        }

        internal void OnValueAdded()
        {
            ValueAdded?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }

    public class PathValueEditorModel : ObservableObject
    {
        public PathValueEditorModel()
        {
            Commands = new PathValueEditorCommands();
        }

        public PathValueEditorCommands Commands { get; }

        public bool ShowAddValueButton {
            get => _showAddValueButton;
            set => Set(nameof(ShowAddValueButton), ref _showAddValueButton, value);
        }
        private bool _showAddValueButton;

        public bool ShowRemoveValueButton {
            get => _showRemoveValueButton;
            set => Set(nameof(ShowRemoveValueButton), ref _showRemoveValueButton, value);
        }
        private bool _showRemoveValueButton = true;

    }

    public class PathValueEditorCommands
    {
        #region ICommands

        public ICommand AddValueCommand => _addValueCommand ??= new RelayCommand<PathValueEditor>(AddValue);
        private RelayCommand<PathValueEditor> _addValueCommand;

        public ICommand RemoveValueCommand => _removeValueCommand ??= new RelayCommand<PathValueEditor>(RemoveValue);
        private RelayCommand<PathValueEditor> _removeValueCommand;

        #endregion

        #region Implementations

        private void AddValue(PathValueEditor control)
        {
            control.OnValueAdded();
        }

        private void RemoveValue(PathValueEditor control)
        {
            control.OnValueRemoved();
        }

        #endregion
    }

    public class ValueRemovedEventArgs : EventArgs
    {
        public ValueRemovedEventArgs(PossiblePathVariable removedValue) => RemovedValue = removedValue;

        public PossiblePathVariable RemovedValue { get; }
    }
}
