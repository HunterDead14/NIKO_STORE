using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class ApplicationDbContextInitialiser
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;

    public ApplicationDbContextInitialiser(
        ApplicationDbContext context,
        ILogger<ApplicationDbContextInitialiser> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred while initialising the database.");
            throw;
        }
    }
}
