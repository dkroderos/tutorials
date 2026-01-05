using Ctf.Api.Features.Artifacts;
using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Artifacts;

public sealed class AddArtifactTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IArtifactRepository>,
        Mock<IValidator<AddArtifact.Command>>,
        AddArtifact.Handler,
        AddArtifact.Command
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var ar = new Mock<IArtifactRepository>();
        var v = new Mock<IValidator<AddArtifact.Command>>();

        var h = new AddArtifact.Handler(cr.Object, rmr.Object, ar.Object, v.Object);
        var c = new AddArtifact.Command(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<Stream>()
        );

        return (cr, rmr, ar, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsInvalidAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
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
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId), Times.Never);
        ar.Verify(r => r.ExistsAsync(c.ChallengeId, c.FileName), Times.Never);
        ar.Verify(r => r.AddAsync(It.IsAny<AddArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
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
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId), Times.Never);
        ar.Verify(r => r.ExistsAsync(c.ChallengeId, c.FileName), Times.Never);
        ar.Verify(r => r.AddAsync(It.IsAny<AddArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUploaderIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Never);
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId))
            .ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId), Times.Once);
        ar.Verify(r => r.ExistsAsync(c.ChallengeId, c.FileName), Times.Never);
        ar.Verify(r => r.AddAsync(It.IsAny<AddArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUploaderIsPlayerAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId))
            .ReturnsAsync(RoomRole.Player);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an editor in this room to modify challenges.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId), Times.Once);
        ar.Verify(r => r.ExistsAsync(c.ChallengeId, c.FileName), Times.Never);
        ar.Verify(r => r.AddAsync(It.IsAny<AddArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenFileNameAlreadyExistsInRoomAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        ar.Setup(r => r.ExistsAsync(c.ChallengeId, c.FileName)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("The file name already exists in the challenge.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId), Times.Once);
        ar.Verify(r => r.ExistsAsync(c.ChallengeId, c.FileName), Times.Once);
        ar.Verify(r => r.AddAsync(It.IsAny<AddArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Upload_WhenCommandIsValidAsync()
    {
        // Arrange
        var (cr, rmr, ar, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.ChallengeId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        ar.Setup(r => r.ExistsAsync(c.ChallengeId, c.FileName)).ReturnsAsync(false);
        ar.Setup(r => r.AddAsync(It.IsAny<AddArtifactDto>()));

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.ChallengeId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UploaderId), Times.Once);
        ar.Verify(r => r.ExistsAsync(c.ChallengeId, c.FileName), Times.Once);
        ar.Verify(r => r.AddAsync(It.IsAny<AddArtifactDto>()), Times.Once);
    }
}
