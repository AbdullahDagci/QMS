namespace Qms.Infrastructure.Persistence.Sequences;

public sealed class RecordNumberSequence
{
    public string RecordType { get; set; } = string.Empty;

    public int CalendarYear { get; set; }

    public long LastValue { get; set; }
}
