using ChatHubTest2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatHubTest2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private ApplicationContext _context;
        private UserManager<IdenUser> _userManager;
        private SignInManager<IdenUser> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationContext context, UserManager<IdenUser> userManager, SignInManager<IdenUser> signInManager,
            RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;

        }

        private string GenerateJwtToken(IdenUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Name, user.Login),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "user"),
                new Claim(ClaimTypes.NameIdentifier, user.UserName)
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("refreshToken")]
        public string RefreshToken([FromBody] RefreshTokenModel refreshTokenModel)
        {
            string result = "";
            var user = _context.IdenUsers.FirstOrDefault(x => x.UserName == refreshTokenModel.UserName);
            var appcc = _context.ApplicationSessions.FirstOrDefault(x => x.UserId.ToString() == user.Id);
            if(appcc != null)
            {
                if(refreshTokenModel.RefreshToken == appcc.RefreshTokenId.ToString() && appcc.ExpirationDate > DateTime.UtcNow) 
                {
                    result =  GenerateJwtToken(user); 
                }
            }
            return result;
        }


        [HttpPost]
        [Route("/api/Register")]
        public async Task<IActionResult> Register(LoginViewModel registerViewModel)
        {
            string token = "";
            string refreshtokenid = "";
            IdenUser user = new IdenUser()
            {
                Login = registerViewModel.Login,
                UserName = registerViewModel.Login,

            };


            try
            {
                var result = await _userManager.CreateAsync(user, registerViewModel.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    List<Claim> claim = new List<Claim>();
                    claim.Add(new Claim(ClaimTypes.Role, "user"));

                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, user.Login));
                    //await _userManager.AddToRoleAsync(user, "admin");
                    await _userManager.AddClaimsAsync(user, claim);
                    bool roleExists = await _roleManager.RoleExistsAsync("user");


                    if (!roleExists)
                    {
                        var role = new IdentityRole("user");
                        await _roleManager.CreateAsync(role);
                    }


                    await _userManager.AddToRoleAsync(user, "user");
                    token = GenerateJwtToken(user);

                    
                    var appss = new ApplicationSession
                    {
                        Id = Guid.NewGuid(),
                        UserId = new Guid(user.Id),
                        RefreshTokenId = Guid.NewGuid(),
                        CreatedTime = DateTime.UtcNow,
                        ExpirationDate = DateTime.UtcNow.AddHours(35),
                    };
                    _context.ApplicationSessions.Add(appss);
                    _context.SaveChanges();
                    refreshtokenid = appss.RefreshTokenId.ToString();
                }
                else
                {

                    return BadRequest(new UserViewModel() { Message = result.Errors.First().Description });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new UserViewModel() { Message = $"{ex.Message}" });
            }

            return Ok(new UserViewModel() { Message = "Все прошло успешно", Token = token, RefreshTokenId = refreshtokenid });
        }


        [HttpPost]
        [Route("/api/Login")]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            var userToFound = _context.IdenUsers.FirstOrDefault(x => x.UserName == loginViewModel.Login);
            if (userToFound == null)
            {
                return Ok(await Register(loginViewModel));
            }

            var user = _context.IdenUsers.FirstOrDefault(x => x.UserName == loginViewModel.Login);

            bool isValid = await _userManager.CheckPasswordAsync(user, loginViewModel.Password);

            if (user == null || !isValid)
            {
                return BadRequest(new UserViewModel() { Message = "Неправильный логин или пароль" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, isPersistent: false, false);
            var token = GenerateJwtToken(user);
            string refreshtokenid = "";
            var appss = _context.ApplicationSessions.FirstOrDefault(x => x.UserId.ToString() == user.Id);
            if (appss != null && appss.ExpirationDate > DateTime.UtcNow)
                refreshtokenid = appss.RefreshTokenId.ToString();
            else
            {
                appss.RefreshTokenId = Guid.NewGuid();
                appss.CreatedTime = DateTime.UtcNow;
                appss.ExpirationDate = DateTime.UtcNow.AddHours(35);
                _context.ApplicationSessions.Update(appss);
                _context.SaveChanges();
                refreshtokenid = appss.RefreshTokenId.ToString();
            }
            return Ok(new UserViewModel { Message = "Все прошло успешно", Token = token, UserName = user.UserName, RefreshTokenId = refreshtokenid });
        }

        [HttpPost("CheckToken")]
        public bool IsTokenValid([FromBody] TokenCheckClass tokenCheckClass)
        {
            if (string.IsNullOrEmpty(tokenCheckClass.Token))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                tokenHandler.ValidateToken(tokenCheckClass.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Issuer"],
                    ValidateLifetime = true, 
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
