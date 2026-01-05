using Ctf.Api.Features.RoomInvitations;
using Ctf.Api.Repositories.RoomIntivations;

namespace Ctf.Api.UnitTests.Features.RoomInvitations;

public sealed class RejectInviteTests
{
    private static (
        Mock<IRoomInvitationRepository>,
        RejectInvite.Handler,
        RejectInvite.Command
    ) Init()
    {
        var r = new Mock<IRoomInvitationRepository>();

        var h = new RejectInvite.Handler(r.Object);
        var c = new RejectInvite.Command(It.IsAny<Guid>(), It.IsAny<Guid>());

        return (r, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomInvitationDoesNotExistsAsync()
    {
        // Arrange
        var (r, h, c) = Init();
        r.Setup(r => r.DeleteAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Invitation was not found.");
        ;
        r.Verify(r => r.DeleteAsync(c.RoomId, c.InviteeId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RejectInvitationAsync()
    {
        // Arrange
        var (r, h, c) = Init();
        r.Setup(r => r.DeleteAsync(c.RoomId, c.InviteeId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        r.Verify(r => r.DeleteAsync(c.RoomId, c.InviteeId), Times.Once);
    }
}
