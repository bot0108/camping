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
        [HttpDelete]
        [Route("DeleteSpot/{id}")]
        public async Task<IActionResult> DeleteSpot(int id)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString()))
                {
                    await con.OpenAsync();
                    using (var transaction = await con.BeginTransactionAsync())
                    {
                        try
                        {
                            // Delete from bookings
                            string deleteBookingsQuery = "DELETE FROM bookings WHERE spotID = @SpotID";
                            using (MySqlCommand cmd = new MySqlCommand(deleteBookingsQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SpotID", id);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Delete from spot_images
                            string deleteImagesQuery = "DELETE FROM spot_images WHERE spot_id = @SpotID";
                            using (MySqlCommand cmd = new MySqlCommand(deleteImagesQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SpotID", id);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Delete from spots
                            string deleteSpotsQuery = "DELETE FROM spots WHERE id = @SpotID";
                            using (MySqlCommand cmd = new MySqlCommand(deleteSpotsQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SpotID", id);
                                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    await transaction.CommitAsync();
                                    return Ok("Listing deleted successfully.");
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    return NotFound("Listing not found.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            return StatusCode(500, $"Error: {ex.Message}");
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
        [Route("GetAllBookings")]
        public async Task<IActionResult> GetAllBookings()
        {
            try
            {
                List<Booking> bookings = new List<Booking>();

                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString()))
                {
                    await con.OpenAsync();

                    string query = "SELECT * FROM bookings";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                Booking booking = new Booking
                                {
                                    BookingId = Convert.ToInt32(rdr["bookingID"]),
                                    SpotId = Convert.ToInt32(rdr["spotID"]),
                                    UserId = Convert.ToInt32(rdr["userID"])
                                    // Add more properties if needed
                                };

                                bookings.Add(booking);
                            }
                        }
                    }
                }

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPut]
        [Route("Update")]
        public async Task<IActionResult> Update(User updatedUser)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString()))
                {
                    await con.OpenAsync();

                    string query = "UPDATE user SET email = @Email, password = @Password WHERE id = @Id";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", updatedUser.email);
                        cmd.Parameters.AddWithValue("@Password", updatedUser.password);
                        cmd.Parameters.AddWithValue("@Id", updatedUser.id);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("User information updated successfully.");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to update user information. User not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("CreateBooking")]
        public async Task<IActionResult> CreateBooking(AddBooking booking)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString()))
                {
                    await con.OpenAsync();

                    string query = "INSERT INTO bookings (spotID,userID) VALUES (@SpotID,@UserID)";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@SpotID", booking.spotID);
                        cmd.Parameters.AddWithValue("@UserID", booking.userID);

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
        public async Task<IActionResult> AddListing([FromForm] Spot newSpot)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    // Insert the spot details
                    string insertSpotQuery = "INSERT INTO spots (spotname, location, capacity, description, price, userIDFK) VALUES (@SpotName, @Location, @Capacity, @Description, @Price , @UserIDFK)";
                    using (MySqlCommand cmd = new MySqlCommand(insertSpotQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@SpotName", newSpot.spotname);
                        cmd.Parameters.AddWithValue("@Location", newSpot.location);
                        cmd.Parameters.AddWithValue("@Capacity", newSpot.capacity);
                        cmd.Parameters.AddWithValue("@Description", newSpot.description);
                        cmd.Parameters.AddWithValue("@Price", newSpot.price);
                        cmd.Parameters.AddWithValue("@UserIDFK", newSpot.userIDFK);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Retrieve the last inserted ID
                    long spot_id;
                    string getLastInsertedIdQuery = "SELECT LAST_INSERT_ID()";
                    using (MySqlCommand cmd = new MySqlCommand(getLastInsertedIdQuery, con))
                    {
                        var lastInsertedId = await cmd.ExecuteScalarAsync();
                        spot_id = Convert.ToInt64(lastInsertedId);
                    }

                    // Handle image uploads
                    if (newSpot.Images != null && newSpot.Images.Count > 0)
                    {
                        foreach (var image in newSpot.Images)
                        {
                            byte[] imageData;
                            using (var ms = new MemoryStream())
                            {
                                await image.CopyToAsync(ms);
                                imageData = ms.ToArray();
                            }

                            string insertImageQuery = "INSERT INTO spot_images (spot_id, image_data) VALUES (@SpotId, @ImageData)";
                            using (MySqlCommand imageCmd = new MySqlCommand(insertImageQuery, con))
                            {
                                imageCmd.Parameters.AddWithValue("@SpotId", spot_id);
                                imageCmd.Parameters.AddWithValue("@ImageData", imageData);

                                await imageCmd.ExecuteNonQueryAsync();
                            }
                        }
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

                    string query = "SELECT id, email, password, isOwner, name FROM user WHERE email = @Email";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", loginRequest.email);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                var userId = reader["id"].ToString();
                                var storedPasswordHash = reader["password"].ToString();
                                var isowner = Convert.ToInt32(reader["isowner"]);
                                var name = reader["name"].ToString(); // Retrieve the user's name

                                // Verify the provided password against the stored hash
                                if (BCrypt.Net.BCrypt.Verify(loginRequest.password, storedPasswordHash))
                                {
                                    // Password matches, authentication successful
                                    return Ok(new { success = true, message = "Login successful", isowner, name, id = userId });
                                }
                                else
                                {
                                    // Password does not match
                                    return Ok(new { success = false, message = "Invalid email or password" });
                                }
                            }
                            else
                            {
                                // User not found
                                return Ok(new { success = false, message = "Invalid email or password" });
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
                    cs.capacity = Convert.ToInt32(dt.Rows[i]["capacity"]);
                    cs.price = Convert.ToDecimal(dt.Rows[i]["price"]);
                    cs.userIDFK = Convert.ToInt32(dt.Rows[i]["userIDFK"]);
                    cs.id = Convert.ToInt32(dt.Rows[i]["id"]);

                    // Retrieve image data for each spot
                    List<string> imageBase64Strings = GetImageBase64StringsForSpot(con, Convert.ToInt64(dt.Rows[i]["id"]));

                    cs.imagePaths = imageBase64Strings;

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

        // Helper method to get image base64 strings for a spot
        private List<string> GetImageBase64StringsForSpot(MySqlConnection con, long spotId)
        {
            List<string> imageBase64Strings = new List<string>();
            try
            {
                if (con.State != ConnectionState.Open)
                {
                    con.Open(); // Open the connection if it's not already open
                }

                // Query to retrieve image data for the spot with given spotId
                string query = "SELECT image_data FROM spot_images WHERE spot_id = @SpotId";
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@SpotId", spotId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] imageData = (byte[])reader["image_data"];
                            string base64String = Convert.ToBase64String(imageData);
                            imageBase64Strings.Add(base64String);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Console.WriteLine("Error retrieving image data for spot: " + ex.Message);
            }
            finally
            {
                con.Close(); // Close the connection
            }
            return imageBase64Strings;
        }


    }
}
