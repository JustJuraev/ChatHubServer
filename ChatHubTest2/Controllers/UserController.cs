using ChatHubTest2.Models;
using Microsoft.AspNetCore.Http;
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
            return Ok(_context.Users.ToList());
        }
    }
}
