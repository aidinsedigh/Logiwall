using Logiwall.Domain.Entities.Global;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Domain.Entities.People;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, int, IdentityUserClaim<int>, IdentityUserRole<int>
        ,UserLogin, IdentityRoleClaim<int>, IdentityUserToken<int>>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<AppSettingLogiwall> AppSettings { get; set; }
    public DbSet<Constant> Constants { get; set; }
    public DbSet<StaticData> StaticDatas { get; set; }
    public DbSet<StaticDataMapper> StaticDataMappers { get; set; }
    public DbSet<UserOtp> UserOtps { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<PersonalInfo> PersonalInfos { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerContact> CustomerContacts { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderInqueryOption> OrderInqueryOptions { get; set; }
    public DbSet<OrderParcel> OrderParcels { get; set; }
    public DbSet<OrderPayment> OrderPayments { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }

    public DbSet<Domain.Entities.OrderItems.ServiceProvider> ServiceProviders { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigIdentityTables(builder);

        builder.HasSequence<int>("OrderSequence", schema: "dbo")
            .StartsAt(1)
            .IncrementsBy(1);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(nameof(AppStrings.UpdateConcurrencyException), exception);
        }
    }


    private void ConfigIdentityTables(ModelBuilder builder)
    {
        //Config Asp Identity table name
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<UserLogin>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
    }
}
