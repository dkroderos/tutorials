using Ctf.Api.Features.Teams;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.Teams;

public sealed class GetTeamsTests
{
    private static (
        Mock<ITeamRepository>,
        Mock<IRoomMemberRepository>,
        GetTeams.Handler,
        GetTeams.Query
    ) Init()
    {
        var tr = new Mock<ITeamRepository>();
        var rmr = new Mock<IRoomMemberRepository>();

        var h = new GetTeams.Handler(rmr.Object, tr.Object);
        var q = new GetTeams.Query(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        );

        return (tr, rmr, h, q);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (tr, rmr, h, q) = Init();
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId), Times.Once);
        tr.Verify(
            r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_GetTeamsAsync()
    {
        // Arrange
        var (tr, rmr, h, q) = Init();
        rmr.Setup(r => r.ExistsAsync(q.RoomId, q.UserId)).ReturnsAsync(true);
        tr.Setup(r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(PagedList<TeamDto>.Empty());

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        tr.Verify(
            r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            Times.Once
        );
        rmr.Verify(r => r.ExistsAsync(q.RoomId, q.UserId), Times.Once);
    }
}
