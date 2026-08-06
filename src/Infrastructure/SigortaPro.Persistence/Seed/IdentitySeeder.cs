using Microsoft.AspNetCore.Identity;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Persistence.Identity;

namespace SigortaPro.Persistence.Seed;

// Identity rolleri (Admin, Personel, Customer), varsayılan admin hesabı ve örnek müşterinin
// Identity kullanıcısı seed edilir. Örnek müşteri kullanıcısı, DbSeeder'ın oluşturduğu Customer profilinin
// AppUserId'siyle (SeedIds.SampleCustomerAppUserId) birebir aynı Id ile oluşturulur (notu).
// Idempotent'tir: mevcut rol/kullanıcı varsa tekrar oluşturmaz.
//
// GÜVENLİK: Buradaki şifreler yalnızca geliştirme ortamı seed'i içindir; üretimde kullanılmamalıdır.
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@sigortapro.com";
    public const string AdminPassword = "Admin!2345";

    public const string SampleCustomerEmail = "musteri@sigortapro.com";
    public const string SampleCustomerPassword = "Musteri!2345";

    // (K13): Geliştirme/test kolaylığı için örnek Personel hesabı. Üretimde seed şifreleri kullanılmaz
    // (bu sınıfın GÜVENLİK notu geçerlidir). Personel'in Customer profili yoktur — yalnızca kimlik hesabıdır.
    public const string SampleStaffEmail = "personel@sigortapro.com";
    public const string SampleStaffPassword = "Personel!2345";

    public static async Task SeedAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);
        await SeedSampleStaffAsync(userManager);
        await SeedSampleCustomerUserAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<AppUser> userManager)
    {
        if (await userManager.FindByEmailAsync(AdminEmail) is not null)
        {
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }

    private static async Task SeedSampleStaffAsync(UserManager<AppUser> userManager)
    {
        if (await userManager.FindByEmailAsync(SampleStaffEmail) is not null)
        {
            return;
        }

        var staff = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = SampleStaffEmail,
            Email = SampleStaffEmail,
            FullName = "Örnek Personel",
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(staff, SampleStaffPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(staff, Roles.Personel);
        }
    }

    private static async Task SeedSampleCustomerUserAsync(UserManager<AppUser> userManager)
    {
        if (await userManager.FindByEmailAsync(SampleCustomerEmail) is not null)
        {
            return;
        }

        var user = new AppUser
        {
            // DbSeeder'daki Customer.AppUserId ile eşleşmek zorunda (notu).
            Id = SeedIds.SampleCustomerAppUserId,
            UserName = SampleCustomerEmail,
            Email = SampleCustomerEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, SampleCustomerPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, Roles.Customer);
        }
    }
}
