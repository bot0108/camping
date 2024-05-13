using camp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.IO;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace camp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampController : ControllerBase
    {
        public readonly IConfiguration _configuration;
        public CampController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllUsers")]
        public string GetUser()
        {
            MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
            MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM user", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<User> usersList = new List<User>();
            Response response = new Response();
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    User user = new User();
                    user.id = Convert.ToInt32(dt.Rows[i]["id"]);
                    user.name = Convert.ToString(dt.Rows[i]["name"]);
                    user.age = Convert.ToInt32(dt.Rows[i]["age"]);
                    usersList.Add(user);
                }
            }
            if (usersList.Count>0)
            {
                return JsonSerializer.Serialize(usersList);
            }
            else
            {
                response.Statuscode = 100;
                response.ErrorMessage = "no data found";
                return JsonSerializer.Serialize(response);
            }
        }
    }
}
