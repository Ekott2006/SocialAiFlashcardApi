using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.Repository;

public class UserRepository(DataContext context)
{
    public async Task UpdateProfileImage(string id, string profileImage) => await context.Users
        .Where(x => x.Id == id)
        .ExecuteUpdateAsync(x => x.SetProperty(y => y.ProfileImageUrl, profileImage));
    
    // public async Task Update(string id, UpdateUserRequest request) => await context.Users
    //     .Where(x => x.Id == id)
    //     .ExecuteUpdateAsync(x => x.SetProperty(y => y.FirstName, request.FirstName).SetProperty(y => y.LastName, request.LastName).SetProperty(y => y.MiddleName, request.MiddleName));

}