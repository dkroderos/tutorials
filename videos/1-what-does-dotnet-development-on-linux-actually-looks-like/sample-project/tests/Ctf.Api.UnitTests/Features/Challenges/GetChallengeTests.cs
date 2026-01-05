using Ctf.Api.Features.Challenges;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Challenges;

public sealed class GetChallengeTests
{
    private static (
        Mock<IRoomMemberRepository>,
        Mock<IRoomRepository>,
        Mock<IChallengeRepository>,
        GetChallenge.Handler,
        GetChallenge.Query,
        ChallengeDetailsDto
    ) Init()
    {
        var rmr = new Mock<IRoomMemberRepository>();
        var rr = new Mock<IRoomRepository>();
        var cr = new Mock<IChallengeRepository>();

        var h = new GetChallenge.Handler(rmr.Object, rr.Object, cr.Object);
        var q = new GetChallenge.Query(It.IsAny<Guid>(), It.IsAny<Guid>());
        var d = new ChallengeDetailsDto
        {
            Id = It.IsAny<Guid>(),
            RoomId = It.IsAny<Guid>(),
            RoomName = It.IsAny<string>(),
            Name = It.IsAny<string>(),
            Description = It.IsAny<string>(),
            MaxAttempts = It.IsAny<int>(),
            CreatorId = It.IsAny<Guid>(),
            CreatorUsername = It.IsAny<string>(),
            CreatedAt = It.IsAny<DateTime>(),
            UpdaterId = It.IsAny<Guid>(),
            UpdaterUsername = It.IsAny<string>(),
            UpdatedAt = It.IsAny<DateTime>(),
            Artifacts = [],
            FlagsCount = It.IsAny<int>(),
            Tags = [],
        };

        return (rmr, rr, cr, h, q, d);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenChallengeDoesNotExistsAsync()
    {
        // Arrange
        var (rmr, rr, cr, h, q, _) = Init();
        cr.Setup(r => r.GetDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ChallengeDetailsDto?)null);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetDetailsAsync(q.Id), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId), Times.Never);
        rr.Verify(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (rmr, rr, cr, h, q, d) = Init();
        cr.Setup(r => r.GetDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(d);
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetDetailsAsync(q.Id), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId), Times.Once);
        rr.Verify(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenChallengesAreHiddenInRoomAsync()
    {
        // Arrange
        var (rmr, rr, cr, h, q, d) = Init();
        cr.Setup(r => r.GetDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(d);
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId)).ReturnsAsync(true);
        rr.Setup(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId), Times.Once);
        rr.Verify(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Get_WhenQueryIsValidAsync()
    {
        // Arrange
        var (rmr, rr, cr, h, q, d) = Init();
        cr.Setup(r => r.GetDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(d);
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId)).ReturnsAsync(true);
        rr.Setup(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        cr.Verify(r => r.GetDetailsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId), Times.Once);
    }
}
