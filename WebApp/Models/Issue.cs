namespace WebApp.Models
{
    public class Issue
    {
        public string ProjectName { get; set; }
        public string IssueTypeId { get; set; }
        public IssueType IssueType { get; set; }
        public string File { get; set; }
        public string FileType { get; internal set; }
        public int? LineNumber { get; set; }
        public string Message { get; set; }
    }
}
