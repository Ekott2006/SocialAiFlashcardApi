var template = Template.Parse("Hello !");
var result = template.Render(new { Name = "World" }); 


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
// DatabaseHelper databaseHelper = new();
// DeckRepository repository = new(databaseHelper.Context);
// User testUser = SeedDatabase().Result;
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
// async Task<User> SeedDatabase()
// {
//     User user = new UserFaker();
//     await databaseHelper.Context.Users.AddAsync(user);
//         
//     List<Deck> decks = [];
//     for (int i = 1; i <= 10; i++)
//     {
//         decks.Add(new DeckFaker(user.Id, i > 7));
//     }
//             
//     await databaseHelper.Context.Decks.AddRangeAsync(decks);
//     await databaseHelper.Context.SaveChangesAsync();
//     return user;
// }
//
//
//
// namespace ConsoleApp1
// {
//     class DatabaseHelper : DatabaseSetupHelper
//     {
//         public new readonly DataContext Context;
//
//         public DatabaseHelper()
//         {
//             Context = base.Context;
//         
//         }
//
//         public void printSqlLog()
//         {
//             _sqlLog.ForEach(Console.WriteLine);
//         }
//     
//     }
// }