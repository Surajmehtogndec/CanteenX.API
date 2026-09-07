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
            // Email Body
            message.Body = new BodyBuilder
            {
                HtmlBody = $"""
                    <div style="font-family: Arial, sans-serif; 
                                max-width: 600px; 
                                margin: auto;">

                        <h2 style="color: #dc3545;">
                            CanteenX
                        </h2>

                        <h3>
                            Reset Your Password
                        </h3>

                        <p>
                            We received a request to reset your
                            CanteenX account password.
                        </p>
                            
                        <p>
                            Click the button below to create
                            a new password.
                        </p>

                        <p>
                            <a href="{resetLink}"
                               style="
                               display:inline-block;
                               padding:12px 20px;
                               background:#dc3545;
                               color:white;
                               text-decoration:none;
                               border-radius:6px;
                               font-weight:bold;">
                                Reset Password
                            </a>
                        </p>

                        <p>
                            If you did not request a password reset,
                            you can safely ignore this email.
                        </p>

                        <hr />

                        <p style="color:#777;">
                            Regards,<br />
                            CanteenX Team
                        </p>

                    </div>
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
