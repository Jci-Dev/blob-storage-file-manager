using Microsoft.AspNetCore.Http;

namespace library;

public class StreamFormFile(Stream stream, string contentType, string fileName) : IFormFile
{
    public Stream OpenReadStream()
    {
        return stream;
    }

    public void CopyTo(Stream target)
    {
        stream.CopyTo(target);
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        await stream.CopyToAsync(target, cancellationToken);
    }

    public string ContentType { get; } = contentType;

    public string ContentDisposition => null;
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => stream.Length;
    public string Name { get; } = fileName;

    public string FileName => Name;
}