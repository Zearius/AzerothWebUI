namespace AzerothWebUI.Core.Data;

public class FileSystemConfigFileStore(string filePath) : IConfigFileStore
{
    public Task<string> ReadAllTextAsync() => File.ReadAllTextAsync(filePath);

    public Task WriteAllTextAsync(string content) => File.WriteAllTextAsync(filePath, content);
}
