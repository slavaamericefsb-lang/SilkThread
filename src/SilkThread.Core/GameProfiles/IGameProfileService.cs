using System.Collections.Generic;
using System.Threading.Tasks;

namespace SilkThread.Core.GameProfiles;

public interface IGameProfileService
{
    IReadOnlyList<GameProfile> GetAllProfiles();
    GameProfile? GetProfileById(string id);
    bool IsProfileApplied(string id);
    Task<bool> ApplyProfileAsync(string id);
    Task<bool> RevertProfileAsync(string id);
}
