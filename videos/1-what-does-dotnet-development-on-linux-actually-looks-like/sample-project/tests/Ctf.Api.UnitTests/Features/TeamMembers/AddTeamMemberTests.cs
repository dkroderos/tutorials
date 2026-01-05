using Ctf.Api.Features.TeamMembers;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.TeamMembers;

public sealed class AddTeamMemberTests
{
    private static (
        Mock<ITeamRepository>,
        Mock<IRoomMemberRepository>,
        Mock<ITeamMemberRepository>,
        AddTeamMember.Handler,
        AddTeamMember.Command
    ) Init()
    {
        var tr = new Mock<ITeamRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var tmr = new Mock<ITeamMemberRepository>();

        var h = new AddTeamMember.Handler(tr.Object, rmr.Object, tmr.Object);
        var c = new AddTeamMember.Command(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        return (tr, rmr, tmr, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTeamDoesNotExistsAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Never);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenAdderIsNotInRoomAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Team was not found.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Player)]
    [InlineData(RoomRole.Editor)]
    public async Task Handle_Should_ReturnForbidden_WhenAdderRoleIsNotAdminAsync(RoomRole adderRole)
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId)).ReturnsAsync(adderRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an admin in this room to modify teams.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenCandidateIsNotInRoomAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId))
            .ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("The user is not a member in the room.");
        tr.Verify(r => r.GetRoomIdAsync(c.TeamId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Editor)]
    [InlineData(RoomRole.Admin)]
    [InlineData(RoomRole.Owner)]
    public async Task Handle_Should_ReturnForbidden_WhenCandidateRoleIsNotPlayerAsync(
        RoomRole candidateRole
    )
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId)).ReturnsAsync(candidateRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should()
            .Be("The candidate must have a player role in order to be added in a team.");
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Never);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenCandidateAlreadyHasTeamInRoomAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId))
            .ReturnsAsync(RoomRole.Player);
        tmr.Setup(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("The player is already has a team.");
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Once);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_WhenCommandIsValidAsync()
    {
        // Arrange
        var (tr, rmr, tmr, h, c) = Init();
        tr.Setup(r => r.GetRoomIdAsync(c.TeamId)).ReturnsAsync(Guid.NewGuid());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId))
            .ReturnsAsync(RoomRole.Player);
        tmr.Setup(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId))
            .ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.AdderId), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.CandidateId), Times.Once);
        tmr.Verify(r => r.GetUserTeamAsync(It.IsAny<Guid>(), c.CandidateId), Times.Once);
        tmr.Verify(r => r.AddAsync(It.IsAny<AddTeamMemberDto>()), Times.Once);
    }
}
