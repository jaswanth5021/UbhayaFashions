# UBHAYA Fashions — Test Case Catalogue

**Scope:** ASP.NET Core MVC storefront, backend product API, account, cart, wishlist, checkout/orders, admin/catalog operations, deployment and security.  
**Test environment:** Use a staging deployment and seeded test data. Do not use real customer accounts, production order data, or live payment credentials.  
**Priority:** P0 = release blocker; P1 = high; P2 = normal.  
**Automation status:** `Automated` means a test is included in the current xUnit/Playwright smoke suite. `Manual/next automation` means it is a test specification to execute manually or automate when safe test data and required configuration are available.

## A. Website availability and navigation

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| UI-001 | P0 | Open home page | HTTP response below 400 and page title is not empty | Automated |
| UI-002 | P0 | Open Products listing | HTTP response below 400 | Automated |
| UI-003 | P1 | Open Categories page | Page loads without server error | Automated |
| UI-004 | P1 | Open Login page | Login form/page loads without server error | Automated |
| UI-005 | P1 | Open Cart page | Cart page loads for anonymous visitor or redirects to sign-in as designed | Automated |
| UI-006 | P1 | Open About, Privacy, Terms and Returns pages | Each configured informational page loads | Manual/next automation |
| UI-007 | P1 | Use logo link from another page | Navigates to home page | Manual/next automation |
| UI-008 | P1 | Use main navigation links | Each link navigates to its intended route | Manual/next automation |
| UI-009 | P1 | Use browser Back and Forward | Navigation history works without broken state | Manual/next automation |
| UI-010 | P1 | Open a non-existent route | Branded not-found response; no stack trace or secret details | Manual/next automation |
| UI-011 | P1 | Refresh Products and product-details pages | Page reloads without a routing error | Manual/next automation |
| UI-012 | P2 | Check links and buttons with keyboard only | Interactive elements can be reached and activated with keyboard | Manual/next automation |

## B. Responsive design and accessibility

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| UX-001 | P1 | Home page at 360px mobile width | No horizontal overflow; content and primary actions remain usable | Manual/next automation |
| UX-002 | P1 | Products listing at mobile width | Product cards stack/flow correctly and filters remain usable | Manual/next automation |
| UX-003 | P1 | Open and close hamburger menu | Menu opens, closes and does not trap the page in an overlay | Manual/next automation |
| UX-004 | P1 | Open category dropdown on desktop and mobile | Options are visible, selectable and keyboard accessible | Manual/next automation |
| UX-005 | P1 | Inspect product images | Images load, preserve aspect ratio and have useful alt text | Manual/next automation |
| UX-006 | P1 | Inspect forms and validation messages | Inputs have labels; errors are clear and associated with fields | Manual/next automation |
| UX-007 | P2 | Check text contrast and zoom to 200% | Text remains readable and content is not clipped | Manual/next automation |
| UX-008 | P2 | Check loading, empty and error states | User receives understandable feedback; layout remains usable | Manual/next automation |

## C. Product API and catalog

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| API-001 | P0 | GET /api/products | HTTP 200 and a JSON array | Automated |
| API-002 | P0 | GET /api/products/colors | HTTP 200 and a JSON array | Automated |
| API-003 | P1 | GET /api/products/price-bounds | HTTP 200; JSON object contains numeric min and max | Automated |
| API-004 | P1 | GET /api/products/{unknown-id} | HTTP 404 for an ID that does not exist | Automated |
| API-005 | P1 | GET /api/products/{unknown-id}/related | HTTP 404 for an ID that does not exist | Automated |
| API-006 | P1 | Search products with a query | HTTP 200 and a JSON array | Automated |
| API-007 | P1 | Filter products by category | Returned products match the selected category | Manual/next automation |
| API-008 | P1 | Filter products by size | Only products with an available matching size are returned | Manual/next automation |
| API-009 | P1 | Filter products by color | Returned products match the canonical color token | Manual/next automation |
| API-010 | P1 | Filter with minPrice and maxPrice | Returned products satisfy the requested price bounds | Manual/next automation |
| API-011 | P1 | Filter in-stock and out-of-stock | Products are grouped according to available variant stock | Manual/next automation |
| API-012 | P1 | Request best sellers and new arrivals | Results respect flags and new-arrival time window | Manual/next automation |
| API-013 | P1 | Sort price-low, price-high, name and newest | Results are in the selected order | Manual/next automation |
| API-014 | P1 | Request a related-products list for an existing product | HTTP 200; excludes the current product and returns valid card data | Manual/next automation |
| API-015 | P1 | Request product list when database is unavailable | Service fails safely; no credentials or stack trace leak | Manual/next automation |
| API-016 | P2 | Send blank, whitespace and URL-encoded search terms | Request is handled safely and returns valid JSON | Manual/next automation |
| API-017 | P2 | Send invalid/unknown filter values | No unhandled server error; behavior is documented and consistent | Manual/next automation |
| API-018 | P2 | Check product response fields | IDs, names, prices, variants and image references have expected types | Manual/next automation |

## D. Product listing and details UI

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| PROD-001 | P0 | Open product listing | Product cards or a useful empty/error state appear | Manual/next automation |
| PROD-002 | P1 | Search by product name | Matching products appear; no-match message is helpful | Manual/next automation |
| PROD-003 | P1 | Apply and clear each filter | Results update and clearing restores the broader list | Manual/next automation |
| PROD-004 | P1 | Change sort order | Visible product order changes as selected | Manual/next automation |
| PROD-005 | P1 | Navigate pagination | Correct page of results appears; previous/next boundaries work | Manual/next automation |
| PROD-006 | P1 | Open a product detail page | Name, images, price, variants and description render | Manual/next automation |
| PROD-007 | P1 | Open unknown product detail | Not-found behavior is shown without application crash | Manual/next automation |
| PROD-008 | P1 | Choose size and color | Selected options are reflected in the form/cart action | Manual/next automation |
| PROD-009 | P1 | Product with zero stock | Add-to-cart is disabled or a clear out-of-stock message appears | Manual/next automation |
| PROD-010 | P2 | Product with missing optional image/video | Page layout remains intact and a fallback is shown | Manual/next automation |
| PROD-011 | P2 | Review rating and review count | Summary is consistent with reviews returned by API | Manual/next automation |

## E. Account, authentication and authorization

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| AUTH-001 | P0 | Open login page | Form loads and is usable | Automated |
| AUTH-002 | P0 | Submit valid test credentials | User is authenticated and redirected to intended local return URL or home | Manual/next automation |
| AUTH-003 | P1 | Submit incorrect password | Login is rejected with a safe, understandable message | Manual/next automation |
| AUTH-004 | P1 | Submit unknown email | Login is rejected without exposing sensitive account details | Manual/next automation |
| AUTH-005 | P1 | Submit empty/invalid login fields | Server-side validation prevents login request | Manual/next automation |
| AUTH-006 | P1 | Submit invalid signup fields | Validation errors appear and no account is created | Manual/next automation |
| AUTH-007 | P1 | Sign up with a valid unique test account | Account is created once and success is communicated | Manual/next automation |
| AUTH-008 | P1 | Sign up with an existing email | Duplicate registration is rejected safely | Manual/next automation |
| AUTH-009 | P0 | Attempt external returnUrl after login | Redirect is restricted to a local URL; no open redirect | Manual/next automation |
| AUTH-010 | P0 | Call protected orders endpoint without token | HTTP 401/403; no order is created | Manual/next automation |
| AUTH-011 | P1 | Sign out | Authentication cookie/session is cleared | Manual/next automation |
| AUTH-012 | P1 | Access account-only page after sign out | User is redirected to login or receives an access-denied response | Manual/next automation |
| AUTH-013 | P1 | Check cookie flags in HTTPS environment | Secure, HttpOnly and appropriate SameSite settings are applied | Manual/next automation |
| AUTH-014 | P2 | OAuth login buttons when providers are not configured | Clear fallback; no raw configuration exception shown | Manual/next automation |

## F. Cart and wishlist

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| CART-001 | P0 | Open cart with no items | Empty-cart state and continue-shopping action appear | Automated |
| CART-002 | P0 | Add an existing product with a valid size | Item is added and cart count/contents update | Manual/next automation |
| CART-003 | P1 | Add a non-existent product | Request returns not-found; cart remains unchanged | Manual/next automation |
| CART-004 | P1 | Add without selecting size | Request is rejected with a useful message | Manual/next automation |
| CART-005 | P1 | Add an out-of-stock size | Request is rejected and cart remains unchanged | Manual/next automation |
| CART-006 | P1 | Add quantity zero or negative | Request is rejected | Manual/next automation |
| CART-007 | P1 | Add quantity above available stock | Request is rejected with available quantity information | Manual/next automation |
| CART-008 | P1 | Add same product/size twice | Quantity merges or duplicates according to documented behavior; total is correct | Manual/next automation |
| CART-009 | P1 | Change quantity in cart | Quantity and line total update; invalid values are rejected | Manual/next automation |
| CART-010 | P1 | Remove a cart item | Item disappears and subtotal updates | Manual/next automation |
| CART-011 | P1 | Refresh page or open another page | Cart persistence follows the designed session/cookie behavior | Manual/next automation |
| CART-012 | P0 | Submit cart POST without antiforgery token | Request is rejected | Manual/next automation |
| WISH-001 | P1 | Add/remove wishlist item while signed in | Wishlist updates and persists for the account | Manual/next automation |
| WISH-002 | P1 | Open wishlist as anonymous user | Sign-in prompt or documented anonymous behavior appears | Manual/next automation |
| WISH-003 | P2 | Remove an item not in wishlist | Safe no-op or appropriate not-found response; no server error | Manual/next automation |

## G. Checkout, orders and payments

> Execute only in a staging environment with test accounts and a payment gateway sandbox. Never create real charges as part of CI.

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| ORDER-001 | P0 | Create order without authentication | HTTP 401/403; no order or stock mutation | Manual/next automation |
| ORDER-002 | P0 | Submit empty order items | HTTP 400 and no order is created | Manual/next automation |
| ORDER-003 | P0 | Submit zero/negative quantity or missing size | HTTP 400 and no order is created | Manual/next automation |
| ORDER-004 | P0 | Submit unknown product ID | HTTP 400 and no order is created | Manual/next automation |
| ORDER-005 | P0 | Submit unavailable size | HTTP 400 and no order is created | Manual/next automation |
| ORDER-006 | P0 | Submit quantity above stock | HTTP 400 and stock remains unchanged | Manual/next automation |
| ORDER-007 | P0 | Submit valid order with sandbox data | Order total equals sum of variant price × quantity | Manual/next automation |
| ORDER-008 | P0 | Verify inventory after successful order | Stock decreases by purchased quantity exactly once | Manual/next automation |
| ORDER-009 | P1 | Verify inventory transaction after order | A sale transaction records negative quantity and correct stock-after value | Manual/next automation |
| ORDER-010 | P1 | Simulate email service failure | Order outcome is consistent and error handling is observable; no duplicate order on retry | Manual/next automation |
| ORDER-011 | P0 | Submit same payment callback twice | Idempotency prevents duplicate successful order/payment | Manual/next automation |
| ORDER-012 | P0 | Payment declined/cancelled | Order/payment status remains failed or pending per design; no false success | Manual/next automation |
| ORDER-013 | P0 | Tamper with client-side total/price | Server recalculates price from trusted database values | Manual/next automation |
| ORDER-014 | P1 | Submit missing/invalid shipping details | Validation blocks checkout with clear errors | Manual/next automation |
| ORDER-015 | P1 | Verify order history/detail authorization | Customer can access only their own order data | Manual/next automation |
| ORDER-016 | P1 | Verify confirmation page | It displays the correct order reference and status only after confirmed success | Manual/next automation |

## H. Admin, catalog and inventory

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| ADM-001 | P0 | Open admin route as anonymous/customer | Access is denied or redirected; no admin data leaks | Manual/next automation |
| ADM-002 | P0 | Open admin route as authorized admin | Authorized admin can access permitted features | Manual/next automation |
| ADM-003 | P1 | Create product with valid fields | Product and related variants/images are saved correctly | Manual/next automation |
| ADM-004 | P1 | Create product with invalid required fields | Validation blocks save | Manual/next automation |
| ADM-005 | P1 | Edit product details | Changes persist and appear in public catalog | Manual/next automation |
| ADM-006 | P1 | Delete/deactivate product | Public catalog reflects intended removal; order history remains consistent | Manual/next automation |
| ADM-007 | P1 | Change variant price or stock | New values persist and public availability/price reflects them | Manual/next automation |
| ADM-008 | P1 | Upload invalid/oversized image | Upload is rejected safely with a useful message | Manual/next automation |
| ADM-009 | P1 | Attempt image upload with script/executable content | File validation rejects unsafe content | Manual/next automation |
| ADM-010 | P1 | Update order status as admin | Status change is authorized and auditable | Manual/next automation |

## I. Security, reliability and deployment

| ID | Pri. | Test | Expected result | Status |
|---|---|---|---|---|
| SEC-001 | P0 | Check repository and build logs for secrets | No passwords, JWT keys, connection strings or payment secrets committed | Manual/next automation |
| SEC-002 | P0 | Check production exception handling | No stack traces, connection strings or internal paths are exposed | Manual/next automation |
| SEC-003 | P0 | Submit HTML/script in user-controlled fields | Output is safely encoded; script does not execute | Manual/next automation |
| SEC-004 | P0 | Test SQL injection-like search input | Input is treated as data; no unauthorized data access or SQL execution | Manual/next automation |
| SEC-005 | P0 | Test CSRF protection on state-changing MVC forms | Requests without valid antiforgery tokens are rejected | Manual/next automation |
| SEC-006 | P0 | Check API authorization on protected routes | Anonymous/unauthorized callers cannot access protected data/actions | Manual/next automation |
| SEC-007 | P1 | Verify HTTPS and HTTP-to-HTTPS behavior | Valid TLS certificate; secure redirect policy; no mixed content | Manual/next automation |
| SEC-008 | P1 | Simulate API timeout/unavailable backend | MVC site shows graceful fallback and logs the failure | Manual/next automation |
| SEC-009 | P1 | Restart app and database in staging | App becomes healthy and data persists as designed | Manual/next automation |
| SEC-010 | P1 | Run migrations against a clean test database | Schema initializes/upgrades successfully without data loss | Manual/next automation |
| SEC-011 | P1 | Run the test workflow on pull request | Unit tests execute; configured UI/API smoke jobs execute and publish status | Manual/next automation |
| SEC-012 | P2 | Inspect response headers and static assets | Appropriate security headers and cache behavior are configured | Manual/next automation |
| SEC-013 | P2 | Check concurrent requests and stock race | Stock cannot be oversold; transaction/concurrency behavior is correct | Manual/next automation |
| SEC-014 | P2 | Check application logs after failed requests | Logs are useful and contain no sensitive personal/payment data | Manual/next automation |

## Current automated coverage

The automated smoke suite covers:
- Homepage, Products, Categories, Login and Cart page availability.
- Product list, color list, price bounds, search response shape and missing product/related-product behavior.
- CI execution of existing backend and frontend unit-test projects.

The remaining catalogue items are acceptance/security/regression specifications, not claims that every workflow is already automated. Authentication, admin, cart mutation, order creation and payments need isolated seeded test data, and payment tests must use a sandbox. Expand automation in layers after confirming the exact UI selectors, admin authorization policy and payment provider behavior.
