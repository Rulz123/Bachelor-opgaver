using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;

public class SnapshotBoard
{
    public SnapshotBoard(string id, string jsonSnapshotData)
    {
        Id = id;
        snapshotData = jsonSnapshotData;
    }
    public string Id { get; set; } = null!;

    public string snapshotData { get; set; } = null!;
}