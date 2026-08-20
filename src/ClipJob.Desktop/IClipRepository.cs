namespace ClipJob.Desktop;

public interface IClipRepository
{
    Task<IReadOnlyList<Clip>> GetAllAsync();
}
