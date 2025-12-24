namespace Core.Model.Interface;

public abstract class HelperModelEntity : IHasTimestamps, ISoftDelete
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}