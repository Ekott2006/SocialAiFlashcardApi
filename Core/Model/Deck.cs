using Core.Model.Helper;
using Core.Model.Helper.Deck;
using Core.Model.Interface;
using Microsoft.EntityFrameworkCore;

namespace Core.Model;

[Index(nameof(Name), nameof(CreatorId), IsUnique = true)]
public class Deck:  HelperModelEntity, IPagination<int>
{
    public int Id { get; set; }
    public string CreatorId {get; set;}
    public User Creator { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DeckOption Option { get; set; }
    public DeckStatistic Statistic { get; set; }
    public bool IsUserOption { get; set; }
    public bool IsDeleted { get; set; }
}

// TODO: Will be generated from a worker