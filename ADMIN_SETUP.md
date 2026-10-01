# Ubhaya Fashions Admin Panel

The admin panel has been added to the existing frontend/backend solution. No separate project is required.

## URLs

- Store: https://localhost:5001/
- Admin login: https://localhost:5001/Admin/Login
- Backend Swagger: https://localhost:56738/swagger

## Admin account

The admin login uses records in `dbo.AdminUsers`. The repository lists `admin@ubhaya.com` / `Admin@123` as sample credentials, but the current backend does not seed that account automatically. Confirm that an active row exists and has a BCrypt password hash before expecting those credentials to work.

## Admin features

- Dashboard
- Product create/edit/delete
- Product image upload (JPG/JPEG/PNG/WEBP, up to 10 MB)
- Stock management
- Inventory transaction history API
- Order list
- Order status updates
- Payment status tracking using the existing Order.PaymentStatus field
- Admin-only JWT role protection on backend admin APIs
- Admin password recovery by one-time email link

## Admin password recovery

Use **Forgot your password?** on `/Admin/Login`. Reset email uses the backend SMTP settings documented in `backend/ACCOUNT_SETUP.md`. Apply the latest EF migration from the backend directory before deploying:

```powershell
dotnet ef database update
```
- Signed-in admin password change with current-password confirmation

## Database note

The existing application uses `EnsureCreatedAsync()`. Because this stage must also work against an already-created `LadiesDressStore` database, startup creates the two new tables when they do not exist:

- AdminUsers
- InventoryTransactions

No existing customer/product/order tables are dropped.

For a production environment, convert these changes to normal EF Core migrations and remove the startup SQL initializer.

## Image storage

Uploaded images are saved by the backend under:

`backend/wwwroot/uploads/products`

The Product.ImageUrl field is updated with the backend public URL.

## Run

Start backend first on `https://localhost:56738`, then frontend on `https://localhost:5001`.
