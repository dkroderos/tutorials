using Ctf.Api.Features.Solves;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.Flags;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Solves;
using Ctf.Api.Repositories.TeamMembers;

namespace Ctf.Api.UnitTests.Features.Solves;

public sealed class SubmitFlagTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IRoomRepository>,
        Mock<ITeamMemberRepository>,
        Mock<IFlagRepository>,
        Mock<ISolveRepository>,
        Mock<IValidator<SubmitFlag.Command>>,
        SubmitFlag.Handler,
        SubmitFlag.Command
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var rr = new Mock<IRoomRepository>();
        var tmr = new Mock<ITeamMemberRepository>();
        var fr = new Mock<IFlagRepository>();
        var sr = new Mock<ISolveRepository>();
        var v = new Mock<IValidator<SubmitFlag.Command>>();

        var h = new SubmitFlag.Handler(
            cr.Object,
            rmr.Object,
            rr.Object,
            tmr.Object,
            fr.Object,
            sr.Object,
            v.Object
        );
        var c = new SubmitFlag.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>());

        return (cr, rmr, rr, tmr, fr, sr, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsInvalidAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default))
            .ReturnsAsync(
                new ValidationResult(
                    [new ValidationFailure(It.IsAny<string>(), It.IsAny<string>())]
                )
            );

        // Act
        var r = await h.Handle(c);

        // Assert
        r.IsSuccess.Should().BeFalse();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Never);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomDoesNoExistsAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Owner)]
    [InlineData(RoomRole.Admin)]
    [InlineData(RoomRole.Editor)]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsNotPlayerAsync(RoomRole roomRole)
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(roomRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an player in this room to submit flags.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomSolveRequirementsDoesNotExistAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((RoomSolveRequirementsDto?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenChallengesAreHiddenAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenSubmissionsAreForceDisabledAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("Submissions are disabled in this room.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenSubmissionsHaveNotStartedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("Submissions are disabled in this room.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenSubmissionsHaveEndedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("Submissions are disabled in this room.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, It.IsAny<Guid>()), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserDoesNotHaveATeamAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("You need to be in a team in order to solve challenges.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, c.UserId), Times.Never);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenChallengeIsAlreadySolvedAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, _) = Init();
        var teamId = Guid.NewGuid();
        var c = new SubmitFlag.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), "incorrect");
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("You already solved this challenge.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, teamId), Times.Once);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Never);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnIncorrect_WhenFlagIsIncorrectAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, _) = Init();
        var teamId = Guid.NewGuid();
        var c = new SubmitFlag.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), "incorrect");
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        fr.Setup(r => r.GetByChallengeIdAsync(c.ChallengeId)).ReturnsAsync(["correct"]);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(SubmitFlag.FlagStatus.Incorrect);
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, teamId), Times.Once);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Once);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrect_WhenFlagIsCorrectAsync()
    {
        // Arrange
        var (cr, rmr, rr, tmr, fr, sr, v, h, _) = Init();
        var teamId = Guid.NewGuid();
        var c = new SubmitFlag.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), "correct");
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
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
        fr.Setup(r => r.GetByChallengeIdAsync(c.ChallengeId)).ReturnsAsync(["correct"]);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        a.Value.Status.Should().Be(SubmitFlag.FlagStatus.Correct);
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        rr.Verify(r => r.GetRoomSolveRequirementsAsync(It.IsAny<Guid>()), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        sr.Verify(r => r.ExistsAsync(c.ChallengeId, teamId), Times.Once);
        fr.Verify(r => r.GetByChallengeIdAsync(c.ChallengeId), Times.Once);
        sr.Verify(r => r.CreateAsync(It.IsAny<CreateSolveDto>()), Times.Once);
    }
}
