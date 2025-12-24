using Core.Model.Helper.Deck;
using Microsoft.AspNetCore.Identity;

namespace Core.Model;

public class User: IdentityUser
{
    public DeckOption DeckOption { get; set; }
    public string ProfileImageUrl { get; set; }
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Deck> Decks { get; set; } = [];
}