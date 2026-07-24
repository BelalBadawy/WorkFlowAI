namespace WFAI.Application.Dtos.Common
{
    public class FileData
    {
        public Stream Content { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
    }
}