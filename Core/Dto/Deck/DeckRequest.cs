using System.ComponentModel.DataAnnotations;

namespace Core.Dto.Deck;

public class DeckRequest : IValidatableObject
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string? Description { get; set; }

    [Required] public bool IsPublic { get; set; }

    public bool IsUserOption { get; set; }
    public DeckOptionRequest? OptionRequest { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsUserOption && OptionRequest == null)
        {
            yield return new ValidationResult(
                "OptionRequest is required when IsUserOption is true.",
                [nameof(OptionRequest)]
            );
        }
    }
}