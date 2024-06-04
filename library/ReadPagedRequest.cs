namespace library;

public class ReadPagedRequest
{
    public string Path { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool GoBack { get; set; }
    public bool GoForward { get; set; }
}