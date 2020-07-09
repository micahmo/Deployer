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
