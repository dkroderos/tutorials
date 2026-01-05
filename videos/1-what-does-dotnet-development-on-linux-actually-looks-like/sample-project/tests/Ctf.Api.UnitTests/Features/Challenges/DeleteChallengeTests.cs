using Ctf.Api.Features.Challenges;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Challenges;

public sealed class DeleteChallengeTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        DeleteChallenge.Handler,
        DeleteChallenge.Command
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();

        var h = new DeleteChallenge.Handler(rmr.Object, cr.Object);
        var c = new DeleteChallenge.Command(It.IsAny<Guid>(), It.IsAny<Guid>());

        return (cr, rmr, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomIdDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();

        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        cr.Verify(r => r.DeleteAsync(c.Id), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        cr.Verify(r => r.DeleteAsync(c.Id), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsPlayerAsync()
    {
        // Arrange
        var (cr, rmr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an editor in this room to modify challenges.");
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        cr.Verify(r => r.DeleteAsync(c.Id), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenChallengeDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        cr.Setup(r => r.DeleteAsync(c.Id)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.DeleteAsync(c.Id), Times.Once);
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
