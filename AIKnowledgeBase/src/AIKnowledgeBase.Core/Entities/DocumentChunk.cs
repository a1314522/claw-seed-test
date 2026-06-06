using System.ComponentModel.DataAnnotations;

namespace AIKnowledgeBase.Core.Entities;

public class DocumentChunk : BaseEntity
{
    public int DocumentId { get; set; }
    public virtual Document Document { get; set; } = null!;

    public int ChunkIndex { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public string? Embedding { get; set; }
}
