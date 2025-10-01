using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SGINVENTARIO.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<DataSeeder> _logger;

    private static readonly string[] Roles = ["Admin", "Tecnico", "Consulta"];

    public DataSeeder(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await _context.Database.MigrateAsync();

        foreach (var role in Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!await _userManager.Users.AnyAsync())
        {
            var admin = new IdentityUser
            {
                UserName = "admin@demo.com",
                Email = "admin@demo.com",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(admin, "Admin#123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                _logger.LogWarning("No se pudo crear el usuario administrador por defecto: {Errors}",
                    string.Join(",", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
