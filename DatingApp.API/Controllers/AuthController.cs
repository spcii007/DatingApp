using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Data;
using DatingApp.API.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Models;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        public async Task <IActionResult> Register(UserForRegisterDto userRegisterForDto)
        {
            //validate request
            userRegisterForDto.Username = userRegisterForDto.Username.ToLower();

            if (await _repo.UserExists(userRegisterForDto.Username))
            {
                return BadRequest("Username already exist!");
            }
            var usertoCreate = new User
            {
                Username = userRegisterForDto.Username,

            };

            var CreatedUser = await _repo.Register(usertoCreate, userRegisterForDto.Password);
            return StatusCode(201);
            
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForRegisterDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username, userForLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized();

            //Build token include System.Security.Claims
            var claims = new[]
            {
                new Claim (ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim (ClaimTypes.Name, userFromRepo.Username)
            };

            // We need a hash key - using Microsoft.IdentityModel.Tokens

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds

            };
            // We need a token handler
            var tokenHandler = new JwtSecurityTokenHandler();

            // Finally create a token

            var token = tokenHandler.CreateToken(tokenDescriptor);

            //Now we return the token

            return Ok(
                new 
                {
                    token = tokenHandler.WriteToken(token)
                }
                );

        }
        

    }
}
