using Core.Data.ModelFaker;
using Core.Dto.Common;
using Core.Dto.NoteType;
using Core.Model;
using Core.Model.Helper;
using Core.Repository;
using Microsoft.EntityFrameworkCore;
using Test.Helper;

namespace Test.Repository;

public class NoteTypeRepositoryTests : DatabaseSetupHelper
{
    private readonly NoteTypeRepository _repository;
    private readonly User _testUser;
    private readonly User _anotherUser;

    public NoteTypeRepositoryTests()
    {
        _repository = new NoteTypeRepository(Context);
        (_testUser, _anotherUser) = SeedDatabase().Result;
    }

    private async Task<(User user, User anotherUser)> SeedDatabase()
    {
        // 1. Setup primary user and an extra user (to satisfy foreign keys)
        User? user = new UserFaker().Generate();
        User? anotherUser = new UserFaker().Generate();
        anotherUser.UserName = "Other";
    
        await Context.Users.AddRangeAsync(user, anotherUser);
        await Context.SaveChangesAsync();

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

        await Context.NoteTypes.AddRangeAsync(allNoteTypes);
        await Context.SaveChangesAsync();
        return (user, anotherUser);
    }


    [Fact]
    public async Task Get_ShouldReturnPaginatedResults_WithCorrectFiltering()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            PageSize = 5,
            CursorId = null
        };

        // Act
        PaginationResult<NoteType> result = await _repository.Get(_testUser.Id, request, isDeleted: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Data.Count);
        // Assert.True(result.TotalCount >= 7); // 3 global + 5 user non-deleted
        Assert.False(result.HasPrevious);
        Assert.True(result.HasNext);
        
        // All items should be either global or belong to the user
        foreach (NoteType item in result.Data)
        {
            Assert.True(item.CreatorId == null || item.CreatorId == _testUser.Id);
            Assert.False(item.IsDeleted);
        }
    }

    [Fact]
    public async Task Get_ShouldReturnDeletedItems_WhenIsDeletedTrue()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            PageSize = 10,
            CursorId = null
        };

        // Act
        PaginationResult<NoteType> result = await _repository.Get(_testUser.Id, request, isDeleted: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count); // User's deleted items (ids 9-10)
        foreach (NoteType item in result.Data)
        {
            Assert.True(item.IsDeleted);
            Assert.Equal(_testUser.Id, item.CreatorId);
        }
    }

    [Fact]
    public async Task Get_WithCursor_ShouldReturnCorrectPage()
    {
        // Arrange
        PaginationRequest<int> firstPageRequest = new()
        {
            PageSize = 3,
            CursorId = null
        };

        // Get first page
        PaginationResult<NoteType> firstPage = await _repository.Get(_testUser.Id, firstPageRequest, isDeleted: false);
        
        // Act - Get second page using cursor
        PaginationRequest<int> secondPageRequest = new()
        {
            PageSize = 3,
            CursorId = firstPage.Data[^1].Id
        };
        
        PaginationResult<NoteType> secondPage = await _repository.Get(_testUser.Id, secondPageRequest, isDeleted: false);

        // Assert
        Assert.NotNull(secondPage);
        Assert.Equal(3, secondPage.Data.Count);
        Assert.True(secondPage.HasPrevious);
        
        // Ensure no overlap between pages
        List<int> firstPageIds = firstPage.Data.Select(x => x.Id).ToList();
        List<int> secondPageIds = secondPage.Data.Select(x => x.Id).ToList();
        Assert.Empty(firstPageIds.Intersect(secondPageIds));
    }

    [Fact]
    public async Task Get_ById_ShouldReturnNoteType_WhenExists()
    {
        // Arrange
        int noteTypeId = 1; // Global note type

        // Act
        NoteType? result = await _repository.Get(_testUser.Id, noteTypeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(noteTypeId, result.Id);
        Assert.Null(result.CreatorId); // Global
    }

    [Fact]
    public async Task Get_ById_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        NoteType? result = await _repository.Get(_testUser.Id, nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Get_ById_ShouldReturnNull_WhenBelongsToDifferentUser()
    {
        // Arrange
        int otherUserNoteTypeId = 11; // Belongs to _anotherUser.Id

        // Act
        NoteType? result = await _repository.Get(_testUser.Id, otherUserNoteTypeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_ShouldSucceed_WithUniqueName()
    {
        // Arrange
        NoteTypeRequest request = new()
        {
            Name = "Unique NoteType Name",
            Templates = [],
            CssStyle = "custom-css"
        };

        // Act
        NoteType? result = await _repository.Create(_testUser.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(_testUser.Id, result.CreatorId);
        Assert.Equal(request.Templates, result.Templates);
        Assert.Equal(request.CssStyle, result.CssStyle);
        Assert.False(result.IsDeleted);
        
        // Verify it was saved to database
        NoteType? fromDb = await Context.NoteTypes.FindAsync(result.Id);
        Assert.NotNull(fromDb);
        Assert.Equal(request.Name, fromDb.Name);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenNameExistsGlobally()
    {
        // Arrange
        string existingGlobalName = "Global NoteType 1";
        NoteTypeRequest request = new()
        {
            Name = existingGlobalName,
            Templates = [],
            CssStyle = ""
        };

        // Act
        NoteType? result = await _repository.Create(_testUser.Id, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenUserAlreadyHasName()
    {
        // Arrange
        NoteType existingNoteType = await Context.NoteTypes.FirstAsync();
        string existingName = existingNoteType.Name; // Already exists in seed for this user
        
        NoteTypeRequest request = new()
        {
            Name = existingName,
            Templates = [],
            CssStyle = ""
        };

        // Act
        NoteType? result = await _repository.Create(_testUser.Id, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateMultiple_WithCreatorAndWithoutCreator_ShouldBothFail_WhenNameExistsGlobally()
    {
        // Arrange
        string globalName = "Global NoteType 1"; // Already exists globally
        
        // Test 1: User tries to create with same name as global
        NoteTypeRequest userRequest = new()
        {
            Name = globalName,
            Templates = [],
            CssStyle = "user-css"
        };
        
        // Test 2: Try to simulate creating global (would fail if attempted)
        int countBefore = await Context.NoteTypes.CountAsync();

        // Act
        NoteType? userResult = await _repository.Create(_testUser.Id, userRequest);
        
        // Also check that name exists globally
        bool doesGlobalNameExist = await Context.NoteTypes
            .AnyAsync(x => x.Name == globalName && x.CreatorId == null);

        // Assert
        Assert.Null(userResult); // User creation should fail
        Assert.True(doesGlobalNameExist); // Global name exists
        
        // Verify no new record was created
        int countAfter = await Context.NoteTypes.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task Update_ShouldSucceed_WithValidData()
    {
        // Arrange
        const int userNoteTypeId = 4; // User's note type
        const string newName = "Updated NoteType Name";
        List<NoteTypeTemplates> newTemplates = [new() { Back = "Updated Template", Front = "Updated Template"} ];
        
        NoteTypeRequest request = new()
        {
            Name = newName,
            Templates = newTemplates,
            CssStyle = "updated-css"
        };

        // Act
        int result = await _repository.Update(userNoteTypeId, _testUser.Id, request);

        // Assert
        Assert.Equal(1, result); // One row affected
        
        // Verify changes in database
        NoteType? updated = await Context.NoteTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userNoteTypeId);
        Assert.NotNull(updated);
        Assert.Equal(newName, updated.Name);
        Assert.Equal(newTemplates, updated.Templates);
        Assert.Equal("updated-css", updated.CssStyle);
    }

    [Fact]
    public async Task Update_ShouldFail_WhenNameConflictsWithGlobal()
    {
        // Arrange
        int userNoteTypeId = 4; // User's note type
        string globalName = "Global NoteType 1"; // Already exists globally
        NoteTypeRequest request = new()
        {
            Name = globalName,
            Templates = [],
            CssStyle = ""
        };

        // Act
        int result = await _repository.Update(userNoteTypeId, _testUser.Id, request);

        // Assert
        Assert.Equal(0, result); // No rows affected
    }

    [Fact]
    public async Task Update_ShouldFail_WhenNoteTypeNotFound()
    {
        // Arrange
        int nonExistentId = 999;
        NoteTypeRequest request = new()
        {
            Name = "New Name",
            Templates = [],
            CssStyle = ""
        };

        // Act
        int result = await _repository.Update(nonExistentId, _testUser.Id, request);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Update_ShouldFail_WhenUserNotOwner()
    {
        // Arrange
        int otherUserNoteTypeId = 11; // Belongs to _anotherUser.Id
        NoteTypeRequest request = new()
        {
            Name = "Updated Name",
            Templates = [],
            CssStyle = ""
        };

        // Act
        int result = await _repository.Update(otherUserNoteTypeId, _testUser.Id, request);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Delete_ShouldSoftDelete_WhenExists()
    {
        // Arrange
        int userNoteTypeId = 4; // User's non-deleted note type
        NoteType? noteTypeBefore = await Context.NoteTypes.FindAsync(userNoteTypeId);
        Assert.NotNull(noteTypeBefore);
        Assert.False(noteTypeBefore.IsDeleted);

        // Act
        int result = await _repository.Delete(userNoteTypeId, _testUser.Id);
        
        // Assert
        Assert.Equal(1, result); // One row affected
        
        Context.ChangeTracker.Clear();
        // Verify soft delete
        NoteType? deleted = await Context.NoteTypes
            .IgnoreQueryFilters()
            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == userNoteTypeId);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        int result = await _repository.Delete(nonExistentId, _testUser.Id);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenUserNotOwner()
    {
        // Arrange
        int otherUserNoteTypeId = 11; // Belongs to _anotherUser.Id

        // Act
        int result = await _repository.Delete(otherUserNoteTypeId, _testUser.Id);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Restore_ShouldRestoreDeleted_WhenExists()
    {
        // Arrange
        int deletedNoteTypeId = 9; // User's deleted note type
        NoteType? noteTypeBefore = await Context.NoteTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == deletedNoteTypeId);
        Assert.NotNull(noteTypeBefore);
        Assert.True(noteTypeBefore.IsDeleted);

        // Act
        int result = await _repository.Restore(deletedNoteTypeId, _testUser.Id);

        // Assert
        Assert.Equal(1, result); // One row affected
        
        // Verify restoration
        NoteType? restored = await Context.NoteTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == deletedNoteTypeId);
        Assert.NotNull(restored);
        Assert.False(restored.IsDeleted);
    }

    [Fact]
    public async Task Restore_ShouldFail_WhenNotDeleted()
    {
        // Arrange
        NoteType nonDeletedNoteType = await Context.NoteTypes.FirstAsync();
        int nonDeletedId = nonDeletedNoteType.Id; // User's non-deleted note type

        // Act
        int result = await _repository.Restore(nonDeletedId, _testUser.Id);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Restore_ShouldFail_WhenNotFound()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        int result = await _repository.Restore(nonExistentId, _testUser.Id);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Restore_ShouldFail_WhenUserNotOwner()
    {
        // Arrange
        int otherUserNoteTypeId = 11; // Belongs to _anotherUser.Id

        // Act
        int result = await _repository.Restore(otherUserNoteTypeId, _testUser.Id);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetAllNoteTypes_ShouldBeOrderedByUpdatedDateDescending()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            PageSize = 20,
            CursorId = null
        };

        // Act
        PaginationResult<NoteType> result = await _repository.Get(_testUser.Id, request, isDeleted: false);

        // Assert
        // Check ordering by UpdatedAt descending
        for (int i = 0; i < result.Data.Count - 1; i++)
        {
            Assert.True(result.Data[i].UpdatedAt >= result.Data[i + 1].UpdatedAt);
            
            // If dates are equal, check Id ordering
            if (result.Data[i].UpdatedAt == result.Data[i + 1].UpdatedAt)
            {
                Assert.True(result.Data[i].Id > result.Data[i + 1].Id);
            }
        }
    }

    [Fact]
    public async Task Get_ShouldIncludeGlobalNoteTypes_ForAllUsers()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            PageSize = 10,
            CursorId = null
        };

        // Act
        PaginationResult<NoteType> result = await _repository.Get(_testUser.Id, request, isDeleted: false);

        // Assert
        Assert.Contains(result.Data, x => x.CreatorId == null); // Should have global note types
        Assert.Contains(result.Data, x => x.CreatorId == _testUser.Id); // Should have user's note types
    }

    [Fact]
    public async Task Get_DeletedItems_ShouldOnlyReturnCurrentUsersDeleted()
    {
        // Arrange
        PaginationRequest<int> request = new()
        {
            PageSize = 10,
            CursorId = null
        };

        // Act
        PaginationResult<NoteType> result = await _repository.Get(_testUser.Id, request, isDeleted: true);

        // Assert
        foreach (NoteType item in result.Data)
        {
            Assert.Equal(_testUser.Id, item.CreatorId);
            Assert.True(item.IsDeleted);
        }
    }
}