using Core.Data.ModelFaker;
using Core.Dto.Common;
using Core.Dto.Deck;
using Core.Model;
using Core.Model.Helper.Deck;
using Core.Repository;
using Microsoft.EntityFrameworkCore;
using Test.Helper;

namespace Test.Repository;

public class DeckRepositoryTests : DatabaseSetupHelper
{
    private readonly DeckRepository _repository;
    private readonly User _testUser;

    public DeckRepositoryTests()
    {
        _repository = new DeckRepository(Context);
        _testUser = SeedDatabase().Result;
    }
        
    private async Task<User> SeedDatabase()
    {
        User user = new UserFaker();
        await Context.Users.AddAsync(user);
        
        List<Deck> decks = [];
        for (int i = 1; i <= 10; i++)
        {
            decks.Add(new DeckFaker(user.Id, i > 7));
        }
            
        await Context.Decks.AddRangeAsync(decks);
        await Context.SaveChangesAsync();
        return user;
    }
     
        
    [Fact]
    public async Task Get_WithValidParameters_ReturnsPaginatedDecks()
    {
        // Arrange
        PaginationRequest<int> request = new() { PageSize = 5 };
            
        // Act
        PaginationResult<Deck> result = await _repository.Get(_testUser.Id, request, false);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Data.Count);
        //// Assert.Equal(7, result.TotalCount); // Only non-deleted decks
        Assert.Equal(5, result.PageSize);
        Assert.False(result.HasPrevious);
        Assert.True(result.HasNext);
    }
    
    [Fact]
    public async Task Get_WithNullCursorId_ReturnsFirstPage()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            CursorId = null,
            PageSize = 3
        };

        // Act
        PaginationResult<Deck> result = await _repository.Get(_testUser.Id, request, false);

        // Assert
        Assert.Equal(3, result.Data.Count);               
        //Assert.Equal(7, result.TotalCount);             
        Assert.True(result.HasNext);                     
        Assert.False(result.HasPrevious);                
        Assert.Equal([7, 6, 5], result.Data.Select(d => d.Id)); 
    }

    [Fact]
    public async Task Get_WithValidCursorId_ReturnsDecksAfterCursor()
    {
        // Arrange
        const int cursorId = 5;
        PaginationRequest<int> request = new()
        {
            CursorId = cursorId,
            PageSize = 3
        };

        // Act
        PaginationResult<Deck> result = await _repository.Get(_testUser.Id, request, false);

        // Assert
        Assert.Equal(3, result.Data.Count);             
        //Assert.Equal(7, result.TotalCount);           
        Assert.DoesNotContain(cursorId, result.Data.Select(d => d.Id));
        Assert.True(result.HasNext);                    
        Assert.True(result.HasPrevious);                
        Assert.Equal([4, 3, 2], result.Data.Select(d => d.Id));
    }

    
    [Fact]
    public async Task Get_WithIsDeletedTrue_ReturnsOnlyDeletedDecks()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            PageSize = 10
        };
            
        // Act
        PaginationResult<Deck> result = await _repository.Get(_testUser.Id, request, true);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count); // Only deleted decks
        //Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.PageSize);
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }
        
    [Fact]
    public async Task GetById_WithValidId_ReturnsDeck()
    {
        // Arrange
        const int deckId = 1;
            
        // Act
        Deck? result = await _repository.Get(_testUser.Id, deckId);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(deckId, result.Id);
        Assert.Equal(_testUser.Id, result.CreatorId);
    }
    
    
    [Fact]
    public async Task GetById_WithDeletedId_ReturnsNull()
    {
        // Arrange
        const int deckId = 1;
        await Context.Decks.Where(x => x.Id == deckId).ExecuteDeleteAsync();
        
        // Act
        Deck? result = await _repository.Get(_testUser.Id, deckId);
        
        // Assert
        Assert.Null(result);
        // Assert.Equal(deckId, result.Id);
        // Assert.Equal(_testUser.IdId, result.CreatorId);
    }
    
    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNull()
    {
        // Arrange
        const int deckId = 999;
            
        // Act
        Deck? result = await _repository.Get(_testUser.Id, deckId);
            
        // Assert
        Assert.Null(result);
    }
        
    [Fact]
    public async Task Create_WithValidRequest_CreatesNewDeck()
    {
        // Arrange
        DeckRequest request = new()
        {
            Name = "New Test Deck",
            Description = "Description for new test deck",
            IsPublic = true,
            IsUserOption = false,
            OptionRequest = new DeckOptionRequest
            {
                NewCardsPerDay = 10,
                ReviewLimitPerDay = 50,
                SortOrder = DeckOptionSortOrder.Alphabetical,
                InterdayLearningMix = false
            }
        };
            
        // Act
        Deck? result = await _repository.Create(_testUser.Id, request);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.IsPublic, result.IsPublic);
        Assert.Equal(_testUser.Id, result.CreatorId);
        Assert.Equal(request.OptionRequest.NewCardsPerDay, result.Option.NewCardsPerDay);
            
        // Verify deck was saved to database
        Deck? savedDeck = await Context.Decks.FindAsync(result.Id);
        Assert.NotNull(savedDeck);
        Assert.Equal(request.Name, savedDeck.Name);
    }
        
    [Fact]
    public async Task Create_WithUserOption_CreatesNewDeckWithUserOptions()
    {
        // Arrange
        DeckRequest request = new()
        {
            Name = "New Test Deck with User Options",
            Description = "Description for new test deck with user options",
            IsPublic = false,
            IsUserOption = true
        };
            
        // Act
        Deck? result = await _repository.Create(_testUser.Id, request);
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.IsPublic, result.IsPublic);
        Assert.Equal(_testUser.Id, result.CreatorId);
            
        // Verify deck was saved to database with user options
        Deck? savedDeck = await Context.Decks.FindAsync(result.Id);
        Assert.NotNull(savedDeck);
        Assert.Equal(request.Name, savedDeck.Name);
        Assert.Equal(_testUser.DeckOption.NewCardsPerDay, savedDeck.Option.NewCardsPerDay); // From seeded user
        Assert.Equal(_testUser.DeckOption.ReviewLimitPerDay, savedDeck.Option.ReviewLimitPerDay); // From seeded user
    }
        
    [Fact]
    public async Task Create_WithInvalidUserOption_ReturnsNull()
    {
        // Arrange
        string invalidUserId = "invalid-user-id";
        DeckRequest request = new()
        {
            Name = "New Test Deck",
            Description = "Description for new test deck",
            IsPublic = true,
            IsUserOption = true
        };
            
        // Act
        Deck? result = await _repository.Create(invalidUserId, request);
            
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task Create_WithInvalidUserOption_ThrowsInvalidOperationException()
    {
        // Arrange
        DeckRequest request = new()
        {
            Name = "New Test Deck",
            Description = "Description for new test deck",
            IsPublic = true,
            IsUserOption = true
        };

        // First create succeeds
        await _repository.Create(_testUser.Id, request);

        // Act & Assert: second create should throw
        await Assert.ThrowsAsync<DbUpdateException>(
            async () => await _repository.Create(_testUser.Id, request)
        );
    }

        
    [Fact]
    public async Task Update_WithValidRequest_UpdatesDeck()
    {
        // Arrange
        const int deckId = 1;
        DeckRequest request = new()
        {
            Name = "Updated Test Deck",
            Description = "Updated description for test deck",
            IsPublic = false,
            IsUserOption = false,
            OptionRequest = new DeckOptionRequest
            {
                NewCardsPerDay = 15,
                ReviewLimitPerDay = 75,
                SortOrder = DeckOptionSortOrder.Random,
                InterdayLearningMix = true
            }
        };
            
        // Act
        int? result = await _repository.Update(deckId, _testUser.Id, request);
            
        // Assert
        Assert.Equal(1, result); // One row affected
            
        // Verify deck was updated in database
        Deck? updatedDeck = await Context.Decks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == deckId);
        Assert.NotNull(updatedDeck);
        Assert.Equal(request.Name, updatedDeck.Name);
        Assert.Equal(request.Description, updatedDeck.Description);
        Assert.Equal(request.IsPublic, updatedDeck.IsPublic);
        Assert.Equal(request.OptionRequest.NewCardsPerDay, updatedDeck.Option.NewCardsPerDay);
    }
        
    [Fact]
    public async Task Update_WithInvalidId_ReturnsZero()
    {
        // Arrange
        const int deckId = 999;
        DeckRequest request = new()
        {
            Name = "Updated Test Deck",
            Description = "Updated description for test deck",
            IsPublic = false,
            IsUserOption = false,
            OptionRequest = new DeckOptionRequest
            {
                NewCardsPerDay = 15,
                ReviewLimitPerDay = 75,
                SortOrder = DeckOptionSortOrder.DateAdded,
                InterdayLearningMix = true
            }
        };
            
        // Act
        int? result = await _repository.Update(deckId, _testUser.Id, request);
            
        // Assert
        Assert.Equal(0, result); // No rows affected
    }
        
    [Fact]
    public async Task Delete_WithValidId_DeletesDeck()
    {
        // Arrange
        const int deckId = 1;
            
        // Act
        int result = await _repository.Delete(deckId, _testUser.Id);
            
        // Assert
        Assert.Equal(1, result); // One row affected
            
        // Verify deck was deleted from database
        Deck? deletedDeck = await Context.Decks.FirstOrDefaultAsync(x => x.Id == deckId);
        Assert.Null(deletedDeck);
    }
        
    [Fact]
    public async Task Delete_WithInvalidId_ReturnsZero()
    {
        // Arrange
        const int deckId = 999;
            
        // Act
        int result = await _repository.Delete(deckId, _testUser.Id);
            
        // Assert
        Assert.Equal(0, result); // No rows affected
    }
        
    [Fact]
    public async Task Restore_WithValidId_RestoresDeletedDeck()
    {
        // Arrange
        const int deckId = 8; // This is a deleted deck from our seed data
            
        // Act
        int result = await _repository.Restore(deckId, _testUser.Id);
            
        // Assert
        Assert.Equal(1, result); // One row affected
            
        // Verify deck was restored in database
        Deck? restoredDeck = await Context.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deckId);
        Assert.NotNull(restoredDeck);
        Assert.False(restoredDeck.IsDeleted);
    }
        
    [Fact]
    public async Task Restore_WithInvalidId_ReturnsZero()
    {
        // Arrange
        const int deckId = 999;
            
        // Act
        int result = await _repository.Restore(deckId, _testUser.Id);
            
        // Assert
        Assert.Equal(0, result); // No rows affected
    }
}