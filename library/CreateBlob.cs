using Microsoft.AspNetCore.Http;

namespace library;

public class CreateBlob
{
    public string ContainerName { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public IFormFile File { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}