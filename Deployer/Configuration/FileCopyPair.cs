namespace Deployer
{
    public class FileCopyPair
    {
        public FileCopyPair(FileItem sourceFile, string destinationPath, bool isDirectory)
        {
            SourceFile = sourceFile;
            DestinationPath = destinationPath;
            IsDirectory = isDirectory;
        }

        public FileItem SourceFile { get; }
     
        public string DestinationPath { get; }

        public bool IsDirectory { get; }
    }
}
