using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace camp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Configure CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:8080") // Adjust to match your frontend origin
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });

            // Add Swagger/OpenAPI documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Enable Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Enable HTTPS redirection (optional)
            // app.UseHttpsRedirection();

            // Enable CORS
            app.UseCors("AllowSpecificOrigin");

            // Serve static files (if needed)
            // app.UseStaticFiles();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
