using System.ComponentModel.DataAnnotations;

namespace Core.Dto.User;

public class RegisterRequest
{
    public string UserName { get; set;}
    [EmailAddress]
    public string Email { get; set;}
    public string ProfileImageUrl { get; set; }
    public string Password { get; set; }
}