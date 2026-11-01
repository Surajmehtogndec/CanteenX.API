using CanteenX.API.Data;
using CanteenX.API.DTOs.CanteenOwner;
using Microsoft.EntityFrameworkCore;

namespace CanteenX.API.Services.HomeService
{
    public class HomeService : IHomeService
    {
        private readonly CanteenxdbContext _context;
        public HomeService(CanteenxdbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CanteenCardDto>> GetAllActiveCanteensAsync( CancellationToken cancellationToken)
        {
            try
            {
            var canteens = await _context.Canteens
                            .AsNoTracking()
                            .Where(c => c.IsActive == true)
                            .Select(c => new CanteenCardDto
                            {
                                CanteenId = c.CanteenId,
                                CanteenName = c.CanteenName ?? "Unknown Canteen",
                                Description = c.Description ?? "No description",
                                ImageUrl = c.ImageUrl ?? "/images/default-canteen.jpg",

                                CrowdStatus = _context.CanteenLiveMetrics
                                    .Where(m => m.CanteenId == c.CanteenId)
                                    .Select(m => m.CrowdStatus)
                                    .FirstOrDefault() ?? "Low Crowd",

                                EstimatedWaitTime = _context.CanteenLiveMetrics
                                    .Where(m => m.CanteenId == c.CanteenId)
                                    .Select(m => m.EstimatedWaitTime)
                                    .FirstOrDefault() ?? "5-10 min",

                                Rating = _context.CanteenLiveMetrics
                                    .Where(m => m.CanteenId == c.CanteenId)
                                    .Select(m => m.Rating)
                                    .FirstOrDefault() ,

                                ReviewCount = _context.CanteenLiveMetrics
                                    .Where(m => m.CanteenId == c.CanteenId)
                                    .Select(m => m.ReviewCount)
                                    .FirstOrDefault() 
                            })
                            .ToListAsync();
                return canteens;
            }

            catch (OperationCanceledException ex)
            {
               
               
                return Enumerable.Empty<CanteenCardDto>(); // Empty list return karega
            }
            catch (Exception ex)
            {
                // Database ya query fatne par error log karega
              

                // Error ko aage Controller tak throw karega taaki wo user ko 500 status bhej sake
                throw;
            }
        }

    }
    
}
