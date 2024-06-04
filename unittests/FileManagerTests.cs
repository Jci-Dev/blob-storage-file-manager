using library;
using Xunit.Abstractions;

namespace unittests;

public class FileManagerTests(ITestOutputHelper testOutputHelper)
{
    private const string ConnectionString =
        "DefaultEndpointsProtocol=https;AccountName=*********;AccountKey=***************************;EndpointSuffix=core.windows.net";

    private const string ContainerName = "test-container-for-blob-storage";
    private readonly BlobStorageFileManagerService _blobStorageFileManagerService = new(ConnectionString);

    [Theory]
    [InlineData("")]
    [InlineData("pics")]
    [InlineData("pics3")]
    [InlineData("pics2/more pics")]
    public async Task CreateBlobPhotoTests(string directory)
    {
        var blobName = "photo-" + DateTime.Now.Ticks + ".jpg";
        const string filePath =
            "/Users/vrods70/Dev/JoiningClouds/blob-storage/unittests/images/1_Ol-5j91kcf23ff0A8spcAw.png";

        var createBlob = CreateBlobFromFile(ContainerName, filePath, directory, blobName);

        var blobFile =
            await _blobStorageFileManagerService.Create(createBlob);

        Assert.NotNull(blobFile);

        var response = await _blobStorageFileManagerService
            .Delete(ContainerName, directory, blobName);

        Assert.True(response);
    }

    [Theory]
    [InlineData("1_Ol-5j91kcf23ff0A8spcAw.png")]
    [InlineData("1607970317643.jpg")]
    [InlineData("IMG-20160408-WA0002.jpg")]
    public async Task CreateBlobTests(string imageName)
    {
        var directory = "";
        var blobName = "photo-" + DateTime.Now.Ticks + ".jpg";
        var filePath =
            $"/Users/vrods70/Dev/JoiningClouds/blob-storage/unittests/images/{imageName}";

        var createBlob = CreateBlobFromFile(ContainerName, filePath, directory, blobName);

        var blobFile =
            await _blobStorageFileManagerService.Create(createBlob);

        Assert.NotNull(blobFile);

        var response = await _blobStorageFileManagerService
            .Delete(ContainerName, directory, blobName);

        Assert.True(response);
    }

    [Theory]
    [InlineData("", "images")]
    [InlineData("folder1/", "images")]
    [InlineData("folder2/more folders/", "images")]
    [InlineData("folder3/folder4/folder5/", "images")]
    public async Task BlobFileManagerCreateDeleteFolderTest(string path, string folderName)
    {
        var blobDirectory = await _blobStorageFileManagerService
            .CreateFolder(ContainerName, path, folderName);

        Assert.NotNull(blobDirectory.Url);
        Assert.Equal(blobDirectory.Name, folderName);
        Assert.True(blobDirectory.IsDirectory);
        Assert.False(blobDirectory.HasDirectories);

        var response = await _blobStorageFileManagerService
            .DeleteFolder(ContainerName, path, folderName);

        Assert.True(response.Count > 0);
    }

    [Fact]
    public async Task GetContainerStatisticsTest()
    {
        var statistics = await _blobStorageFileManagerService.GetContainerStatistics(ContainerName);

        Assert.True(statistics.TotalDirectories > 0);
        Assert.True(statistics.TotalFiles > 0);
        testOutputHelper.WriteLine(statistics.UsedCapacity.ToString());
    }

    private MemoryStream ConvertImageToMemoryStream(string imagePath)
    {
        var imageBytes = File.ReadAllBytes(imagePath);
        var ms = new MemoryStream(imageBytes);
        return ms;
    }

    private CreateBlob CreateBlobFromFile(string containerName, string filePath, string prefix, string fileName)
    {
        var imageStream = ConvertImageToMemoryStream(filePath);
        imageStream.Position = 0;
        var extension = fileName.Split('.').Last();

        var contentType = $"image/{extension}";

        var formFile = new StreamFormFile(imageStream, contentType, fileName);

        var createBlob = new CreateBlob
        {
            ContainerName = containerName,
            Path = prefix,
            FileName = fileName,
            File = formFile
        };
        return createBlob;
    }
}