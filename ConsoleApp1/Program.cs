// using System.Text.Json;
// using System.Text.Json.Serialization;
// using ConsoleApp1;
// using Core.Data;
// using Core.Data.ModelFaker;
// using Core.Dto.Common;
// using Core.Model;
// using Core.Repository;
// using Test.Helper;
//

using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleApp1;
using Core.Data;
using Core.Data.ModelFaker;
using Core.Model;
using Core.Repository;
using Microsoft.EntityFrameworkCore;

JsonSerializerOptions jsonSerializerOptions = new()
{
    WriteIndented = true,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
};
DatabaseHelper databaseHelper = new();
NoteTypeRepository repository = new(databaseHelper.Context);
User testUser = await SeedDatabase(databaseHelper.Context);

// Arrange
int userNoteTypeId = 4; // User's non-deleted note type
Console.WriteLine($"Creator ID: {testUser.Id}, ID: {userNoteTypeId}");

// Act
int deleteResult = await repository.Delete(userNoteTypeId, testUser.Id);

Console.WriteLine($"Result: {deleteResult}");
        
// Verify soft delete
var result = await databaseHelper.Context.NoteTypes.Select(x => new {x.Id, x.CreatorId, x.IsDeleted}).ToListAsync();

Console.WriteLine(JsonSerializer.Serialize(result, jsonSerializerOptions));
return;

//
// // Arrange
// PaginationRequest request = new()
// {
//     CursorId = 5,
//     PageSize = 3
// };
//             
// // Act
// PaginationResult<Deck> result = await repository.Get(testUser.Id, request, false);
// Console.WriteLine(JsonSerializer.Serialize(result,  new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.IgnoreCycles}));
//
//
//             
// // Verify deck was restored in database
// databaseHelper.printSqlLog();
// return;
//

async Task<User> SeedDatabase(DataContext context)
{
    // 1. Setup primary user and an extra user (to satisfy foreign keys)
    User? user = new UserFaker().Generate();
    User? anotherUser = new UserFaker().Generate();
    anotherUser.UserName = "Other";
    
    await context.Users.AddRangeAsync(user, anotherUser);
    await context.SaveChangesAsync(); // Essential to establish IDs for Foreign Keys

    // 2. Generate Global NoteTypes
    IEnumerable<NoteType> globalNoteTypes = Enumerable.Range(1, 3)
        .Select(i => new NoteTypeFaker(null, false, $"Global NoteType {i}").Generate());

    // 3. Generate User NoteTypes
    IEnumerable<NoteType> userNoteTypes = Enumerable.Range(4, 7)
        .Select(i => new NoteTypeFaker(user.Id, i > 7, $"User NoteType {i-3}").Generate());

    // 4. Combine all and add the specific "Another User" NoteType
    IEnumerable<NoteType> allNoteTypes = globalNoteTypes
        .Concat(userNoteTypes)
        .Append(new NoteTypeFaker(anotherUser.Id, false, "Another User's NoteType").Generate());

    await context.NoteTypes.AddRangeAsync(allNoteTypes);
    await context.SaveChangesAsync();
    return user;
}

//
// async Task<User> SeedDatabase()
// {
//     User user = new UserFaker();
//     await databaseHelper.Context.Users.AddAsync(user);
//
//     List<NoteType> noteTypes = [];
//     for (int i = 1; i <= 10; i++)
//     {
//         noteTypes.Add(new NoteTypeFaker(null, i > 7));
//     }
//
//     await databaseHelper.Context.NoteTypes.AddRangeAsync(noteTypes);
//     await databaseHelper.Context.SaveChangesAsync();
//     return user;
// }