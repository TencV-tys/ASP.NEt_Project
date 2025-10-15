using AirlineReservationSystem.Models;

namespace AirlineReservationSystem.ViewModels
{
    public class UserWithRolesViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}