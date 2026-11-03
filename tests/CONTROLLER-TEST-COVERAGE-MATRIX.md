# UBHAYA FASHIONS – Controller Test Coverage Matrix

This document tracks the implementation status and test case coverage for all Frontend and Backend controllers in Ubhaya Fashions.

> **Note:** Scenario counts and per-action Passed/Failed values are the supplied coverage baseline, not automatically inferred from CI. Keep this matrix synchronized with executed NUnit results. A controller is COMPLETE only when its planned behavioral scenarios are implemented and verified.

## Summary

- **Frontend Controllers**: 12
- **Backend Controllers**: 20
- **Total Controllers**: 32
- **Overall Status**: IN PROGRESS

---

## 1. Frontend Controllers

| Controller | Action Method | Positive | Negative | Boundary | Security | Total | Passed | Failed | Status |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| **AccountController** | Login (GET) | 1 | 0 | 0 | 1 | 2 | 2 | 0 | IN PROGRESS |
| | Login (POST) | 1 | 2 | 0 | 1 | 4 | 4 | 0 | IN PROGRESS |
| | SignupFromLogin | 1 | 1 | 0 | 0 | 2 | 2 | 0 | IN PROGRESS |
| | Signup (GET) | 1 | 0 | 0 | 0 | 1 | 1 | 0 | IN PROGRESS |
| | Signup (POST) | 1 | 2 | 1 | 0 | 4 | 4 | 0 | IN PROGRESS |
| | SendEmailVerification | 1 | 1 | 0 | 1 | 3 | 3 | 0 | IN PROGRESS |
| | ConfirmEmailVerification | 1 | 1 | 0 | 0 | 2 | 2 | 0 | IN PROGRESS |
| | VerifySignupEmail (GET) | 1 | 0 | 0 | 0 | 1 | 1 | 0 | IN PROGRESS |
| | VerifySignupEmail (POST) | 1 | 1 | 0 | 0 | 2 | 2 | 0 | IN PROGRESS |
| | ResendSignupCode | 1 | 1 | 0 | 0 | 2 | 2 | 0 | IN PROGRESS |
| | Profile (GET) | 1 | 1 | 0 | 1 | 3 | 3 | 0 | IN PROGRESS |
| | Profile (POST) | 1 | 1 | 1 | 1 | 4 | 4 | 0 | IN PROGRESS |
| | SaveAddress | 1 | 1 | 1 | 1 | 4 | 4 | 0 | IN PROGRESS |
| | DeleteAddress | 1 | 1 | 0 | 1 | 3 | 3 | 0 | IN PROGRESS |
| | ChangePassword | 1 | 2 | 1 | 1 | 5 | 5 | 0 | IN PROGRESS |
| | DeleteMyAccount | 1 | 2 | 0 | 1 | 4 | 4 | 0 | IN PROGRESS |
| | ForgotPassword (GET) | 1 | 0 | 0 | 0 | 1 | 1 | 0 | IN PROGRESS |
| | ForgotPassword (POST) | 1 | 1 | 0 | 0 | 2 | 2 | 0 | IN PROGRESS |
| | ResetPassword (GET) | 1 | 0 | 0 | 0 | 1 | 1 | 0 | IN PROGRESS |
| | ResetPassword (POST) | 1 | 2 | 0 | 0 | 3 | 3 | 0 | IN PROGRESS |
| | Logout | 1 | 0 | 0 | 0 | 1 | 1 | 0 | IN PROGRESS |
| **AddressesController** | ReverseGeocode | 1 | 1 | 1 | 0 | 3 | 0 | 0 | NOT STARTED |
| | Save | 1 | 1 | 1 | 1 | 4 | 0 | 0 | NOT STARTED |
| | Delete | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| **AdminController** | Index | 1 | 0 | 0 | 0 | 1 | 0 | 0 | NOT STARTED |
| | Login | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | ForgotPassword (GET/POST) | 2 | 1 | 0 | 0 | 3 | 0 | 0 | NOT STARTED |
| | ResetPassword (GET/POST) | 2 | 2 | 0 | 0 | 4 | 0 | 0 | NOT STARTED |
| | ChangePassword | 2 | 2 | 0 | 1 | 5 | 0 | 0 | NOT STARTED |
| | Logout | 1 | 0 | 0 | 0 | 1 | 0 | 0 | NOT STARTED |
| | Dashboard | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | StorefrontBanner | 2 | 1 | 0 | 1 | 4 | 0 | 0 | NOT STARTED |
| | SetStorefrontBannerActive | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | Customers | 2 | 1 | 0 | 1 | 4 | 0 | 0 | NOT STARTED |
| | AdminAccounts | 1 | 1 | 0 | 2 | 4 | 0 | 0 | NOT STARTED |
| | SaveAdminAccount | 1 | 1 | 0 | 2 | 4 | 0 | 0 | NOT STARTED |
| | DeleteAdminAccount | 1 | 1 | 0 | 2 | 4 | 0 | 0 | NOT STARTED |
| | Products | 2 | 1 | 1 | 1 | 5 | 0 | 0 | NOT STARTED |
| | CreateProduct / EditProduct | 2 | 1 | 0 | 1 | 4 | 0 | 0 | NOT STARTED |
| | Categories | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | DeleteCategory | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | DeleteProduct | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | DeleteProductImage | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | Inventory | 1 | 1 | 1 | 1 | 4 | 0 | 0 | NOT STARTED |
| | AdjustStock | 1 | 1 | 1 | 1 | 4 | 0 | 0 | NOT STARTED |
| | Orders / WhatsAppOrders / DelhiveryOrders | 3 | 1 | 0 | 1 | 5 | 0 | 0 | NOT STARTED |
| | UpdateOrder | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | CreateDelhiveryShipment | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | Logs | 1 | 1 | 1 | 1 | 4 | 0 | 0 | NOT STARTED |
| **CartController** | Index | 1 | 0 | 0 | 0 | 1 | 0 | 0 | NOT STARTED |
| | Add | 1 | 2 | 1 | 0 | 4 | 0 | 0 | NOT STARTED |
| | Remove | 1 | 1 | 0 | 0 | 2 | 0 | 0 | NOT STARTED |
| | Update | 1 | 2 | 1 | 0 | 4 | 0 | 0 | NOT STARTED |
| **ClientLogController** | Create | 1 | 1 | 0 | 0 | 2 | 0 | 0 | NOT STARTED |
| **ContactController** | Index (GET/POST) | 2 | 1 | 0 | 0 | 3 | 0 | 0 | NOT STARTED |
| **HomeController** | Privacy / Terms / Returns / Cancellation / About / NotFoundPage / Error / Index | 8 | 1 | 0 | 0 | 9 | 9 | 0 | COMPLETE |
| **NotificationsController** | Index | 1 | 0 | 0 | 1 | 2 | 0 | 0 | NOT STARTED |
| | Recent | 1 | 0 | 0 | 1 | 2 | 0 | 0 | NOT STARTED |
| | Read | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | Dismiss | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| **OrdersController** | Index | 1 | 0 | 0 | 0 | 1 | 0 | 0 | NOT STARTED |
| | Details | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | Cancel | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | Track | 1 | 1 | 0 | 0 | 2 | 0 | 0 | NOT STARTED |
| | Find | 1 | 1 | 0 | 0 | 2 | 0 | 0 | NOT STARTED |
| | Checkout (GET/POST) | 2 | 2 | 1 | 1 | 6 | 0 | 0 | NOT STARTED |
| | Create | 1 | 2 | 1 | 1 | 5 | 0 | 0 | NOT STARTED |
| | Success / Failure | 2 | 1 | 0 | 0 | 3 | 0 | 0 | NOT STARTED |
| | TestPaymentSuccess | 1 | 1 | 0 | 0 | 2 | 0 | 0 | NOT STARTED |
| | Verify | 1 | 2 | 0 | 1 | 4 | 0 | 0 | NOT STARTED |
| **ProductsController** | Categories | 1 | 0 | 0 | 0 | 1 | 0 | 0 | NOT STARTED |
| | Index | 2 | 1 | 1 | 0 | 4 | 0 | 0 | NOT STARTED |
| | Details | 1 | 1 | 0 | 0 | 2 | 0 | 0 | NOT STARTED |
| | SyncRecentlyViewed | 1 | 1 | 1 | 0 | 3 | 0 | 0 | NOT STARTED |
| | VoteReview | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |
| | ReviewPage / Reviews | 2 | 1 | 0 | 0 | 3 | 0 | 0 | NOT STARTED |
| | SubmitReview | 1 | 2 | 0 | 1 | 4 | 0 | 0 | NOT STARTED |
| **ShippingController** | Index | 1 | 0 | 0 | 0 | 1 | 0 | 0 | NOT STARTED |
| **WishlistController** | Index | 1 | 0 | 0 | 1 | 2 | 0 | 0 | NOT STARTED |
| | Toggle | 1 | 1 | 0 | 1 | 3 | 0 | 0 | NOT STARTED |

---

## 2. Backend Controllers

| Controller | Action Method | Positive | Negative | Boundary | Security | Total | Passed | Failed | Status |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| **AddressesController** | All methods | 3 | 2 | 1 | 2 | 8 | 0 | 0 | NOT STARTED |
| **AdminAccountsController** | All methods | 3 | 2 | 0 | 2 | 7 | 0 | 0 | NOT STARTED |
| **AdminAuthController** | All methods | 3 | 2 | 0 | 2 | 7 | 0 | 0 | NOT STARTED |
| **AdminCategoriesController** | All methods | 3 | 2 | 1 | 2 | 8 | 0 | 0 | NOT STARTED |
| **AdminCustomersController** | All methods | 2 | 1 | 0 | 2 | 5 | 0 | 0 | NOT STARTED |
| **AdminDashboardController** | All methods | 2 | 1 | 0 | 2 | 5 | 0 | 0 | NOT STARTED |
| **AdminDelhiveryController** | All methods | 3 | 2 | 1 | 2 | 8 | 0 | 0 | NOT STARTED |
| **AdminInventoryController** | All methods | 3 | 2 | 1 | 2 | 8 | 0 | 0 | NOT STARTED |
| **AdminOrdersController** | All methods | 3 | 2 | 1 | 2 | 8 | 0 | 0 | NOT STARTED |
| **AdminProductsController** | All methods | 4 | 3 | 1 | 2 | 10 | 0 | 0 | NOT STARTED |
| **AuthController** | All methods | 5 | 5 | 1 | 3 | 14 | 14 | 0 | IN PROGRESS |
| **CategoriesController** | All methods | 2 | 1 | 0 | 0 | 3 | 0 | 0 | NOT STARTED |
| **NotificationsController** | All methods | 3 | 2 | 0 | 1 | 6 | 0 | 0 | NOT STARTED |
| **OrdersController** | All methods | 5 | 4 | 2 | 2 | 13 | 13 | 0 | IN PROGRESS |
| **PaymentsController** | All methods | 3 | 2 | 1 | 2 | 8 | 0 | 0 | NOT STARTED |
| **ProductReviewsController** | All methods | 3 | 2 | 1 | 1 | 7 | 0 | 0 | NOT STARTED |
| **ProductsController** | All methods | 4 | 2 | 2 | 0 | 8 | 8 | 0 | IN PROGRESS |
| **RecentlyViewedController** | All methods | 2 | 1 | 0 | 1 | 4 | 0 | 0 | NOT STARTED |
| **StorefrontBannerController** | All methods | 3 | 1 | 0 | 1 | 5 | 0 | 0 | NOT STARTED |
| **WishlistController** | All methods | 3 | 1 | 0 | 1 | 5 | 0 | 0 | NOT STARTED |

## Status definitions

- **NOT STARTED**: No behavioral test cases have been implemented and verified for the action/controller.
- **IN PROGRESS**: Some test cases exist, but the complete positive, negative, boundary, and security coverage has not been verified.
- **COMPLETE**: All applicable planned test cases are implemented and passing in CI.
