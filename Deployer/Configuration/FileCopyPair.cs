namespace Deployer
{
    public class FileCopyPair
    {
        public FileCopyPair(FileItem sourceFile, string destinationPath)
            => (SourceFile, DestinationPath) = (sourceFile, destinationPath);
        
        public FileItem SourceFile { get; }
     
        public string DestinationPath { get; }
    }
}
