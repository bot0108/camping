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
        [Route("GetAllUsers&Sites")]
        public string GetUser()
        {
            MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
            MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM user", con);
            MySqlDataAdapter da2 = new MySqlDataAdapter("SELECT * FROM spots", con);
            DataTable dt = new DataTable();
            DataTable dt2 = new DataTable();
            da.Fill(dt);
            da2.Fill(dt2);

            List<User> usersList = new List<User>();
            List<Spot> spotsList = new List<Spot>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    User user = new User();
                    user.id = Convert.ToInt32(dt.Rows[i]["id"]);
                    user.name = Convert.ToString(dt.Rows[i]["name"]);
                    user.email = Convert.ToString(dt.Rows[i]["email"]);
                    user.password = Convert.ToString(dt.Rows[i]["password"]);
                    usersList.Add(user);

                }
            }
            if (dt2.Rows.Count > 0)
            {
                for (int i = 0; i < dt2.Rows.Count; i++)
                {
                    Spot spot = new Spot();
                    spot.id = Convert.ToInt32(dt2.Rows[i]["id"]);
                    spot.spotname = Convert.ToString(dt2.Rows[i]["spotname"]);
                    spot.location = Convert.ToString(dt2.Rows[i]["location"]);
                    spot.capacity = Convert.ToInt32(dt2.Rows[i]["capacity"]);
                    spot.description = Convert.ToString(dt2.Rows[i]["description"]);
                    spot.price = Convert.ToInt32(dt2.Rows[i]["price"]);

                    spotsList.Add(spot);
                }
            }

            if (usersList.Any() && spotsList.Any())
            {
                var combinedData = new
                {
                    Users = usersList,
                    Spots = spotsList
                };

                return JsonSerializer.Serialize(combinedData);
            }
            else
            {
                response.Statuscode = 100;
                response.ErrorMessage = "no data found";
                return JsonSerializer.Serialize(response);
            }
        }
        [HttpPost]
        [Route("AddUser")]
        public async Task<IActionResult> AddUser(User newUser)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString()))
                {
                    await con.OpenAsync();

                    string query = "INSERT INTO user (name,email,password,isowner) VALUES (@Name,@Email,@Password,@IsOwner)";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Name", newUser.name);
                        cmd.Parameters.AddWithValue("@Email", newUser.email);
                        cmd.Parameters.AddWithValue("@Password", newUser.password);
                        cmd.Parameters.AddWithValue("@IsOwner", newUser.isowner);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok("User added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("AddListing")]
        public async Task<IActionResult> AddListing(Spot newSpot)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    string query = "INSERT INTO spots (spotname, location, capacity, description, price) VALUES (@SpotName, @Location, @Capacity, @Description, @Price)";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@SpotName", newSpot.spotname);
                        cmd.Parameters.AddWithValue("@Location", newSpot.location);
                        cmd.Parameters.AddWithValue("@Capacity", newSpot.capacity);
                        cmd.Parameters.AddWithValue("@Description", newSpot.description);
                        cmd.Parameters.AddWithValue("@Price", newSpot.price);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Listing added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(UserLoginRequest loginRequest)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString()))
                {
                    await con.OpenAsync();

                    string query = "SELECT * FROM user WHERE email = @Email AND password = @Password";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", loginRequest.email);
                        cmd.Parameters.AddWithValue("@Password", loginRequest.password);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                // Authentication successful
                                return Ok(new { success = true });
                            }
                            else
                            {
                                // Authentication failed
                                return Ok(new { success = false });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetAllSpots")]
        public string GetSpots()
        {
            MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
            MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM spots", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<FeaturedSite> spotsList = new List<FeaturedSite>();
            Response response = new Response();
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    FeaturedSite cs = new FeaturedSite();
                    cs.spotname = Convert.ToString(dt.Rows[i]["spotname"]);
                    cs.description = Convert.ToString(dt.Rows[i]["description"]);

                    spotsList.Add(cs);
                }
            }
            if (spotsList.Count > 0)
            {
                return JsonSerializer.Serialize(spotsList);
            }
            else
            {
                response.ErrorMessage = "no data found";
                return JsonSerializer.Serialize(response);
            }
        }

    }
}
