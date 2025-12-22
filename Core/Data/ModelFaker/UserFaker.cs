using Bogus;
using Core.Model;

namespace Core.Data.ModelFaker;

public sealed class UserFaker : Faker<User>
{
    public UserFaker()
    {
        // IdentityUser Properties
        RuleFor(u => u.UserName, f => f.Internet.UserName());
        RuleFor(u => u.PasswordHash, f => f.Internet.Password());
        RuleFor(u => u.Email, f => f.Internet.Email());

        // Custom User Properties
        RuleFor(u => u.ProfileImageUrl, f => f.Internet.Avatar());
        RuleFor(u => u.DeckOption, _ => new DeckOptionFaker());
    }
}