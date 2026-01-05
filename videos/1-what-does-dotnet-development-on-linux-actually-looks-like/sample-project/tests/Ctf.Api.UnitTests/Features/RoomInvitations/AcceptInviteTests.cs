using Ctf.Api.Features.RoomInvitations;
using Ctf.Api.Repositories.RoomIntivations;
using Ctf.Api.Repositories.RoomMembers;

namespace Ctf.Api.UnitTests.Features.RoomInvitations;

public sealed class AcceptInviteTests
{
    private static (
        Mock<IRoomInvitationRepository>,
        Mock<IRoomMemberRepository>,
        AcceptInvite.Handler,
        AcceptInvite.Command
    ) Init()
    {
        var rir = new Mock<IRoomInvitationRepository>();
        var rmr = new Mock<IRoomMemberRepository>();

        var h = new AcceptInvite.Handler(rir.Object, rmr.Object);
        var c = new AcceptInvite.Command(It.IsAny<Guid>(), It.IsAny<Guid>());

        return (rir, rmr, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenAlreadyMemberAsync()
    {
        // Arrange
        var (rir, rmr, h, c) = Init();

        rmr.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(true);
        rir.Setup(r => r.DeleteAsync(c.RoomId, c.InviteeId));

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("You are already a member of the room.");
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rir.Verify(r => r.DeleteAsync(c.RoomId, c.InviteeId), Times.Once);
        rir.Verify(r => r.AcceptAsync(c.RoomId, c.InviteeId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenAcceptingFailedAsync()
    {
        // Arrange
        var (rir, rmr, h, c) = Init();

        rmr.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);
        rir.Setup(r => r.AcceptAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Invitation was not found.");
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rir.Verify(r => r.DeleteAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.AcceptAsync(c.RoomId, c.InviteeId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Accept_WhenCommandIsValidAsync()
    {
        // Arrange
        var (rir, rmr, h, c) = Init();

        rmr.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);
        rir.Setup(r => r.AcceptAsync(c.RoomId, c.InviteeId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rir.Verify(r => r.DeleteAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.AcceptAsync(c.RoomId, c.InviteeId), Times.Once);
    }
}
