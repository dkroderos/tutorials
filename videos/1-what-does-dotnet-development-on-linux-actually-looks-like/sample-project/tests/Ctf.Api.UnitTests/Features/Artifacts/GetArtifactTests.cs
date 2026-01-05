using System.Text;
using Ctf.Api.Features.Artifacts;
using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;

namespace Ctf.Api.UnitTests.Features.Artifacts;

public sealed class GetArtifactTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IArtifactRepository>,
        Mock<IValidator<GetArtifact.Query>>,
        GetArtifact.Handler,
        GetArtifact.Query
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var ar = new Mock<IArtifactRepository>();
        var v = new Mock<IValidator<GetArtifact.Query>>();

        var h = new GetArtifact.Handler(cr.Object, rmr.Object, ar.Object);
        var c = new GetArtifact.Query(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>());

        return (cr, rmr, ar, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        ar.Verify(r => r.GetStreamAsync(c.ChallengeId, c.FileName), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        ar.Verify(r => r.GetStreamAsync(c.ChallengeId, c.FileName), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenArtifactDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(true);
        ar.Setup(r => r.GetStreamAsync(c.ChallengeId, c.FileName)).ReturnsAsync((Stream?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Artifact was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        ar.Verify(r => r.GetStreamAsync(c.ChallengeId, c.FileName), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Upload_WhenCommandIsValidAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(true);
        ar.Setup(r => r.GetStreamAsync(c.ChallengeId, c.FileName))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(string.Empty)));

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        ar.Verify(r => r.GetStreamAsync(c.ChallengeId, c.FileName), Times.Once);
    }
}
