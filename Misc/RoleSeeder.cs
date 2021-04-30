using System;
using Microsoft.AspNetCore.Identity;

namespace CourseWiki.Misc
{
    public class RoleSeeder
    {
        public static void SeedData(RoleManager<IdentityRole<Guid>> roleManager)
        {
            if (!roleManager.RoleExistsAsync("User").Result)
            {
                IdentityRole<Guid> role = new IdentityRole<Guid>();
                role.Name = Models.Roles.User.ToString();
                IdentityResult roleResult = roleManager.
                    CreateAsync(role).Result;
            }
 
 
            if (!roleManager.RoleExistsAsync("Admin").Result)
            {
                IdentityRole<Guid> role = new IdentityRole<Guid>();
                role.Name = Models.Roles.Admin.ToString();
                IdentityResult roleResult = roleManager.
                    CreateAsync(role).Result;
            }
        }
    }
}