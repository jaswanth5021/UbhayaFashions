using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public static class CouponCustomerHistory
{
    public static Task<bool> HasPreviousOrderAsync(ApplicationDbContext db, int customerId)
    {
        return db.Orders.AsNoTracking().AnyAsync(order => order.CustomerId == customerId);
    }
}
