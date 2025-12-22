using Microsoft.AspNetCore.Identity;

namespace Core.Model;

public class User: IdentityUser
{
    public DeckOption DeckOption { get; set; }
    public string ProfileImageUrl { get; set; }
    public List<UserRefreshToken> RefreshTokens { get; set; } = [];
    public List<Deck> Decks { get; set; } = [];
}