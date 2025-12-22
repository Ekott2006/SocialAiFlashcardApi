namespace Core.Model;

public class UserRefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }
    public DateTime Validity { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }
}