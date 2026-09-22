using Qms.Domain.Workflows;

namespace Qms.Domain.Tests.Workflows;

public sealed class WorkflowTaskAssignmentTests
{
    [Fact]
    public void Complete_PreservesAssignmentAndClosesTaskChronologically()
    {
        var assignedAt = new DateTimeOffset(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);
        var completedAt = assignedAt.AddHours(2);
        var assigneeId = Guid.NewGuid();
        var task = WorkflowTaskAssignment.Create(
            "Deviation",
            Guid.NewGuid(),
            "Investigator",
            assigneeId,
            Guid.NewGuid(),
            assignedAt,
            assignedAt.AddDays(2));

        task.Complete(completedAt);

        Assert.Equal(WorkflowTaskStatus.Completed, task.Status);
        Assert.Equal(assigneeId, task.AssignedUserId);
        Assert.Equal(completedAt, task.CompletedAtUtc);
    }

    [Fact]
    public void Create_RejectsTaskWithoutAggregateOrRole()
    {
        Assert.Throws<ArgumentException>(() => WorkflowTaskAssignment.Create(
            "",
            Guid.NewGuid(),
            "Investigator",
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => WorkflowTaskAssignment.Create(
            "Deviation",
            Guid.NewGuid(),
            " ",
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Escalate_TracksLevelAndRejectsPrematureOrClosedTasks()
    {
        var now = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero);
        var task = WorkflowTaskAssignment.Create(
            "Capa",
            Guid.NewGuid(),
            "ActionOwner",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddDays(-2),
            now.AddMinutes(-1));

        Assert.Equal(1, task.Escalate(now));
        Assert.Equal(2, task.Escalate(now.AddDays(1)));
        Assert.Equal(now.AddDays(1), task.LastEscalatedAtUtc);

        task.Complete(now.AddDays(1).AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => task.Escalate(now.AddDays(2)));

        var futureTask = WorkflowTaskAssignment.Create(
            "Capa", Guid.NewGuid(), "ActionOwner", Guid.NewGuid(), null, now, now.AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => futureTask.Escalate(now));
    }
}
