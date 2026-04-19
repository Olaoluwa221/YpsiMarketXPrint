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

        public async Task SendOrderStatusUpdateAsync(string toEmail, int orderId, string newStatus)
        {
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
