using Training.Services.IServices;
using Training.Models;
using Training.Services.IServices;

namespace Training.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public Employees CurrentEmployee { get; set; }
    }
}
