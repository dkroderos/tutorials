using Ctf.Api.Features.Solves;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Solves;
using Ctf.Api.Repositories.TeamMembers;

namespace Ctf.Api.UnitTests.Features.Solves;

public sealed class GetChallengeStatusTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IRoomRepository>,
        Mock<ITeamMemberRepository>,
        Mock<ISolveRepository>,
        GetChallengeStatus.Handler,
        GetChallengeStatus.Query
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var rr = new Mock<IRoomRepository>();
        var tmr = new Mock<ITeamMemberRepository>();
        var sr = new Mock<ISolveRepository>();

        var h = new GetChallengeStatus.Handler(
            cr.Object,
            rmr.Object,
            rr.Object,
            tmr.Object,
            sr.Object
        );
        var c = new GetChallengeStatus.Query(It.IsAny<Guid>(), It.IsAny<Guid>());

        return (cr, rmr, rr, tmr, sr, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomDoesNoExistsAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();

        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Never);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomSolveRequirementsDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();

        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((RoomSolveRequirementsDto?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenChallengesAreHiddenAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = true,
                    IsSubmissionsForceDisabled = It.IsAny<bool>(),
                    SubmissionStart = It.IsAny<DateTime>(),
                    SubmissionEnd = It.IsAny<DateTime>(),
                }
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = It.IsAny<bool>(),
                    SubmissionStart = It.IsAny<DateTime>(),
                    SubmissionEnd = It.IsAny<DateTime>(),
                }
            );
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Owner)]
    [InlineData(RoomRole.Admin)]
    [InlineData(RoomRole.Editor)]
    public async Task Handle_Should_ReturnNotAPlayer_WhenUserIsNotPlayerAsync(RoomRole roomRole)
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(roomRole);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = false,
                    SubmissionStart = It.IsAny<DateTime>(),
                    SubmissionEnd = It.IsAny<DateTime>(),
                }
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.NotAPlayer);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnDisabled_WhenSubmissionsAreForceDisabledAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = true,
                    SubmissionStart = It.IsAny<DateTime>(),
                    SubmissionEnd = It.IsAny<DateTime>(),
                }
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.Disabled);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnDisabled_WhenSubmissionsHaveNotStartedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = false,
                    SubmissionStart = DateTime.UtcNow.AddHours(byte.MaxValue),
                    SubmissionEnd = DateTime.UtcNow.AddHours(byte.MaxValue),
                }
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.Disabled);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnDisabled_WhenSubmissionsHaveEndedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = false,
                    SubmissionStart = DateTime.UtcNow.AddHours(-byte.MaxValue),
                    SubmissionEnd = DateTime.UtcNow.AddHours(-byte.MaxValue),
                }
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.Disabled);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNoTeam_WhenUserDoesNotHaveATeamAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, c) = Init();
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = false,
                    SubmissionStart = DateTime.UtcNow.AddHours(-byte.MaxValue),
                    SubmissionEnd = DateTime.UtcNow.AddHours(byte.MaxValue),
                }
            );
        tmr.Setup(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.NoTeam);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, c.UserId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadySolved_WhenChallengeIsAlreadySolvedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, _) = Init();
        var teamId = Guid.NewGuid();
        var c = new GetChallengeStatus.Query(It.IsAny<Guid>(), It.IsAny<Guid>());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = false,
                    SubmissionStart = DateTime.UtcNow.AddHours(-byte.MaxValue),
                    SubmissionEnd = DateTime.UtcNow.AddHours(byte.MaxValue),
                }
            );
        tmr.Setup(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(teamId);
        sr.Setup(r => r.ExistsAsync(c.ChallengeId, teamId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.AlreadySolved);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, teamId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotSolved_WhenChallengeNotSolvedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, sr, h, _) = Init();
        var teamId = Guid.NewGuid();
        var c = new GetChallengeStatus.Query(It.IsAny<Guid>(), It.IsAny<Guid>());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(
                new RoomSolveRequirementsDto
                {
                    AreChallengesHidden = false,
                    IsSubmissionsForceDisabled = false,
                    SubmissionStart = DateTime.UtcNow.AddHours(-byte.MaxValue),
                    SubmissionEnd = DateTime.UtcNow.AddHours(byte.MaxValue),
                }
            );
        tmr.Setup(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(teamId);
        sr.Setup(r => r.ExistsAsync(c.ChallengeId, teamId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(GetChallengeStatus.ChallengeStatus.NotSolved);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, teamId), Times.Once);
    }
}
