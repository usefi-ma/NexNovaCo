namespace NexNovaCo.Web.Data;

// Independent of collection membership: Admin deletion must never reset initialization.
public sealed class TestimonialInitializationState
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
}
