#region Usings

using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

#endregion

namespace Utilities
{
    public class XmlSerialization
    {
        /// <summary>
        /// Serializes and writes <paramref name="objectToSerialize"/> to a custom config file, as specified by the <paramref name="configFileName"/> and <paramref name="specialFolder"/>.
        /// Calling this method will guarantee that the path and the file exist.
        /// </summary>
        /// <param name="configFileName">The name of the custom config file to write to.</param>
        /// <param name="objectToSerialize">The object to serialize and write to file.</param>
        /// <param name="specialFolder">
        /// The pre-defined folder in which the custom config file should reside.
        /// If none specified, it will default to Public Documents,
        /// which is guaranteed to be readable and writable by non-administrator users.
        /// </param>
        public static void SerializeObjectToCustomConfigFile<T>(string configFileName, T objectToSerialize, Environment.SpecialFolder specialFolder = Environment.SpecialFolder.CommonDocuments)
        {
            string configFileFullPath = GetCustomConfigFilePath(specialFolder, configFileName);
            SerializeObjectToCustomConfigFile(configFileFullPath, objectToSerialize);
        }

        public static void SerializeObjectToCustomConfigFile<T>(string configFileFullPath, T objectToSerialize)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using var mutex = new Mutex(false, configFileFullPath.GetHashCode().ToString());
            try
            {
                if (mutex.WaitOne())
                {
                    using Stream reader = new FileStream(configFileFullPath, FileMode.Create);
                    serializer.Serialize(reader, objectToSerialize);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Reads data from a custom config file, as specified by the <paramref name="configFileName"/> and the <paramref name="specialFolder"/>.
        /// Calling this method will guarantee that the path and the file exist.
        /// </summary>
        /// <param name="configFileName">The name of the custom config file to read from</param>
        /// <param name="specialFolder">
        /// The pre-defined folder in which the custom config file should reside.
        /// If none specified, it will default to Public Documents,
        /// which is guaranteed to be readable and writable by non-administrator users.
        /// </param>
        /// <returns>The deserialized object</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when there is an error deserializing the XML document</exception>
        public static T DeserializeObjectFromCustomConfigFile<T>(string configFileName, Environment.SpecialFolder specialFolder = Environment.SpecialFolder.CommonDocuments)
        {
            string configFileFullPath = GetCustomConfigFilePath(specialFolder, configFileName);
            return DeserializeObjectFromCustomConfigFile<T>(configFileFullPath);
        }

        public static T DeserializeObjectFromCustomConfigFile<T>(string configFileFullPath)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using FileStream configFile = File.Open(configFileFullPath, FileMode.Open, FileAccess.Read);
            using StreamReader streamReader = new StreamReader(configFile);

            return (T)xmlSerializer.Deserialize(streamReader);
        }

        public static string GetCustomConfigFilePath(Environment.SpecialFolder specialFolder, string configFileName, bool createIfNotExists = true)
        {
            string customConfigFilePath = Environment.GetFolderPath(specialFolder);
            customConfigFilePath = Path.Combine(customConfigFilePath, "Deployer");

            if (Directory.Exists(customConfigFilePath) == false)
            {
                // If the directory doesn't exist, create it.
                Directory.CreateDirectory(customConfigFilePath);
            }

            customConfigFilePath = Path.Combine(customConfigFilePath, configFileName);

            if (File.Exists(customConfigFilePath) == false && createIfNotExists)
            {
                // If the file doesn't exist, create it
                using (File.Create(customConfigFilePath)) { }
            }

            return customConfigFilePath;
        }
    }
}
