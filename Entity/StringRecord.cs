namespace StringAnalyzerService.Entity
{
    public class StringRecord
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public StringProperties Properties { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
