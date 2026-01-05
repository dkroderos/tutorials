using Ctf.Api.Features.TeamMembers;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.TeamMembers;

public sealed class RemoveTeamMemberTests
{
    private static (
        Mock<ITeamRepository>,
        Mock<IRoomMemberRepository>,
        Mock<ITeamMemberRepository>,
        RemoveTeamMember.Handler,
        RemoveTeamMember.Command
    ) Init()
    {
        var tr = new Mock<ITeamRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var tmr = new Mock<ITeamMemberRepository>();

        var h = new RemoveTeamMember.Handler(tr.Object, rmr.Object, tmr.Object);
        var c = new RemoveTeamMember.Command(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

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
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Never);
        tmr.Verify(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRemoveIsNotInRoomAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        tmr.Verify(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Player)]
    [InlineData(RoomRole.Editor)]
    public async Task Handle_Should_ReturnForbidden_WhenRemoverRoleIsNotAdminAsync(
        RoomRole adderRole
    )
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId)).ReturnsAsync(adderRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an admin in this room to modify teams.");
        ;
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        tmr.Verify(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTargeteUserIsNotInTheTeamAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        tmr.Setup(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()))
            .ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("The user is not in the team.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        tmr.Verify(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Remove_WhenCommandIsValidAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        tmr.Setup(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        tmr.Verify(r => r.RemoveAsync(c.TeamId, c.TargetUserId, It.IsAny<bool>()), Times.Once);
    }
}
