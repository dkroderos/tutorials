using Ctf.Api.Features.Artifacts;
using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Artifacts;

public sealed class DeleteArtifactTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IArtifactRepository>,
        DeleteArtifact.Handler,
        DeleteArtifact.Command
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var ar = new Mock<IArtifactRepository>();

        var h = new DeleteArtifact.Handler(cr.Object, rmr.Object, ar.Object);
        var c = new DeleteArtifact.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>());

        return (cr, rmr, ar, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, ar, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Artifact was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Never);
        ar.Verify(r => r.DeleteAsync(c.ChallengeId, c.FileName), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRemoverIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, ar, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Never);
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Artifact was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        ar.Verify(r => r.DeleteAsync(c.ChallengeId, c.FileName), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenRemoverIsPlayerAsync()
    {
        // Arrange
        var (cr, rmr, ar, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId)).ReturnsAsync(RoomRole.Player);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();

        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an editor in this room to modify challenges.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        ar.Verify(r => r.DeleteAsync(c.ChallengeId, c.FileName), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenArtifactDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, ar, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        ar.Setup(r => r.DeleteAsync(c.ChallengeId, c.FileName)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Artifact was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        ar.Verify(r => r.DeleteAsync(c.ChallengeId, c.FileName), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Delete_WhenCommandIsValidAsync()
    {
        // Arrange
        var (cr, rmr, ar, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        ar.Setup(r => r.DeleteAsync(c.ChallengeId, c.FileName)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.RemoverId), Times.Once);
        ar.Verify(r => r.DeleteAsync(c.ChallengeId, c.FileName), Times.Once);
    }
}
