using System.Globalization;
using System.Net;
using System.Net.Mail;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Services;

public interface IOrderConfirmationEmailService
{
    Task SendOrderConfirmationAsync(Order order, Customer customer);
}

public sealed class OrderConfirmationEmailService(
    ApplicationDbContext db,
    IConfiguration configuration,
    ILogger<OrderConfirmationEmailService> logger) : IOrderConfirmationEmailService
{
    public async Task SendOrderConfirmationAsync(Order order, Customer customer)
    {
        var recipient = customer.Email?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogInformation("Order confirmation email skipped for order {OrderId}: customer has no email address.", order.Id);
            return;
        }

        var smtp = configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var from = smtp["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogWarning("Order {OrderId} was placed, but confirmation email was skipped because SMTP is not configured.", order.Id);
            return;
        }

        var currency = CultureInfo.GetCultureInfo("en-IN");
        var greetingName = string.IsNullOrWhiteSpace(customer.Name) ? "there" : customer.Name.Trim();
        var itemLines = order.Items.Select(item =>
            $"- {item.ProductName} | Size: {item.Size ?? "â€”"} | Qty: {item.Quantity} | {(item.Price * item.Quantity).ToString("C", currency)}");
        var plainText = $"""
            Hi {greetingName},

            Your order #{order.Id} has been placed successfully. Weâ€™re getting it ready.
            Weâ€™ll share your tracking details as soon as your order is ready to ship.

            ORDER SUMMARY
            {string.Join(Environment.NewLine, itemLines)}

            Order total: {order.TotalAmount.ToString("C", currency)}
            Delivery address: {order.ShippingAddress}

            PARCEL ARRIVAL TIP
            If possible, record a continuous unboxing video showing the sealed parcel and the item as you unpack it.
            If an item is damaged, defective, missing, or incorrect, keep the packaging and take 5-6 clear photos
            of the item, issue, parcel label, and outer packaging. Contact us with your order number.
            A video is helpful but is not a strict requirement for raising a concern. See our Returns, Refunds &
            Product Issues Policy, or email ubhayafashions@gmail.com for help.

            Thank you for shopping with Ubhaya Fashions.
            """;

        var encodedName = WebUtility.HtmlEncode(greetingName);
        var encodedAddress = WebUtility.HtmlEncode(order.ShippingAddress);
        var productImageCells = new Dictionary<int, string>();
        try
        {
            var productIds = order.Items.Select(item => item.ProductId).Distinct().ToArray();
            var productImages = await db.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, product => product.ImageUrl);
            var publicBaseUrl = configuration["FrontendUrl"]?.TrimEnd('/');

            foreach (var (productId, imagePath) in productImages)
            {
                if (string.IsNullOrWhiteSpace(imagePath)) continue;
                var imageUrl = Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteImageUrl)
                    ? absoluteImageUrl.AbsoluteUri
                    : string.IsNullOrWhiteSpace(publicBaseUrl)
                        ? null
                        : $"{publicBaseUrl}/{imagePath.TrimStart('/')}";
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                productImageCells[productId] = $"<td style=\"width:76px;padding:10px;border-bottom:1px solid #eee5e1;\"><img src=\"{WebUtility.HtmlEncode(imageUrl)}\" alt=\"\" width=\"64\" style=\"display:block;width:64px;height:auto;border-radius:6px;\"></td>";
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Product images could not be loaded for order {OrderId}; sending confirmation without images.", order.Id);
        }
        var itemRows = string.Join("", order.Items.Select(item =>
        {
            var imageCell = productImageCells.GetValueOrDefault(item.ProductId)
                ?? "<td style=\"width:76px;padding:10px;border-bottom:1px solid #eee5e1;\"></td>";
            return $"""
            <tr>
              {imageCell}
              <td style="padding:12px 10px;border-bottom:1px solid #eee5e1;color:#39272b;">{WebUtility.HtmlEncode(item.ProductName)}<br><small style="color:#80645f;">Size: {WebUtility.HtmlEncode(item.Size ?? "â€”")}</small></td>
              <td style="padding:12px 10px;border-bottom:1px solid #eee5e1;text-align:center;color:#39272b;">{item.Quantity}</td>
              <td style="padding:12px 10px;border-bottom:1px solid #eee5e1;text-align:right;color:#39272b;">{(item.Price * item.Quantity).ToString("C", currency)}</td>
            </tr>
            """;
        }));
        var html = $"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#f8f3f0;font-family:Arial,Helvetica,sans-serif;color:#302522;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f8f3f0;padding:32px 12px;">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;border:1px solid #efdfdf;border-radius:12px;background:#fffdfb;overflow:hidden;">
                    <tr><td style="padding:26px 30px;background:linear-gradient(110deg,#68142e,#963451);text-align:center;color:#fff;">
                      <div style="font-size:12px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:#f0d8a7;">Ubhaya Fashions</div>
                      <h1 style="margin:12px 0 0;font-family:Georgia,'Times New Roman',serif;font-size:30px;font-weight:400;">Your order is confirmed</h1>
                    </td></tr>
                    <tr><td style="padding:30px;">
                      <p style="margin:0 0 12px;font-size:16px;">Hi {encodedName},</p>
                      <p style="margin:0;font-size:15px;line-height:1.7;">Your order <strong style="color:#68142e;">#{order.Id}</strong> has been placed successfully. Weâ€™re getting it ready.</p>
                      <p style="margin:12px 0 24px;font-size:14px;line-height:1.7;color:#665557;">Weâ€™ll share your tracking details as soon as your order is ready to ship.</p>
                      <div style="padding:16px;border:1px solid #efdfdf;border-radius:8px;background:#fffaf8;">
                        <div style="margin-bottom:10px;color:#9a6b64;font-size:11px;font-weight:700;letter-spacing:1.5px;">ORDER SUMMARY</div>
                        <div style="margin-bottom:14px;color:#68142e;font-size:16px;font-weight:700;">Order #{order.Id}</div>
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;font-size:13px;">
                          <thead><tr><th style="padding:8px 10px;border-bottom:1px solid #e7d8d3;color:#80645f;font-size:11px;">Image</th><th align="left" style="padding:8px 10px;border-bottom:1px solid #e7d8d3;color:#80645f;font-size:11px;">Item</th><th style="padding:8px 4px;border-bottom:1px solid #e7d8d3;color:#80645f;font-size:11px;">Qty</th><th align="right" style="padding:8px 10px;border-bottom:1px solid #e7d8d3;color:#80645f;font-size:11px;">Amount</th></tr></thead>
                          <tbody>{itemRows}</tbody>
                        </table>
                        <p style="margin:16px 0 0;text-align:right;font-size:15px;">Total <strong style="color:#68142e;">{order.TotalAmount.ToString("C", currency)}</strong></p>
                      </div>
                      <div style="margin-top:20px;">
                        <div style="margin-bottom:6px;color:#9a6b64;font-size:11px;font-weight:700;letter-spacing:1.2px;">DELIVERING TO</div>
                        <div style="font-size:13px;line-height:1.6;color:#665557;">{encodedAddress}</div>
                      </div>
                      <div style="margin-top:24px;padding:20px;border:1px solid #e5c7c3;border-radius:14px;background:#fffaf8;">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="border-collapse:collapse;">
                          <tr>
                            <td valign="middle" style="width:48px;padding-right:12px;">
                              <div style="width:42px;height:42px;border-radius:12px;background:#f5e5e3;color:#813c40;text-align:center;font-size:22px;line-height:42px;">&#128247;</div>
                            </td>
                            <td valign="middle">
                              <div style="margin-bottom:3px;color:#9a6b64;font-size:10px;font-weight:700;letter-spacing:1.5px;">A LITTLE PRECAUTION</div>
                              <h2 style="margin:0;color:#492d30;font-family:Georgia,'Times New Roman',serif;font-size:20px;line-height:1.3;">Care Instruction Guide</h2>
                            </td>
                          </tr>
                        </table>
                        <p style="margin:14px 0 12px;font-size:13px;line-height:1.7;color:#665557;">If there’s a problem with your parcel, these simple steps can help us understand it and assist you.</p>
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="border-collapse:separate;border-spacing:0 8px;">
                          <tr>
                            <td valign="top" style="width:38px;padding:12px 8px;border:1px solid #efdfdd;border-right:0;border-radius:10px 0 0 10px;background:#fff;">
                              <div style="width:30px;height:30px;border-radius:50%;background:#f5e5e3;color:#813c40;text-align:center;font-weight:700;font-size:13px;line-height:30px;">1</div>
                            </td>
                            <td valign="top" style="padding:12px;border:1px solid #efdfdd;border-left:0;border-radius:0 10px 10px 0;background:#fff;">
                              <div style="margin:0 0 4px;color:#492d30;font-size:14px;font-weight:700;">Record an unboxing video</div>
                              <div style="font-size:12px;line-height:1.65;color:#746668;">If possible, record a continuous video showing the sealed parcel and the item as you unpack it.</div>
                            </td>
                          </tr>
                          <tr>
                            <td valign="top" style="width:38px;padding:12px 8px;border:1px solid #efdfdd;border-right:0;border-radius:10px 0 0 10px;background:#fff;">
                              <div style="width:30px;height:30px;border-radius:50%;background:#f5e5e3;color:#813c40;text-align:center;font-weight:700;font-size:13px;line-height:30px;">2</div>
                            </td>
                            <td valign="top" style="padding:12px;border:1px solid #efdfdd;border-left:0;border-radius:0 10px 10px 0;background:#fff;">
                              <div style="margin:0 0 4px;color:#492d30;font-size:14px;font-weight:700;">Take 5–6 clear photos</div>
                              <div style="font-size:12px;line-height:1.65;color:#746668;">Show the item, the issue, the parcel label, and the outer packaging if anything is damaged, defective, missing, or incorrect.</div>
                            </td>
                          </tr>
                          <tr>
                            <td valign="top" style="width:38px;padding:12px 8px;border:1px solid #efdfdd;border-right:0;border-radius:10px 0 0 10px;background:#fff;">
                              <div style="width:30px;height:30px;border-radius:50%;background:#f5e5e3;color:#813c40;text-align:center;font-weight:700;font-size:13px;line-height:30px;">3</div>
                            </td>
                            <td valign="top" style="padding:12px;border:1px solid #efdfdd;border-left:0;border-radius:0 10px 10px 0;background:#fff;">
                              <div style="margin:0 0 4px;color:#492d30;font-size:14px;font-weight:700;">Keep packaging and contact us</div>
                              <div style="font-size:12px;line-height:1.65;color:#746668;">Keep the original packaging and email <a href="mailto:ubhayafashions@gmail.com" style="color:#813c40;font-weight:700;">ubhayafashions@gmail.com</a> with your order number.</div>
                            </td>
                          </tr>
                        </table>
                        <div style="margin-top:8px;padding:13px 14px;border:1px solid #ead8c8;border-radius:10px;background:#f8f0e8;">
                          <div style="margin:0 0 4px;color:#694c42;font-size:13px;font-weight:700;">No video? That’s okay.</div>
                          <div style="font-size:12px;line-height:1.7;color:#665557;">A video is helpful, but it is not a strict requirement for raising a concern. Please see our Returns, Refunds &amp; Product Issues Policy on the website.</div>
                        </div>
                      </div>
                      <p style="margin:24px 0 0;font-size:13px;line-height:1.6;color:#665557;">Thank you for choosing Ubhaya Fashions. You can check your order status anytime in <strong>My Orders</strong>.</p>
                    </td></tr>
                    <tr><td style="padding:16px 24px;border-top:1px solid #eee5e1;text-align:center;color:#9a918a;font-size:12px;">Tradition meets today Â· Ubhaya Fashions</td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from, "Ubhaya Fashions"),
                Subject = $"Order #{order.Id} confirmed - Ubhaya Fashions",
                Body = html,
                IsBodyHtml = true
            };
            message.To.Add(recipient);
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain"));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));

            var port = int.TryParse(smtp["Port"], out var configuredPort) ? configuredPort : 587;
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(smtp["EnableSsl"], out var enableSsl) ? enableSsl : true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var username = smtp["Username"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, smtp["Password"]);

            await client.SendMailAsync(message);
            logger.LogInformation("Order confirmation email sent for order {OrderId} to customer {CustomerId}.", order.Id, customer.Id);
        }
        catch (Exception exception)
        {
            // A mail delivery issue must never undo a committed order or payment.
            logger.LogError(exception, "Order {OrderId} was placed, but its confirmation email could not be sent.", order.Id);
        }
    }
}


