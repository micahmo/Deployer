using System;
using System.IO;

namespace Utilities
{
    public class SessionFileLogger : ILogger
    {
        #region Constructor

        public SessionFileLogger()
        {
            File.WriteAllText(DEPLOYER_LOG_PATH, string.Empty);
        }

        #endregion

        #region ILogger members

        public void Log(string message)
        {
            File.AppendAllText(DEPLOYER_LOG_PATH, message);
        }

        #endregion

        #region Public methods

        public void Clear()
        {
            File.WriteAllText(DEPLOYER_LOG_PATH, string.Empty);
        }

        #endregion

        #region Public properties

        public string Path => DEPLOYER_LOG_PATH;

        #endregion

        #region Private fields

        private static string DEPLOYER_LOG_NAME = "Deployer.log";

        private static string DEPLOYER_LOG_PATH = XmlSerialization.GetCustomConfigFilePath(Environment.SpecialFolder.ApplicationData, DEPLOYER_LOG_NAME);

        #endregion
    }
}
