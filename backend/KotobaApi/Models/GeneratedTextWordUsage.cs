namespace KotobaApi.Models;

public class GeneratedTextWordUsage
{
    public Guid Id { get; set; }
    public Guid GeneratedTextId { get; set; }
    public Guid WordId { get; set; }
    public int Occurrences { get; set; }

    public GeneratedText GeneratedText { get; set; } = null!;
    public Word Word { get; set; } = null!;
}
