using Microsoft.EntityFrameworkCore;

namespace Core.Model;

[Owned]
public class DeckStatistic
{
    public int Due { get; set; }
    public int New { get; set; }
}