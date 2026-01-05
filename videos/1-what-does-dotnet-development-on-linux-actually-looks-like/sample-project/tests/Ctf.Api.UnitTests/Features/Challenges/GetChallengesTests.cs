using Ctf.Api.Features.Challenges;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Challenges;

public sealed class GetChallengesTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IRoomRepository>,
        GetChallenges.Handler,
        GetChallenges.Query
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var rr = new Mock<IRoomRepository>();

        var h = new GetChallenges.Handler(rmr.Object, rr.Object, cr.Object);
        var q = new GetChallenges.Query(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        );

        return (cr, rmr, rr, h, q);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, rr, h, q) = Init();
        rmr.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        rmr.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), q.UserId), Times.Once);
        rr.Verify(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>()), Times.Never);
        cr.Verify(
            r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_GetEmptyChallenges_WhenChallengesAreHiddenAsync()
    {
        // Arrange
        var (cr, rmr, rr, h, q) = Init();
        rmr.Setup(r => r.ExistsAsync(q.RoomId, q.UserId)).ReturnsAsync(true);
        rr.Setup(r => r.AreChallengesHiddenAsync(q.RoomId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        rmr.Verify(r => r.ExistsAsync(q.RoomId, q.UserId), Times.Once);
        rr.Verify(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>()), Times.Once);
        cr.Verify(
            r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_GetChallenges_WhenChallengesAreNotHiddenAsync()
    {
        // Arrange
        var (cr, rmr, rr, h, q) = Init();
        rmr.Setup(r => r.ExistsAsync(q.RoomId, q.UserId)).ReturnsAsync(true);
        rr.Setup(r => r.AreChallengesHiddenAsync(q.RoomId)).ReturnsAsync(false);
        cr.Setup(r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(PagedList<ChallengeDto>.Empty());

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        rmr.Verify(r => r.ExistsAsync(q.RoomId, q.UserId), Times.Once);
        rr.Verify(r => r.AreChallengesHiddenAsync(It.IsAny<Guid>()), Times.Once);
        cr.Verify(
            r =>
                r.QueryAsync(
                    q.RoomId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            Times.Once
        );
    }
}
