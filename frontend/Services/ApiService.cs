using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using LadiesDressStore.Web.Models;

namespace LadiesDressStore.Web.Services;

public class ApiService(
    IHttpClientFactory factory,
    IHttpContextAccessor accessor)
{
    private HttpClient Client =>
        factory.CreateClient("Api");


    // =====================================================
    // ADD JWT TOKEN
    // =====================================================

   private void AddToken(HttpRequestMessage request)
{
    var context = accessor.HttpContext;
    var token = context?.Request.Cookies["access_token"];

    // Also read the token from the protected frontend auth ticket. This
    // fallback keeps API calls authenticated when the standalone token
    // cookie is unavailable.
    if (string.IsNullOrWhiteSpace(token))
    {
        token = context?.User.FindFirst("ApiAccessToken")?.Value;
    }

    if (!string.IsNullOrWhiteSpace(token))
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}


    // =====================================================
    // PRODUCTS
    // =====================================================

    public async Task<List<ProductViewModel>> GetProductsAsync(
        string? search = null,
        string? category = null,
        string? size = null,
        string? color = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? availability = null,
        bool bestSellers = false,
        string? sort = null)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(category))
            query.Add($"category={Uri.EscapeDataString(category.Trim())}");
        if (!string.IsNullOrWhiteSpace(size))
            query.Add($"size={Uri.EscapeDataString(size.Trim())}");
        if (!string.IsNullOrWhiteSpace(color))
            query.Add($"color={Uri.EscapeDataString(color.Trim())}");
        if (minPrice.HasValue)
            query.Add($"minPrice={minPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (maxPrice.HasValue)
            query.Add($"maxPrice={maxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(availability))
            query.Add($"availability={Uri.EscapeDataString(availability.Trim())}");
        if (bestSellers)
            query.Add("bestSellers=true");
        if (!string.IsNullOrWhiteSpace(sort))
            query.Add($"sort={Uri.EscapeDataString(sort.Trim())}");

        var url = "api/products" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        return await Client.GetFromJsonAsync<List<ProductViewModel>>(url) ?? [];
    }


    public async Task<ProductViewModel?>
        GetProductAsync(int id)
    {
        return await Client
            .GetFromJsonAsync<
                ProductViewModel>(
                $"api/products/{id}");
    }

    public async Task<ProductReviewsViewModel> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 2, int? rating = null, string? sort = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 20);
        var query = $"page={page}&pageSize={pageSize}";
        if (rating is >= 1 and <= 5) query += $"&rating={rating}";
        if (!string.IsNullOrWhiteSpace(sort)) query += $"&sort={Uri.EscapeDataString(sort)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/products/{productId}/reviews?{query}");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Reviews API returned {(int)response.StatusCode} ({response.StatusCode}).", null, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<ProductReviewsViewModel>()
            ?? new ProductReviewsViewModel();
    }

    public async Task<(bool Success, string Message)> SubmitProductReviewAsync(
        int productId,
        ProductReviewSubmissionViewModel model)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/products/{productId}/reviews");
        AddToken(request);
        var content = new MultipartFormDataContent
        {
            { new StringContent(model.Rating.ToString()), "Rating" },
            { new StringContent(model.Title ?? string.Empty), "Title" },
            { new StringContent(model.Comment ?? string.Empty), "Comment" }
        };
        foreach (var image in model.Images)
        {
            var imageContent = new StreamContent(image.OpenReadStream());
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType);
            content.Add(imageContent, "Images", image.FileName);
        }
        request.Content = content;

        using var response = await Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? (true, string.Empty)
            : (false, FormatReviewError(response.StatusCode, body));
    }

    public async Task<(bool Success, ProductReviewVoteResultViewModel? Result, HttpStatusCode Status)> VoteForProductReviewAsync(int reviewId, int voteType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/products/reviews/{reviewId}/vote")
        {
            Content = JsonContent.Create(new { voteType })
        };
        AddToken(request);
        using var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return (false, null, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProductReviewVoteResultViewModel>();
        return (result is not null, result, response.StatusCode);
    }

    private static string FormatReviewError(HttpStatusCode statusCode, string body)
    {
        if (statusCode == HttpStatusCode.Unauthorized)
            return "Please sign in again before submitting a review.";
        if (statusCode == HttpStatusCode.Forbidden)
            return "Please purchase this product before submitting a review.";
        if (statusCode == HttpStatusCode.Conflict)
            return "You have already reviewed this product.";
        if (statusCode == HttpStatusCode.NotFound)
            return "This product is no longer available, so we couldn't submit your review.";

        var message = body.Trim();
        if (message.StartsWith('{') || message.StartsWith('"'))
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                if (document.RootElement.ValueKind == JsonValueKind.String)
                {
                    message = document.RootElement.GetString() ?? string.Empty;
                }
                else if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    message = ReadString(document.RootElement, "detail")
                        ?? ReadString(document.RootElement, "message")
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(message) &&
                        document.RootElement.TryGetProperty("errors", out var errors) &&
                        errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var error in errors.EnumerateObject())
                        {
                            if (error.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var value in error.Value.EnumerateArray())
                                {
                                    if (value.ValueKind == JsonValueKind.String &&
                                        !string.IsNullOrWhiteSpace(value.GetString()))
                                    {
                                        message = value.GetString()!;
                                        break;
                                    }
                                }
                            }
                            else if (error.Value.ValueKind == JsonValueKind.String)
                            {
                                message = error.Value.GetString() ?? string.Empty;
                            }

                            if (!string.IsNullOrWhiteSpace(message))
                                break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(message))
                        message = ReadString(document.RootElement, "title") ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                message = string.Empty;
            }
        }

        var normalized = message.Trim().Trim('"').ToLowerInvariant();
        if (normalized.Contains("already reviewed", StringComparison.Ordinal))
            return "You have already reviewed this product.";
        if (normalized.Contains("purchase", StringComparison.Ordinal) ||
            normalized.Contains("eligible", StringComparison.Ordinal))
            return "Please purchase this product before submitting a review.";
        if (normalized.Contains("rating", StringComparison.Ordinal))
            return "Please choose a rating from 1 to 5 stars.";
        if (normalized.Contains("too long", StringComparison.Ordinal))
            return "Please shorten your review and try again.";

        if (statusCode == HttpStatusCode.BadRequest)
            return "Please check your rating and review, then try again.";

        return "We couldn't submit your review right now. Please try again.";
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;


    public async Task<List<RelatedProductViewModel>> GetRelatedProductsAsync(int id)
    {
        return await Client.GetFromJsonAsync<List<RelatedProductViewModel>>($"api/products/{id}/related") ?? [];
    }


    public async Task<List<ProductViewModel>>
        GetNewarrivalsAsync()
    {
        return await Client
            .GetFromJsonAsync<
                List<ProductViewModel>>(
                "api/products/GetNewarrivals")
            ?? [];
    }

    public async Task<List<CategoryViewModel>> GetHomeCategoriesAsync()
    {
        return await Client.GetFromJsonAsync<List<CategoryViewModel>>("api/categories/home") ?? [];
    }

    public async Task<List<CategoryViewModel>> GetCategoriesAsync()
    {
        return await Client.GetFromJsonAsync<List<CategoryViewModel>>("api/categories") ?? [];
    }

    public async Task<List<ProductViewModel>> GetWishlistAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/wishlist");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Wishlist API returned {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<List<ProductViewModel>>() ?? [];
    }

    public async Task<bool> AddToWishlistAsync(int productId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/wishlist/{productId}");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveFromWishlistAsync(int productId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/wishlist/{productId}");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }


    // =====================================================
    // SIGNUP
    // =====================================================

    public async Task<(
        bool Success,
        string Message)>
        SignupAsync(
            SignupViewModel model)
    {
        var response =
            await Client.PostAsJsonAsync(
                "api/auth/signup",
                new
                {
                    name = model.Name,

                    email = model.Email,

                    mobile = model.Mobile,

                    password = model.Password,

                    age = model.Age
                });

        if (!response.IsSuccessStatusCode)
        {
            return (
                false,
                await response.Content
                    .ReadAsStringAsync());
        }

        return (
            true,
            "");
    }

    public async Task<(bool Success, string Message)> VerifySignupEmailAsync(string email, string code)
    {
        using var response = await Client.PostAsJsonAsync("api/auth/signup/verify-email", new { email, code });
        return (response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string Message)> ResendSignupCodeAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync("api/auth/signup/resend-otp", new { email });
        return (response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string Message)> SendMyEmailVerificationAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/me/email-verification/send");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        return (response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string Message)> ConfirmMyEmailVerificationAsync(string code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/me/email-verification/confirm");
        AddToken(request);
        request.Content = JsonContent.Create(new { code });
        using var response = await Client.SendAsync(request);
        return (response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }


    // =====================================================
    // LOGIN
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        AuthResponse? Data)>
        LoginAsync(
            LoginViewModel model)
    {
        var response =
            await Client.PostAsJsonAsync(
                "api/auth/login",
                new
                {
                    identifier = model.Identifier,

                    password = model.Password
                });

        if (!response.IsSuccessStatusCode)
        {
            return (
                false,
                await response.Content
                    .ReadAsStringAsync(),
                null);
        }

        var data =
            await response.Content
                .ReadFromJsonAsync<
                    AuthResponse>();

        return (
            true,
            "",
            data);
    }


    // =====================================================
    // GET PROFILE
    // =====================================================

  public async Task<(
    bool Success,
    int StatusCode,
    string Message,
    ProfileViewModel? Data)> GetMyProfileAsync()
{
    var request = new HttpRequestMessage(
        HttpMethod.Get,
        "api/auth/me");

    AddToken(request);

    var response = await Client.SendAsync(request);

    var body =
        await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        return (
            false,
            (int)response.StatusCode,
            body,
            null);
    }

    var profile =
        System.Text.Json.JsonSerializer
            .Deserialize<ProfileViewModel>(
                body,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

    return (
        true,
        (int)response.StatusCode,
        "",
        profile);
}

    // =====================================================
    // UPDATE PROFILE
    // =====================================================

    public async Task<(
        bool Success,
        string Message,
        ProfileViewModel? Data)>
        UpdateProfileAsync(
            ProfileViewModel model)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                "api/auth/me");

        AddToken(request);

        request.Content =
            JsonContent.Create(
                new
                {
                    name = model.Name,

                    mobile = model.Mobile,

                    age = model.Age,

                    dateOfBirth = model.DateOfBirth,

                    gender = model.Gender
                });

        var response =
            await Client.SendAsync(request);

        var content =
            await response.Content
                .ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (
                false,
                content,
                null);
        }

        var data =
            JsonSerializer.Deserialize<
                ProfileViewModel>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                });

        return (
            true,
            "",
            data);
    }


    // =====================================================
    // CREATE ORDER
    // =====================================================

    public async Task<(
        bool Success,
        string Message)>
        CreateOrderAsync(
            List<CartItemViewModel> cart,
            string address)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/orders");

        AddToken(request);

        request.Content =
            JsonContent.Create(
                new
                {
                    shippingAddress = address,

                    items = cart.Select(
                        x => new
                        {
                            productId =
                                x.ProductId,

                            quantity =
                                x.Quantity,

                            size =
                                x.Size,

                            color =
                                (string?)null
                        })
                });

        var response =
            await Client.SendAsync(request);

        return response.IsSuccessStatusCode
            ? (true, "")
            : (
                false,
                await response.Content
                    .ReadAsStringAsync());
    }


    // =====================================================
    // RAZORPAY PAYMENT
    // =====================================================

    public async Task<(bool Success, string Message, PaymentCheckoutViewModel? Data)>
        CreateRazorpayOrderAsync(List<CartItemViewModel> cart, string address)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/create-order");
        AddToken(request);
        request.Content = JsonContent.Create(new
        {
            shippingAddress = address,
            items = cart.Select(x => new
            {
                productId = x.ProductId,
                quantity = x.Quantity,
                size = x.Size,
                color = (string?)null
            })
        });

        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return (false, content, null);

        var data = JsonSerializer.Deserialize<PaymentCheckoutViewModel>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return data is null ? (false, "Invalid payment response from server.", null) : (true, "", data);
    }


    public async Task<(bool Success, string Message, int OrderId)>
        CreateTestSuccessfulPaymentAsync(
            List<CartItemViewModel> cart,
            string address)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/test-success");
        AddToken(request);
        request.Content = JsonContent.Create(new
        {
            shippingAddress = address,
            items = cart.Select(x => new
            {
                productId = x.ProductId,
                quantity = x.Quantity,
                size = x.Size,
                color = (string?)null
            })
        });

        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (false, content, 0);

        var data = JsonSerializer.Deserialize<JsonElement>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var orderId = data.TryGetProperty("orderId", out var id)
            ? id.GetInt32()
            : 0;

        return orderId > 0
            ? (true, "", orderId)
            : (false, "Invalid test payment response.", 0);
    }


    public async Task<(bool Success, string Message, int OrderId)>
        VerifyRazorpayPaymentAsync(PaymentVerifyViewModel model)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/verify");
        AddToken(request);
        request.Content = JsonContent.Create(new
        {
            orderId = model.OrderId,
            razorpayOrderId = model.RazorpayOrderId,
            razorpayPaymentId = model.RazorpayPaymentId,
            razorpaySignature = model.RazorpaySignature
        });

        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return (false, content, model.OrderId);

        return (true, "", model.OrderId);
    }


    // =====================================================
    // SAVED ADDRESSES
    // =====================================================

    public async Task<List<SavedAddressViewModel>> GetMyAddressesAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/addresses");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Addresses API returned {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);

        return await response.Content.ReadFromJsonAsync<List<SavedAddressViewModel>>() ?? [];
    }

    public async Task<(bool Success, string Message, SavedAddressViewModel? Data)> SaveAddressAsync(SaveAddressViewModel model)
    {
        var method = model.Id > 0 ? HttpMethod.Put : HttpMethod.Post;
        var url = model.Id > 0 ? $"api/addresses/{model.Id}" : "api/addresses";
        using var request = new HttpRequestMessage(method, url);
        AddToken(request);
        request.Content = JsonContent.Create(new
        {
            name = model.Name,
            mobile = model.Mobile,
            addressLine1 = model.AddressLine1,
            addressLine2 = model.AddressLine2,
            city = model.City,
            state = model.State,
            postalCode = model.PostalCode,
            country = model.Country,
            isDefault = model.IsDefault
        });

        using var response = await Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return (false, body, null);

        var address = JsonSerializer.Deserialize<SavedAddressViewModel>(body, new JsonSerializerOptions
        { PropertyNameCaseInsensitive = true });
        return (address is not null, address is null ? "Invalid address response." : "", address);
    }

    public async Task<(bool Success, string Message)> DeleteAddressAsync(int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/addresses/{id}");
        AddToken(request);
        using var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode
            ? (true, "")
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(ChangePasswordViewModel model)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password");
        AddToken(request);
        request.Content = JsonContent.Create(new
        {
            currentPassword = model.CurrentPassword,
            newPassword = model.NewPassword
        });
        using var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode
            ? (true, "")
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync("api/auth/forgot-password", new { email });
        return response.IsSuccessStatusCode
            ? (true, "")
            : (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordViewModel model)
    {
        using var response = await Client.PostAsJsonAsync("api/auth/reset-password", new
        {
            email = model.Email,
            token = model.Token,
            newPassword = model.NewPassword
        });
        return response.IsSuccessStatusCode
            ? (true, "")
            : (false, await response.Content.ReadAsStringAsync());
    }

    // =====================================================
    // MY ORDERS
    // =====================================================

    public async Task<string>
        GetMyOrdersAsync()
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "api/orders/my");

        AddToken(request);

        var response =
            await Client.SendAsync(request);

        return await response.Content
            .ReadAsStringAsync();
    }


    public async Task<(bool Success, string Message)> UpdateMyProfileAsync(
    ProfileViewModel model)
{
    var request = new HttpRequestMessage(
        HttpMethod.Put,
        "api/auth/me");

    AddToken(request);

    request.Content = JsonContent.Create(new
    {
        name = model.Name,
        email = model.Email,
        mobile = model.Mobile,
        age = model.Age,
        dateOfBirth = model.DateOfBirth,
        gender = model.Gender
    });

    var response = await Client.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        return (
            false,
            await response.Content.ReadAsStringAsync());
    }

    return (true, "");
}
}
