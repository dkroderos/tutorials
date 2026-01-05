using Ctf.Api.Features.Teams;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.Teams;

public sealed class DeleteTeamTests
{
    private static (
        Mock<ITeamRepository>,
        Mock<IRoomMemberRepository>,
        DeleteTeam.Handler,
        DeleteTeam.Command
    ) Init()
    {
        var tr = new Mock<ITeamRepository>();
        var rmr = new Mock<IRoomMemberRepository>();

        var h = new DeleteTeam.Handler(tr.Object, rmr.Object);
        var c = new DeleteTeam.Command(It.IsAny<Guid>(), It.IsAny<Guid>());

        return (tr, rmr, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomIdDoesNotExistsAsync()
    {
        // Arrange
        var (tr, rmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        tr.Verify(r => r.DeleteAsync(c.Id), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (tr, rmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tr.Verify(r => r.DeleteAsync(c.Id), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Player)]
    [InlineData(RoomRole.Editor)]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsNotAnAdminAsync(RoomRole roomRole)
    {
        // Arrange
        var (tr, rmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(roomRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an admin in this room to modify teams.");
        tr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tr.Verify(r => r.DeleteAsync(c.Id), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTeamDoesNotExistsAsync()
    {
        // Arrange
        var (tr, rmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        tr.Setup(r => r.DeleteAsync(c.Id)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.DeleteAsync(c.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Delete_WhenCommandIsValidAsync()
    {
        // Arrange
        var (cr, rmr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        cr.Setup(r => r.DeleteAsync(c.Id)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        cr.Verify(r => r.DeleteAsync(c.Id), Times.Once);
    }
}
