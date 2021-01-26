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

        public void Information(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public NotifyOption Information(string message, string title, NotifyOption firstOption, NotifyOption secondOption)
        {
            NotifyOption result = default;

            Style style = new Style();
            style.Setters.Add(new Setter(MessageBox.YesButtonContentProperty, firstOption.Text));
            style.Setters.Add(new Setter(MessageBox.NoButtonContentProperty, secondOption.Text));

            switch (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information, style))
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

        public void Warning(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public NotifyOption Warning(string message, string title, NotifyOption firstOption, NotifyOption secondOption, NotifyOption thirdOption)
        {
            NotifyOption result = default;

            Style style = new Style();
            style.Setters.Add(new Setter(MessageBox.YesButtonContentProperty, firstOption.Text));
            style.Setters.Add(new Setter(MessageBox.NoButtonContentProperty, secondOption.Text));
            style.Setters.Add(new Setter(MessageBox.CancelButtonContentProperty, thirdOption.Text));

            switch (MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, style))
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
