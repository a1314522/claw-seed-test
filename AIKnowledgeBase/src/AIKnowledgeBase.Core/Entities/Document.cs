using System.ComponentModel.DataAnnotations;
using AIKnowledgeBase.Core.Enums;

namespace AIKnowledgeBase.Core.Entities;

public class Document : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(50)]
    public string DocType { get; set; } = string.Empty;

    public int CategoryId { get; set; } = 1;
    public virtual Category Category { get; set; } = null!;

    public int ChunkCount { get; set; } = 0;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    [MaxLength(50)]
    public string? UploadedBy { get; set; }

    public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
