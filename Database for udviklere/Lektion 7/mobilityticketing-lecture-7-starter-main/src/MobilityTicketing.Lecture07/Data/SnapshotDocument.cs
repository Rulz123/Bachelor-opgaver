using Microsoft.EntityFrameworkCore;
namespace MobilityTicketing.Lecture07.Data;

public class SnapshotDocument
{
    public string Id { get; set; } = null!;
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string Payload { get; set; } = null!;
}

public partial class LectureDbContext
{
    partial void ConfigureReadModels(ModelBuilder b)
    {
        b.Entity<SnapshotDocument>(e => {
            e.ToTable("published_snapshots");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.FromUtc).HasColumnName("from_utc");
            e.Property(x => x.ToUtc).HasColumnName("to_utc");
            e.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        });
    }
}
