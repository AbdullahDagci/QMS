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
}
