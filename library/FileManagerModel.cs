namespace library
{
    public class FileManagerModel
    {
        public string Name { get; set; }
        public long? Size { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public string Extension { get; set; }
        public string IconClass { get; set; } 
        public bool IsDirectory { get; set; }
        public bool HasDirectories { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? CreatedUtc { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public DateTimeOffset? ModifiedUtc { get; set; }
        public Guid Id { get; set; }
    }
}