using Microsoft.AspNetCore.Http;

namespace library;

public interface IBlobStorageFileManagerService
{
    Task<IList<FileManagerModel>> ReadAll(string containerName, string path);

    Task<FileManagerModel> Create(CreateBlob item);

    Task<Stream> Download(string containerName, string folderName, string blobName);
    Task<FileManagerModel> Get(string containerName, string folderName, string blobName);

    Task<FileManagerModel> Update(string containerName, string path, string blobName,
        IFormFile file);

    Task<bool> Delete(string containerName, string path, string blobName);
    Task<FileManagerModel> Rename(string containerName, string path, string oldBlobName, string newBlobName);
    Task<FileManagerModel> CreateFolder(string containerName, string path, string folderName);
    Task<IList<string>> DeleteFolder(string containerName, string path, string folderName);
    Task<BlobItemsPaged> GetBlobsInDirectory(string prefix, int pageSize, string continuationToken);
    Task<BlobItemsPaged> ReadAll(string containerName, string path, int pageSize, string continuationToken);
    Task<ContainerStats> GetContainerStatistics(string containerName);
}