using Core.Data;
using Core.Dto.Note;
using Core.Helper;
using Core.Model;
using Core.Repository;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class NoteService(INoteTypeRepository noteTypeRepository, INoteRepository repository, DataContext context)
{
    private static Dictionary<string, string> Cleanup(string creatorId, List<string> fields,
        Dictionary<string, string> data)
    {
        return fields.ToDictionary(
            key => key,
            key => data.GetValueOrDefault(key, string.Empty)
        );
    }
    

    public async Task<Note?> Create(string creatorId, int deckId, int noteTypeId, List<CreateNoteRequest> request)
    {
        // Check that the NoteType and the Deck Id is valid
        bool doesDeckExist = await context.Decks.AnyAsync(x => x.Id == deckId && x.CreatorId == creatorId);
        if (!doesDeckExist) return null;
        var noteType = await context.NoteTypes
            .Select(x => new { x.Id, x.CreatorId, x.Fields, x.Templates })
            .FirstOrDefaultAsync(x => x.Id == noteTypeId && (x.CreatorId == creatorId || x.CreatorId == null));
        if (noteType == null) return null;

        // Create multiple Note and Cards in a
        List<Task<Note>> asyncNotes = request.Select(async x =>
        {
            Dictionary<string, string> data = Cleanup(creatorId, noteType.Fields.ToList(), x.Data);
            
            // 1. Prepare the tasks for all cards
            IEnumerable<Task<Card>> cardTasks = noteType.Templates.Select(async template => new Card
            {
                DeckId = deckId,
                CreatorId = creatorId,
                Front = await TemplateHelper.Parse(template.Front, data),
                Back = await TemplateHelper.Parse(template.Back, data),
                IsSuspended = false,
                
                DueDate = DateTime.UtcNow, // Better than default
                Interval = 0,
                // Ease = 2.5f // Standard starting ease
            });

            Card[] createdCards = await Task.WhenAll(cardTasks);

            return new Note
            {
                DeckId = deckId,
                NoteTypeId = noteTypeId,
                CreatorId = creatorId,
                Data = data,
                Tags = x.Tags,
                Cards = createdCards.ToList()
            };
        }).ToList();
        Note[] notes = await Task.WhenAll(asyncNotes);
        await context.Notes.AddRangeAsync(notes);
        await context.SaveChangesAsync();
        return notes.ToList()[0];
    }

    public async Task<int> Update(int id, string creatorId, UpdateNoteRequest request)
    {
        // TODO: Optimise here
        Note? note = await repository.GetAdvanced(creatorId, id);
        if (note == null) return 0;

        request.Data = Cleanup(creatorId, note.NoteType.Fields.ToList(), request.Data);
        return await repository.Update(id, creatorId, request);
    }
}