using Tools.ApiCore.Modules.Users.Domain;

namespace Tools.ApiCore.Modules.Users.Application;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync();

    Task<IReadOnlyList<User>> GetAllNative();

    Task<User> CreateAsync(User user);

    Task<User> CreateNative(User user);
}
