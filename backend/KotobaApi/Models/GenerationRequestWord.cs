namespace KotobaApi.Models;

public class GenerationRequestWord
{
    public Guid Id { get; set; }
    public Guid GenerationRequestId { get; set; }
    public Guid WordId { get; set; }

    public GenerationRequest GenerationRequest { get; set; } = null!;
    public Word Word { get; set; } = null!;
}
