using ChatHubTest2.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatHubTest2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private ApplicationContext _context;

        public UserController(ApplicationContext context)
        {
            _context = context;
        }

        [HttpGet]

        public IActionResult GetUsers()
        {
            List<User> retUsers = new List<User>();
            var users = _context.IdenUsers.ToList();
            foreach (var item in users)
            {
                User user = new User
                {
                    Id = new Guid(item.Id),
                    Name = item.UserName
                };
                retUsers.Add(user);
            }
            return Ok(users);
        }

       
    }
}
