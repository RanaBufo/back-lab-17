using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1;


var builder = WebApplication.CreateBuilder(args);

// внедрение зависимости Entity Framework
// DB
builder.Services.AddDbContext<ApplicationContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "default connection string";
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
// внедрение зависимости UserService
builder.Services.AddTransient<UserService>();
// добавление кэширования
builder.Services.AddMemoryCache();
var app = builder.Build();

app.MapGet("/user/{id}", async (int id, UserService userService) =>
{
    User? user = await userService.GetUser(id);
    if (user != null) return $"User {user.Name}  Id={user.Id}  Age={user.Age}";
    return "User not found";
});
app.MapGet("/", () => "Hello World!");

app.Run();




public class UserService
{
    ApplicationContext db;
    IMemoryCache cache;
    public UserService(ApplicationContext context, IMemoryCache memoryCache)
    {
        db = context;
        cache = memoryCache;
    }
    public async Task<User?> GetUser(int id)
    {
        // пытаемся получить данные из кэша
        cache.TryGetValue(id, out User? user);
        // если данные не найдены в кэше
        if (user == null)
        {
            // обращаемся к базе данных
            user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
            // если пользователь найден, то добавляем в кэш - время кэширования 5 минут
            if (user != null)
            {
                Console.WriteLine($"{user.Name} извлечен из базы данных");
                cache.Set(user.Id, user, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }
        }
        else
        {
            Console.WriteLine($"{user.Name} извлечен из кэша");
        }
        return user;
    }
}