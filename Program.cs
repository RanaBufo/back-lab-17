using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1;


var builder = WebApplication.CreateBuilder(args);

// ��������� ����������� Entity Framework
// DB
builder.Services.AddDbContext<ApplicationContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "default connection string";
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
// ��������� ����������� UserService
builder.Services.AddTransient<UserService>();
// ���������� �����������
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
        // �������� �������� ������ �� ����
        cache.TryGetValue(id, out User? user);
        // ���� ������ �� ������� � ����
        if (user == null)
        {
            // ���������� � ���� ������
            user = await db.Users.FirstOrDefaultAsync(p => p.Id == id);
            // ���� ������������ ������, �� ��������� � ��� - ����� ����������� 5 �����
            if (user != null)
            {
                Console.WriteLine($"{user.Name} �������� �� ���� ������");
                cache.Set(user.Id, user, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }
        }
        else
        {
            Console.WriteLine($"{user.Name} �������� �� ����");
        }
        return user;
    }
}