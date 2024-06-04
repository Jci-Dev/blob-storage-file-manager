namespace library;

public class BlobItemsPaged
{
    public string ContinuationToken { get; set; }
    public List<FileManagerModel> Items { get; set; }
}