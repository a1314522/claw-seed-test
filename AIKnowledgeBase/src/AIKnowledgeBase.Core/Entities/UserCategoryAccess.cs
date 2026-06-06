namespace AIKnowledgeBase.Core.Entities;

public class UserCategoryAccess : BaseEntity
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanDelete { get; set; } = false;
}
