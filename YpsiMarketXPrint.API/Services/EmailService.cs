using Resend;

namespace YpsiMarketXPrint.API.Services
{
    public class EmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _config;

        public EmailService(IResend resend, IConfiguration config)
        {
            _resend = resend;
            _config = config;
        }

        private string From => _config["Resend:FromEmail"]!;
        private string? ReplyTo => _config["Resend:ReplyTo"];

        public async Task SendOrderConfirmationAsync(string toEmail, int orderId, decimal total)
        {
            var message = new EmailMessage();
            message.From = From;
            message.To.Add(toEmail);
            message.Subject = $"Order #{orderId} confirmed — Ypsi Marketing & Print";
            message.HtmlBody = $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #1B2A4A; padding: 24px; text-align: center;">
                            <h1 style="color: white; margin: 0; font-size: 24px;">Ypsi Marketing & Print</h1>
                        </div>
                        <div style="padding: 32px; background: #f9f9f9;">
                            <h2 style="color: #1B2A4A;">Order confirmed!</h2>
                            <p style="color: #555;">Thank you for your order. We'll get started on it right away.</p>
                            <div style="background: white; border-radius: 8px; padding: 20px; margin: 24px 0; border: 1px solid #eee;">
                                <p style="margin: 0; color: #888; font-size: 14px;">Order number</p>
                                <p style="margin: 4px 0 0; font-size: 24px; font-weight: bold; color: #1B2A4A;">#{orderId}</p>
                            </div>
                            <div style="background: white; border-radius: 8px; padding: 20px; border: 1px solid #eee;">
                                <p style="margin: 0; color: #888; font-size: 14px;">Order total</p>
                                <p style="margin: 4px 0 0; font-size: 24px; font-weight: bold; color: #E8620A;">${total:F2}</p>
                            </div>
                            <p style="color: #555; margin-top: 24px;">We'll notify you when your order is ready.</p>
                        </div>
                        <div style="background: #1B2A4A; padding: 16px; text-align: center;">
                            <p style="color: #8899bb; margin: 0; font-size: 12px;">© 2026 Ypsi Marketing & Print Company</p>
                        </div>
                    </div>
                """;

            await _resend.EmailSendAsync(message);
        }

        public async Task SendOrderStatusUpdateAsync(
            string toEmail,
            int orderId,
            string newStatus,
            decimal total = 0
        )
        {
            if (newStatus == "readyforpickup")
            {
                await SendPickupReadyAsync(toEmail, orderId, total);
                return;
            }
            var message = new EmailMessage();
            message.From = From;
            message.To.Add(toEmail);
            message.Subject = $"Order #{orderId} update — {newStatus}";
            message.HtmlBody = $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #1B2A4A; padding: 24px; text-align: center;">
                            <h1 style="color: white; margin: 0; font-size: 24px;">Ypsi Marketing & Print</h1>
                        </div>
                        <div style="padding: 32px; background: #f9f9f9;">
                            <h2 style="color: #1B2A4A;">Your order has been updated</h2>
                            <p style="color: #555;">Order <strong>#{orderId}</strong> is now <strong style="color: #E8620A; text-transform: capitalize;">{newStatus}</strong>.</p>
                            <p style="color: #555;">Thank you for choosing Ypsi Marketing & Print!</p>
                        </div>
                        <div style="background: #1B2A4A; padding: 16px; text-align: center;">
                            <p style="color: #8899bb; margin: 0; font-size: 12px;">© 2026 Ypsi Marketing & Print Company</p>
                        </div>
                    </div>
                """;

            await _resend.EmailSendAsync(message);
        }

        public async Task SendPasswordResetAsync(string toEmail, string token)
        {
            var resetUrl = $"http://localhost:5173/reset-password?token={token}";

            var message = new EmailMessage();
            message.From = From;
            message.To.Add(toEmail);
            message.Subject = "Reset your password — Ypsi Marketing & Print";
            message.HtmlBody = $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #1B2A4A; padding: 24px; text-align: center;">
                            <h1 style="color: white; margin: 0; font-size: 24px;">Ypsi Marketing & Print</h1>
                        </div>
                        <div style="padding: 32px; background: #f9f9f9;">
                            <h2 style="color: #1B2A4A;">Reset your password</h2>
                            <p style="color: #555;">We received a request to reset your password. Click the button below to choose a new one.</p>
                            <a href="{resetUrl}" style="display: inline-block; margin: 24px 0; padding: 14px 32px; background: #E8620A; color: white; text-decoration: none; border-radius: 8px; font-weight: bold;">
                                Reset password
                            </a>
                            <p style="color: #888; font-size: 13px;">This link expires in 1 hour. If you didn't request a password reset, you can ignore this email.</p>
                        </div>
                        <div style="background: #1B2A4A; padding: 16px; text-align: center;">
                            <p style="color: #8899bb; margin: 0; font-size: 12px;">© 2026 Ypsi Marketing & Print Company</p>
                        </div>
                    </div>
                """;

            await _resend.EmailSendAsync(message);
        }

        public async Task SendPickupReadyAsync(string toEmail, int orderId, decimal total)
        {
            var calendlyUrl = _config["Calendly:PickupUrl"] ?? "https://calendly.com";

            var message = new EmailMessage();
            message.From = From;
            message.To.Add(toEmail);
            message.Subject = $"Your order #{orderId} is ready for pickup!";
            message.HtmlBody = $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #1B2A4A; padding: 24px; text-align: center;">
                            <h1 style="color: white; margin: 0; font-size: 24px;">Ypsi Marketing & Print</h1>
                        </div>
                        <div style="padding: 32px; background: #f9f9f9;">
                            <h2 style="color: #1B2A4A;">Your order is ready! 🎉</h2>
                            <p style="color: #555;">Great news — order <strong>#{orderId}</strong> is ready for pickup.</p>
                            <div style="background: white; border-radius: 8px; padding: 20px; margin: 24px 0; border: 1px solid #eee;">
                                <p style="margin: 0; color: #888; font-size: 14px;">Order total</p>
                                <p style="margin: 4px 0 0; font-size: 24px; font-weight: bold; color: #E8620A;">${total:F2}</p>
                            </div>
                            <p style="color: #555;">Please schedule your pickup appointment using the link below:</p>
                            <div style="text-align: center; margin: 32px 0;">
                                <a href="{calendlyUrl}" 
                                   style="background: #E8620A; color: white; padding: 16px 32px; border-radius: 10px; text-decoration: none; font-weight: bold; font-size: 16px; display: inline-block;">
                                    📅 Schedule Pickup
                                </a>
                            </div>
                            <p style="color: #888; font-size: 14px;">If the button doesn't work, copy this link: <a href="{calendlyUrl}" style="color: #E8620A;">{calendlyUrl}</a></p>
                        </div>
                        <div style="background: #1B2A4A; padding: 16px; text-align: center;">
                            <p style="color: #8899bb; margin: 0; font-size: 12px;">© 2026 Ypsi Marketing & Print Company</p>
                        </div>
                    </div>
                """;

            await _resend.EmailSendAsync(message);
        }

        public async Task SendArtworkUploadRequestAsync(
            string toEmail,
            int orderId,
            List<(string ProductName, string Size, string UploadUrl)> items
        )
        {
            var itemsHtml = string.Join("", items.Select(i => $"""
                <div style="background: white; border-radius: 8px; padding: 16px; margin: 12px 0; border: 1px solid #eee;">
                    <p style="margin: 0 0 4px; font-weight: bold; color: #1B2A4A;">{i.ProductName}</p>
                    <p style="margin: 0 0 12px; color: #888; font-size: 14px;">Size: {i.Size}</p>
                    <a href="{i.UploadUrl}"
                       style="display: inline-block; padding: 10px 20px; background: #E8620A; color: white; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px;">
                        📎 Upload artwork
                    </a>
                </div>
            """));

            var message = new EmailMessage();
            message.From = From;
            message.To.Add(toEmail);
            message.Subject = $"Artwork needed for order #{orderId} — Ypsi Marketing & Print";
            message.HtmlBody = $"""
                    <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                        <div style="background: #1B2A4A; padding: 24px; text-align: center;">
                            <h1 style="color: white; margin: 0; font-size: 24px;">Ypsi Marketing & Print</h1>
                        </div>
                        <div style="padding: 32px; background: #f9f9f9;">
                            <h2 style="color: #1B2A4A;">We need your artwork</h2>
                            <p style="color: #555;">Thanks for ordering! Before we can start on order <strong>#{orderId}</strong>, we need the artwork for the item(s) below. Click each button to upload.</p>
                            {itemsHtml}
                            <p style="color: #888; font-size: 13px; margin-top: 24px;">Each link can only be used once. If you need a new link, reply to this email and we'll send one.</p>
                        </div>
                        <div style="background: #1B2A4A; padding: 16px; text-align: center;">
                            <p style="color: #8899bb; margin: 0; font-size: 12px;">© 2026 Ypsi Marketing & Print Company</p>
                        </div>
                    </div>
                """;

            await _resend.EmailSendAsync(message);
        }

        public async Task SendPromotionalEmailAsync(
            List<string> recipients,
            string subject,
            string htmlBody
        )
        {
            foreach (var email in recipients)
            {
                var message = new EmailMessage();
                message.From = From;
                message.To.Add(email);
                message.Subject = subject;
                message.HtmlBody = $"""
                        <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto;">
                            <div style="background: #1B2A4A; padding: 24px; text-align: center;">
                                <h1 style="color: white; margin: 0; font-size: 24px;">Ypsi Marketing & Print</h1>
                            </div>
                            <div style="padding: 32px; background: #f9f9f9;">
                                {htmlBody}
                            </div>
                            <div style="background: #1B2A4A; padding: 16px; text-align: center;">
                                <p style="color: #8899bb; margin: 0; font-size: 12px;">© 2026 Ypsi Marketing & Print Company. You're receiving this because you opted in to marketing emails.</p>
                            </div>
                        </div>
                    """;

                await _resend.EmailSendAsync(message);
            }
        }
    }
}
