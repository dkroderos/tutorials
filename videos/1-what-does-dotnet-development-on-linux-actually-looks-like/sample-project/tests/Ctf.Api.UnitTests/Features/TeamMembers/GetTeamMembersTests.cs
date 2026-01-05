using Ctf.Api.Features.TeamMembers;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.TeamMembers;

public sealed class GetTeamMembersTests
{
    private static (
        Mock<ITeamRepository>,
        Mock<IRoomMemberRepository>,
        Mock<ITeamMemberRepository>,
        GetTeamMembers.Handler,
        GetTeamMembers.Query
    ) Init()
    {
        var tr = new Mock<ITeamRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var tmr = new Mock<ITeamMemberRepository>();

        var h = new GetTeamMembers.Handler(tr.Object, rmr.Object, tmr.Object);
        var c = new GetTeamMembers.Query(Guid.NewGuid(), Guid.NewGuid());

        return (tr, rmr, tmr, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTeamDoesNotExistsAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        tmr.Verify(r => r.QueryAsync(c.TeamId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.QueryAsync(c.TeamId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Query_WhenCommandIsValidAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        tmr.Setup(r => r.QueryAsync(c.TeamId)).ReturnsAsync([]);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);
        tmr.Verify(r => r.QueryAsync(c.TeamId), Times.Once);
    }
}
