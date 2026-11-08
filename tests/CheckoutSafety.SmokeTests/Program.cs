using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

int passed = 0;
void Check(bool result, string name)
{
    if (!result) throw new Exception("FAIL: " + name);
    passed++; Console.WriteLine("PASS: " + name);
}
void Throws(Action action, string name)
{
    try { action(); } catch (InvalidOperationException) { Check(true, name); return; }
    throw new Exception("FAIL: " + name);
}
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
    { ["PaymentTesting:Enabled"] = "true" }).Build();
Check(!CheckoutSafety.TestPaymentsAllowed(new TestEnvironment("Production"), config), "Production cannot enable test payments");
Check(!CheckoutSafety.TestPaymentsAllowed(new TestEnvironment("Staging"), config), "Staging cannot enable test payments");
Check(CheckoutSafety.TestPaymentsAllowed(new TestEnvironment("Development"), config), "Development explicit opt-in works");
var red = new ProductVariant { Id=1, ProductId=10, Size="M", Color="Red", Stock=3 };
var blue = new ProductVariant { Id=2, ProductId=10, Size="M", Color="Blue", Stock=5 };
OrderItem Line(string? color, int quantity) => new() { ProductId=10, Size="M", Color=color, Quantity=quantity };
var plan = CheckoutSafety.PlanStock([Line("Blue",2)], [red,blue]);
Check(plan.Single().Variant.Id == 2, "Same-size colour variants resolve correctly");
plan = CheckoutSafety.PlanStock([Line("blue",3),Line(" Blue ",3)], [red,blue]);
Check(plan.Single().Quantity == 6 && plan.Single().Quantity > blue.Stock, "Duplicate lines cannot bypass stock check");
Check(blue.Stock == 5, "Validation does not mutate stock");
Throws(() => CheckoutSafety.PlanStock([Line(null,1)], [red,blue]), "Ambiguous legacy colour is rejected");
Throws(() => CheckoutSafety.PlanStock([Line("Blue",0)], [red,blue]), "Zero quantity is rejected");
var address = new PaymentDeliveryAddress("Customer", "9999999999", "Test address", null, "Pune", "Maharashtra", "411027");
Check(CheckoutSafety.ValidateAddress(address) is null, "Complete Indian address accepted");
Check(CheckoutSafety.ValidateAddress(address with { PostalCode="000000" }) is not null, "Invalid PIN rejected");
Check(CheckoutSafety.ValidateAddress(null) is not null, "Missing address rejected");
Check(CheckoutSafety.ValidateAddress(address with { Country="Other" }) is not null, "Unsupported country rejected");
using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options);
var cancelled = new Order { Id=1, Status="Cancelled", PaymentStatus="Pending" };
var payment = new Payment { OrderId=1, Order=cancelled };
await CheckoutSafety.ApplyCaptureAsync(db,cancelled,payment,"pay_test");
Check(cancelled.Status == "Cancelled" && cancelled.PaymentStatus == "Refund Pending", "Late capture never reactivates a cancelled order");
Check(payment.Status == "Captured" && payment.PaidDate.HasValue, "Late capture preserves financial truth");
Check(!cancelled.InventoryCommitted, "Late capture does not deduct stock");
payment.Status="Refunded"; cancelled.PaymentStatus="Refunded";
await CheckoutSafety.ApplyCaptureAsync(db,cancelled,payment,"pay_test");
Check(payment.Status == "Refunded" && cancelled.PaymentStatus == "Refunded", "Duplicate capture never reverses a refund");
var unpaid = new Order { PaymentMethod="Prepaid", PaymentStatus="Pending" };
Check(!unpaid.InventoryCommitted, "Unpaid prepaid orders have no stock to restore");
var sold = new Order { Id=2 };
CheckoutSafety.ApplyStock(db,sold,CheckoutSafety.PlanStock([Line("Blue",2)],[red,blue]),false);
Check(blue.Stock == 3 && sold.InventoryCommitted && red.Stock == 3, "Sale deducts only the correct variant");
CheckoutSafety.ApplyStock(db,sold,CheckoutSafety.PlanStock([Line("Blue",2)],[red,blue]),true);
Check(blue.Stock == 5 && !sold.InventoryCommitted, "Cancellation clears the stock-commit flag");
Check(db.Database.GetMigrations().Contains("20261108120000_ProductionCheckoutSafety"), "Production migration is discoverable");
var version = AuthenticationVersion.Create("old-password-hash", "test-signing-key");
Check(AuthenticationVersion.Matches(version, "old-password-hash", "test-signing-key"), "Current authentication version is accepted");
Check(!AuthenticationVersion.Matches(version, "new-password-hash", "test-signing-key"), "Password change revokes the old token");
Check(!AuthenticationVersion.Matches(null, "old-password-hash", "test-signing-key"), "Legacy tokens require a fresh login");
var sql = db.GetService<IMigrator>().GenerateScript("20261104170000_AddRazorpayWebhookEvents", "20261108120000_ProductionCheckoutSafety");
Check(sql.Contains("InventoryCommitted") && sql.Contains("IX_Orders_CustomerId_CheckoutKey"), "Upgrade SQL generates without a database connection");
Console.WriteLine($"{passed} checks passed. No SQL server or payment provider was contacted.");

sealed class TestEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "SmokeTests";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
