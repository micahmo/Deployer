#region Usings

using System.Windows;
using GuiLibraryInterfaces;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

#endregion

namespace WindowsGuiLibrary
{
    public class MessageBoxNotify : INotify
    {
        public QuestionResult Question(string message, string title, QuestionOptions options)
        {
            MessageBoxResult result = MessageBox.Show(message, title, QuestionOptionsToMessageBoxButton(options), MessageBoxImage.Question);
            return MessageBoxResultToQuestionResult(result);
        }

        public NotifyOption Question(string message, string title, NotifyOption firstOption, NotifyOption secondOption, NotifyOption thirdOption)
        {
            return ThreeOptionMessage(MessageBoxImage.Question, message, title, firstOption, secondOption, thirdOption);
        }

        public void Information(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public NotifyOption Information(string message, string title, NotifyOption firstOption, NotifyOption secondOption)
        {
            return TwoOptionMessage(MessageBoxImage.Information, message, title, firstOption, secondOption);
        }

        public void Warning(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public NotifyOption Warning(string message, string title, NotifyOption firstOption, NotifyOption secondOption, NotifyOption thirdOption)
        {
            return ThreeOptionMessage(MessageBoxImage.Warning, message, title, firstOption, secondOption, thirdOption);
        }

        private NotifyOption TwoOptionMessage(MessageBoxImage image, string message, string title, NotifyOption firstOption, NotifyOption secondOption)
        {
            NotifyOption result = default;

            // Set the button text
            Style style = new Style();
            style.Setters.Add(new Setter(MessageBox.YesButtonContentProperty, firstOption.Text));
            style.Setters.Add(new Setter(MessageBox.NoButtonContentProperty, secondOption.Text));

            // Add descriptions as tooltips, if given
            if (string.IsNullOrEmpty(firstOption.Description) == false)
            {
                Style firstButtonStyle = new Style();
                firstButtonStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, firstOption.Description));
                style.Setters.Add(new Setter(MessageBox.YesButtonStyleProperty, firstButtonStyle));
            }

            if (string.IsNullOrEmpty(secondOption.Description) == false)
            {
                Style secondButtonStyle = new Style();
                secondButtonStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, secondOption.Description));
                style.Setters.Add(new Setter(MessageBox.NoButtonStyleProperty, secondButtonStyle));
            }

            switch (MessageBox.Show(message, title, MessageBoxButton.YesNo, image, style))
            {
                case MessageBoxResult.Yes:
                    result = firstOption;
                    break;
                case MessageBoxResult.No:
                    result = secondOption;
                    break;
            }

            return result;
        }

        private NotifyOption ThreeOptionMessage(MessageBoxImage image, string message, string title, NotifyOption firstOption, NotifyOption secondOption, NotifyOption thirdOption)
        {
            NotifyOption result = default;

            // Set the button text
            Style style = new Style();
            style.Setters.Add(new Setter(MessageBox.YesButtonContentProperty, firstOption.Text));
            style.Setters.Add(new Setter(MessageBox.NoButtonContentProperty, secondOption.Text));
            style.Setters.Add(new Setter(MessageBox.CancelButtonContentProperty, thirdOption.Text));

            // Add descriptions as tooltips, if given
            if (string.IsNullOrEmpty(firstOption.Description) == false)
            {
                Style firstButtonStyle = new Style();
                firstButtonStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, firstOption.Description));
                style.Setters.Add(new Setter(MessageBox.YesButtonStyleProperty, firstButtonStyle));
            }

            if (string.IsNullOrEmpty(secondOption.Description) == false)
            {
                Style secondButtonStyle = new Style();
                secondButtonStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, secondOption.Description));
                style.Setters.Add(new Setter(MessageBox.NoButtonStyleProperty, secondButtonStyle));
            }

            if (string.IsNullOrEmpty(thirdOption.Description) == false)
            {
                Style thirdButtonStyle = new Style();
                thirdButtonStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, thirdOption.Description));
                style.Setters.Add(new Setter(MessageBox.CancelButtonStyleProperty, thirdButtonStyle));
            }

            switch (MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, image, style))
            {
                case MessageBoxResult.Yes:
                    result = firstOption;
                    break;
                case MessageBoxResult.No:
                    result = secondOption;
                    break;
                case MessageBoxResult.Cancel:
                    result = thirdOption;
                    break;
            }

            return result;
        }

        private MessageBoxButton QuestionOptionsToMessageBoxButton(QuestionOptions options)
        {
            switch (options)
            {
                case QuestionOptions.YesNo:
                    return MessageBoxButton.YesNo;
            }

            return default;
        }

        private QuestionResult MessageBoxResultToQuestionResult(MessageBoxResult result)
        {
            switch (result)
            {
                case MessageBoxResult.OK:
                    return QuestionResult.OK;
                case MessageBoxResult.Cancel:
                    return QuestionResult.Cancel;
                case MessageBoxResult.Yes:
                    return QuestionResult.Yes;
                case MessageBoxResult.No:
                    return QuestionResult.No;
                case MessageBoxResult.None:
                    return QuestionResult.None;
            }

            return default;
        }
    }
}
