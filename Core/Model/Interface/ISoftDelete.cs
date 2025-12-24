namespace Core.Model.Interface;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}