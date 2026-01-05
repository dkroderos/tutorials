using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.Flags;
using Ctf.Api.Repositories.RefreshTokens;
using Ctf.Api.Repositories.RoomIntivations;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Solves;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;
using Ctf.Api.Repositories.Users;

namespace Ctf.Api.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped<IChallengeRepository, ChallengeRepository>();
        services.AddScoped<IFlagRepository, FlagRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomInvitationRepository, RoomInvitationRepository>();
        services.AddScoped<IRoomMemberRepository, RoomMemberRepository>();
        services.AddScoped<ISolveRepository, SolveRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
