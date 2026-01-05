using Ctf.Api.Features.RoomMembers;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.RoomMembers;

public sealed class LeaveRoomTests
{
    private static (Mock<IRoomMemberRepository>, LeaveRoom.Handler, LeaveRoom.Command) Init()
    {
        var r = new Mock<IRoomMemberRepository>();

        var h = new LeaveRoom.Handler(r.Object);
        var c = new LeaveRoom.Command(It.IsAny<Guid>(), It.IsAny<Guid>());

        return (r, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (r, h, c) = Init();
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.UserId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIsOwnerAsync()
    {
        // Arrange
        var (r, h, c) = Init();
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(RoomRole.Owner);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("The owner cannot leave the room.");
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.UserId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserFailedToRemoveInRoomAsync()
    {
        // Arrange
        var (r, h, c) = Init();
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(It.IsAny<RoomRole>());
        r.Setup(r => r.DeleteAsync(c.RoomId, c.UserId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.UserId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RejectInvitationAsync()
    {
        // Arrange
        var (r, h, c) = Init();
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(It.IsAny<RoomRole>());
        r.Setup(r => r.DeleteAsync(c.RoomId, c.UserId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.UserId), Times.Once);
    }
}
