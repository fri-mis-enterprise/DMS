namespace Document_Management.Models
{
    public class GeneralSearchViewModel
    {
        public List<FileDocument> Results { get; set; } = [];
        public int CurrentPage { get; set; }
        public bool HasNextPage { get; set; }
        public int PageSize { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public bool SearchDocumentText { get; set; }
        public string SortBy { get; set; } = "DateUploaded";
        public string SortOrder { get; set; } = "desc";
    }
}
