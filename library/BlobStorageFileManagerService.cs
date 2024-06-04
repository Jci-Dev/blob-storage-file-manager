using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;


namespace library;

public class BlobStorageFileManagerService : IBlobStorageFileManagerService
{
    private readonly bool _isPrivate;

    private const string DefaultFilter =
        "*.txt,*.csv,*.doc,*.docx,*.xls,*.xlsx,*.ppt,*.pptx,*.zip,*.rar,*.jpg,*.jpeg,*.gif,*.png,*.db,*.pdf";

    private BlobServiceClient _blobServiceClient;
    private BlobContainerClient _blobContainerClient;

    public BlobStorageFileManagerService(
        string blobConnectionString,
        bool isPrivate = true)
    {
        _isPrivate = isPrivate;

        if (IsValidConnectionString(blobConnectionString)) return;

    }

    public async Task<IList<FileManagerModel>> ReadAll(string containerName, string path = "")
    {
        var containerIsValid = await IsValidateContainer(containerName);

        if (!containerIsValid) return new List<FileManagerModel>();

        var items = await GetBlobsInDirectory(path);

        return items ?? [];
    }

    public async Task<BlobItemsPaged> ReadAll(string containerName, string path, int pageSize, string continuationToken)
    {
        var containerIsValid = await IsValidateContainer(containerName);

        if (!containerIsValid) return new BlobItemsPaged();

        var items = await GetBlobsInDirectory(path, pageSize, continuationToken);

        return items;
    }

    public async Task<FileManagerModel> Create(CreateBlob item)
    {
        if (!IsValidFile(item.FileName)) throw new Exception("Invalid file type");

        await IsValidateContainer(item.ContainerName);

        var content = item.File.OpenReadStream();
        content.Position = 0;

        var filename = $"{item.Path}{item.FileName}";

        var blobClient = _blobContainerClient.GetBlobClient(filename);
        var response = await blobClient.UploadAsync(content, true);

        if (!response.HasValue) return null;

        if (item.Metadata != default)
        {
            await blobClient.SetMetadataAsync(item.Metadata);
        }

        var blobProperties = await blobClient.GetPropertiesAsync();
        return new FileManagerModel
        {
            Name = blobClient.Name.Split('/').LastOrDefault()?.Split('.').FirstOrDefault(),
            Extension = $".{blobClient.Name.Split('.').LastOrDefault()}",
            Path = blobClient.Name,
            IsDirectory = false,
            Id = Guid.NewGuid(),
            HasDirectories = false,
            Size = blobProperties.Value.ContentLength,
            Created = blobProperties.Value.CreatedOn,
            CreatedUtc = blobProperties.Value.CreatedOn,
            Modified = blobProperties.Value.LastModified,
            ModifiedUtc = blobProperties.Value.LastModified,
            IconClass = GetIconClass(blobClient.Name.Split('/').LastOrDefault()),
            Url = blobClient.Uri.ToString()
        };
    }

    // Generic method to read a blob
    public async Task<Stream> Download(string containerName, string path, string blobName)
    {
        await IsValidateContainer(containerName);

        var prefix = path == string.Empty ? blobName : $"{path}{blobName}";
        var blobClient = _blobContainerClient.GetBlobClient(prefix);

        BlobDownloadInfo download = await blobClient.DownloadAsync();
        return download.Content;
    }

    public async Task<FileManagerModel> Get(string containerName, string path, string blobName)
    {
        await IsValidateContainer(containerName);

        var name = path == string.Empty ? blobName : $"{path}{blobName}";
        var blobClient = _blobContainerClient.GetBlobClient(name);

        if (!await blobClient.ExistsAsync()) return null;

        var blobProperties = await blobClient.GetPropertiesAsync();
        return new FileManagerModel
        {
            Name = blobClient.Name.Split('/').LastOrDefault(),
            Extension = $".{blobClient.Name.Split('.').LastOrDefault()}",
            Path = blobClient.Name,
            IsDirectory = false,
            Id = Guid.NewGuid(),
            HasDirectories = false,
            Size = blobProperties.Value.ContentLength,
            Created = blobProperties.Value.CreatedOn,
            CreatedUtc = blobProperties.Value.CreatedOn,
            Modified = blobProperties.Value.LastModified,
            ModifiedUtc = blobProperties.Value.LastModified,
            IconClass = GetIconClass(blobClient.Name.Split('/').LastOrDefault()),
            Url = blobClient.Uri.ToString()
        };
    }

    public async Task<FileManagerModel> Update(string containerName, string path, string blobName,
        IFormFile file)
    {
        await IsValidateContainer(containerName);

        var prefix = path == string.Empty ? blobName : $"{path}/{blobName}";
        var blobClient = _blobContainerClient.GetBlobClient(prefix);
        var content = file.OpenReadStream();
        content.Position = 0;

        await blobClient.UploadAsync(content, true);

        var blobProperties = await blobClient.GetPropertiesAsync();
        return new FileManagerModel
        {
            Name = blobClient.Name.Split('/').LastOrDefault()?.Split('.').FirstOrDefault(),
            Extension = $".{blobClient.Name.Split('.').LastOrDefault()}",
            Path = blobClient.Name,
            IsDirectory = false,
            Id = Guid.NewGuid(),
            HasDirectories = false,
            Size = blobProperties.Value.ContentLength,
            Created = blobProperties.Value.CreatedOn,
            CreatedUtc = blobProperties.Value.CreatedOn,
            Modified = blobProperties.Value.LastModified,
            ModifiedUtc = blobProperties.Value.LastModified,
            IconClass = GetIconClass(blobClient.Name.Split('/').LastOrDefault()),
            Url = blobClient.Uri.ToString()
        };
    }

    public async Task<bool> Delete(string containerName, string path, string blobName)
    {
        await IsValidateContainer(containerName);

        var prefix = path == string.Empty ? blobName : $"{path}{blobName}";
        var blobClient = _blobContainerClient.GetBlobClient(prefix);

        var response = await blobClient.DeleteIfExistsAsync();

        return response.Value;
    }

    public async Task<FileManagerModel> Rename(string containerName, string path, string oldBlobName,
        string newBlobName)
    {
        await IsValidateContainer(containerName);

        var oldPrefix = path == string.Empty ? oldBlobName : $"{path}{oldBlobName}";
        var newPrefix = path == string.Empty ? newBlobName : $"{path}{newBlobName}";

        // Get a reference to the blob to be renamed (i.e., the source blob).
        var sourceBlobClient = _blobContainerClient.GetBlobClient(oldPrefix);

        // Check if the source blob exists
        if (!await sourceBlobClient.ExistsAsync())
        {
            throw new Exception("Source blob does not exist");
        }

        // Get a reference to the destination blob (i.e., the blob with the new name).
        var destinationBlobClient = _blobContainerClient.GetBlobClient(newPrefix);

        // Start the copy operation. This will create a new blob with the same content as the source blob.
        await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

        // Wait for the copy operation to finish.
        while ((await destinationBlobClient.GetPropertiesAsync()).Value.CopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(5000); // Wait for 1 second
        }

        // If the copy operation was successful, delete the source blob.
        if ((await destinationBlobClient.GetPropertiesAsync()).Value.CopyStatus == CopyStatus.Success)
        {
            await sourceBlobClient.DeleteIfExistsAsync();

            var newFile = await Get(containerName, path, newBlobName);
            return newFile;
        }
        else
        {
            return null;
        }
    }

    public async Task<FileManagerModel> CreateFolder(string containerName, string path, string folderName)
    {
        await IsValidateContainer(containerName);

        // Create a small placeholder file in the folder
        var prefix = path == string.Empty ? folderName : $"{path}{folderName}";
        var blobClient = _blobContainerClient.GetBlobClient($"{prefix}/.placeholder");
        var content = new MemoryStream("placeholder"u8.ToArray());
        await blobClient.UploadAsync(content, true);

        var blobProperties = await blobClient.GetPropertiesAsync();
        var model = new FileManagerModel
        {
            Name = prefix.Split('/').LastOrDefault(),
            Extension = $".{blobClient.Name.Split('.').LastOrDefault()}",
            Path = prefix,
            IsDirectory = true,
            Id = Guid.NewGuid(),
            HasDirectories = await DirectoryHasChildren(prefix),
            Size = blobProperties.Value.ContentLength,
            Created = blobProperties.Value.CreatedOn,
            CreatedUtc = blobProperties.Value.CreatedOn,
            Modified = blobProperties.Value.LastModified,
            ModifiedUtc = blobProperties.Value.LastModified,
            IconClass = "ri-folder-5-line",
            Url = blobClient.Uri.ToString()
        };

        return model;
    }

    public async Task<IList<string>> DeleteFolder(string containerName, string path, string folderName)
    {
        await IsValidateContainer(containerName);

        var deletedBlobs = new List<string>();
        var prefix = path == string.Empty ? folderName : $"{path}{folderName}";

        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix))
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                deletedBlobs.Add(blobItem.Name);
            }
        }

        return deletedBlobs;
    }

    private static bool IsValidFile(string fileName)
    {
        var extension = "*." + fileName.Split('.').LastOrDefault();
        var allowedExtensions = DefaultFilter.Split(',');

        return allowedExtensions.Any(e => e.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task<List<FileManagerModel>> GetBlobsInDirectory(string prefix)
    {
        const string delimiter = "/";
        var blobs =
            _blobContainerClient.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, delimiter, prefix);
        var blobItems = new List<FileManagerModel>();

        await foreach (var blobItem in blobs)
        {
            if (blobItem.IsBlob && blobItem.Blob.Name.EndsWith(".placeholder"))
            {
                continue;
            }

            blobItems.Add(blobItem.IsBlob ? CreateFile(blobItem) : await CreateFolder(blobItem));
        }

        return blobItems;
    }

    private async Task<bool> DirectoryHasChildren(string prefix)
    {
        // Get the blobs in the directory
        var blobs = _blobContainerClient.GetBlobsByHierarchyAsync(prefix: prefix);

        // Check if the directory has any children
        await foreach (var blobItem in blobs)
        {
            if (blobItem.IsBlob && blobItem.Blob.Name.EndsWith(".placeholder"))
            {
                continue;
            }

            return true;
        }

        // If we get here, it means the directory has no children
        return false;
    }

    private FileManagerModel CreateFile(BlobHierarchyItem item)
    {
        var blob = item.Blob;

        return new FileManagerModel
        {
            Name = blob.Name.Split('/').LastOrDefault(),
            Extension = $".{blob.Name.Split('.').LastOrDefault()}",
            Path = blob.Name,
            IsDirectory = false,
            Id = Guid.NewGuid(),
            HasDirectories = false,
            Size = blob.Properties.ContentLength,
            Created = blob.Properties.CreatedOn,
            CreatedUtc = blob.Properties.CreatedOn,
            Modified = blob.Properties.LastModified,
            ModifiedUtc = blob.Properties.LastModified,
            IconClass = GetIconClass(blob.Name.Split('/').LastOrDefault())
        };
    }

    private async Task<FileManagerModel> CreateFolder(BlobHierarchyItem item)
    {
        var name = item.Prefix.EndsWith('/') ? item.Prefix.Split('/').Reverse().Skip(1).FirstOrDefault() : item.Prefix;

        return new FileManagerModel
        {
            Name = name,
            Extension = "",
            Path = item.Prefix,
            IsDirectory = item.IsPrefix,
            IconClass = "ri-folder-5-line",
            Id = Guid.NewGuid(),
            HasDirectories = await DirectoryHasChildren(item.Prefix),
        };
    }

    private async Task<bool> IsValidateContainer(string containerName)
    {
        try
        {
            // Create the container if it does not already exist.
            if (_blobContainerClient != null) return true;

            var privateSuffix = _isPrivate ? "-private" : "";
            var accessType = _isPrivate ? PublicAccessType.None : PublicAccessType.Blob;

            _blobContainerClient =
                _blobServiceClient.GetBlobContainerClient($"{containerName.ToLower()}{privateSuffix}");
            await _blobContainerClient.CreateIfNotExistsAsync(accessType);

            return true;
        }
        catch (Exception e)
        {
            throw new Exception("Invalid container name", e);
        }
    }

    private bool IsValidConnectionString(string connectionString)
    {
        try
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetIconClass(string fileName)
    {
        // Split the DefaultFilter into an array of extensions
        var allowedExtensions = DefaultFilter.Replace("*", "").Split(',');

        // Get the extension of the file
        var fileExtension = Path.GetExtension(fileName);

        // Check the extension and return the corresponding CSS class
        if (allowedExtensions.Any(extension =>
                extension.Equals(fileExtension, StringComparison.InvariantCultureIgnoreCase)))
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => "ri-file-pdf-line",
                ".txt" => "ri-file-text-line",
                ".doc" or ".docx" => "ri-file-word-2-line",
                ".xls" or ".xlsx" or ".csv" => "ri-file-excel-line",
                ".ppt" or ".pptx" => "ri-file-ppt-line",
                ".rar" or ".zip" => "ri-file-zip-line",
                ".gif" => "ri-file-gif-line",
                ".jpg" or ".jpeg" or ".png" => "ri-image-2-line",
                ".db" => "ri-database-2-line",
                _ => "ri-file-pdf-line"
            };
        }

        // If the file extension is not in the DefaultFilter, return a default CSS class
        return "ri-file-pdf-line";
    }

    public async Task<BlobItemsPaged> GetBlobsInDirectory(string prefix, int pageSize, string continuationToken)
    {
        const string delimiter = "/";
        var blobs = new List<FileManagerModel>();
        var blobPages = _blobContainerClient
            .GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, delimiter, prefix)
            .AsPages(continuationToken, pageSize);

        await foreach (var blobPage in blobPages)
        {
            // Convert BlobHierarchyItem to FileManagerModel before adding to the list
            foreach (var blobItem in blobPage.Values)
            {
                if (blobItem.IsBlob)
                {
                    blobs.Add(CreateFile(blobItem));
                }
                else
                {
                    blobs.Add(await CreateFolder(blobItem));
                }
            }

            if (blobPage.ContinuationToken == null) continue;
            // Save this continuation token to use for the next page request
            continuationToken = blobPage.ContinuationToken;
            break;
        }

        return new BlobItemsPaged
        {
            ContinuationToken = continuationToken,
            Items = blobs
        };
    }

    public async Task<ContainerStats> GetContainerStatistics(string containerName)
    {
        await IsValidateContainer(containerName);

        long totalSize = 0;
        var totalFiles = 0;

        // Create a HashSet to store unique prefixes
        var uniquePrefixes = new HashSet<string>();

        // Create a Dictionary to store total size per unique prefix
        var sizePerPrefix = new Dictionary<string, long>();

        // Create a queue to hold the prefixes of "folders" to be processed
        var prefixesToProcess = new Queue<string>();
        prefixesToProcess.Enqueue(""); // Start with the root "folder"

        while (prefixesToProcess.Count > 0)
        {
            var currentPrefix = prefixesToProcess.Dequeue();

            await foreach (var blobItem in _blobContainerClient.GetBlobsByHierarchyAsync(prefix: currentPrefix,
                               delimiter: "/"))
            {
                if (blobItem.IsBlob)
                {
                    totalFiles++;
                    var blobSize = blobItem.Blob.Properties.ContentLength ?? 0;
                    totalSize += blobSize;

                    // Use "/" as the key if the currentPrefix is empty
                    var key = string.IsNullOrEmpty(currentPrefix) ? "/" : currentPrefix;

                    // Add the blob size to the total size of its prefix
                    if (sizePerPrefix.ContainsKey(key))
                    {
                        sizePerPrefix[key] += blobSize;
                    }
                    else
                    {
                        sizePerPrefix[key] = blobSize;
                    }
                }
                else if (blobItem.IsPrefix)
                {
                    // Add the prefix to the HashSet and the queue
                    uniquePrefixes.Add(blobItem.Prefix);
                    prefixesToProcess.Enqueue(blobItem.Prefix);
                }
            }
        }

        // The total number of folders is the count of unique prefixes
        var totalFolders = uniquePrefixes.Count;

        return new ContainerStats
        {
            TotalCapacity = 1105199104, // This is an example, replace with your actual total capacity
            UsedCapacity = totalSize,
            RemainingCapacity = 1105199104 - totalSize, // This is an example, replace with your actual total capacity
            TotalFiles = totalFiles,
            TotalDirectories = totalFolders,
            SizePerDirectory = sizePerPrefix // This is the total size per unique prefix
        };
    }
}