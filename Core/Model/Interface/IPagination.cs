namespace Core.Model.Interface;

public interface IPagination<T> where T: struct
{
    public T Id { get; set; }
    public DateTime UpdatedAt { get; set; }
}