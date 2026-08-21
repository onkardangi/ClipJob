namespace ClipJob.Desktop;

public interface IClipRepository
{
    Task<IReadOnlyList<Clip>> GetAllAsync();
    Task AddAsync(Clip clip);
    Task UpdateAsync(Clip clip);
    Task DeleteAsync(Guid id);
}
