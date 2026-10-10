using CanteenX.API.Services.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace CanteenX.API.Services
{
    public class EmailService : IEmailService
    {
    private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync( string email, string resetLink)
        {
             var message = new MimeMessage();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Recipient email cannot be empty.",
                    nameof(email)
                );
            }

            message.From.Add(
          new MailboxAddress(
              "CanteenX",
              _configuration["EmailSettings:FromEmail"]!
          )
      );

            // IMPORTANT
            message.To.Add(
                MailboxAddress.Parse(email)
            );

            // Subject
            message.Subject = "CanteenX - Reset Your Password";

            // Email Body
            message.Body = new BodyBuilder
            {
                HtmlBody = $"""
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    </head>
                    <body style="margin: 0; padding: 0; background-color: #f2f2f7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
                        
                        <table width="100%" border="0" cellspacing="0" cellpadding="0" style="background-color: #f2f2f7; padding: 40px 20px;">
                            <tr>
                                <td align="center">
                                    
                                    <!-- iOS Style Card -->
                                    <table width="100%" max-width="500" border="0" cellspacing="0" cellpadding="0" style="max-width: 500px; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 8px 24px rgba(0,0,0,0.04); border: 1px solid #e5e5ea;">
                                        
                                        <!-- Header -->
                                        <tr>
                                            <td align="center" style="padding: 35px 30px 15px;">
                                                <h2 style="margin: 0; font-size: 24px; font-weight: 700; color: #1c1c1e; letter-spacing: -0.5px;">
                                                    Canteen<span style="color: #e8281a;">X</span>
                                                </h2>
                                            </td>
                                        </tr>

                                        <!-- Body Content -->
                                        <tr>
                                            <td style="padding: 10px 35px 35px; color: #3a3a3c; font-size: 16px; line-height: 1.6; text-align: left;">
                                                
                                                <h3 style="margin-top: 0; font-size: 20px; font-weight: 600; color: #1c1c1e;">
                                                    Reset Your Password
                                                </h3>
                                                
                                                <p style="margin: 0 0 20px 0;">
                                                    We received a request to reset the password for your CanteenX account. Click the button below to create a new, secure password.
                                                </p>

                                                <!-- Expiry Notice Alert (iOS Style) -->
                                                <div style="background-color: #fff0f0; border-radius: 12px; padding: 14px; text-align: center; margin-bottom: 25px; border: 1px solid #ffe0e0;">
                                                    <p style="margin: 0; color: #e8281a; font-size: 14px; font-weight: 600;">
                                                        ⏳ This link will expire in 10 minutes.
                                                    </p>
                                                </div>

                                                <!-- Call to Action Button -->
                                                <table width="100%" border="0" cellspacing="0" cellpadding="0">
                                                    <tr>
                                                        <td align="center">
                                                            <a href="{resetLink}" 
                                                               style="display: inline-block; padding: 15px 30px; background-color: #e8281a; color: #ffffff; text-decoration: none; border-radius: 25px; font-weight: 600; font-size: 16px;">
                                                                Reset Password
                                                            </a>
                                                        </td>
                                                    </tr>
                                                </table>

                                                <p style="margin: 25px 0 0 0; font-size: 15px; color: #8e8e93;">
                                                    If you did not request this password reset, please ignore this email or contact support if you have concerns.
                                                </p>

                                            </td>
                                        </tr>

                                        <!-- Footer -->
                                        <tr>
                                            <td align="center" style="padding: 20px 30px; background-color: #f9f9f9; border-top: 1px solid #ebebf0;">
                                                <p style="margin: 0; color: #8e8e93; font-size: 13px; font-weight: 500;">
                                                    Regards,<br/>
                                                    <strong>The CanteenX Team</strong>
                                                </p>
                                            </td>
                                        </tr>
                                    </table>

                                </td>
                            </tr>
                        </table>

                    </body>
                    </html>
                    """
            }.ToMessageBody();

            // smtp configuration
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"]!,
                int.Parse(
                    _configuration["EmailSettings:Port"]!
                    ),
                SecureSocketOptions.StartTls
                ); 



            // email authentication
            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:Username"]!,
                _configuration["EmailSettings:Password"]!
                );

            // email send karna 
            await smtp.SendAsync(message);

            // smtp disconnect

            await smtp.DisconnectAsync(true);
        }


    }
}
