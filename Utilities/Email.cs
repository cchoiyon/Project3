using System;
using System.Net.Mail;
using System.Net; // Required for NetworkCredential
// using Microsoft.Extensions.Options; // No longer needed in this class if settings are passed to SendMail
using Project3.Models; // Required for SmtpSettings
using System.Text; // Often needed if manipulating body

namespace Project3.Utilities
{
    public class Email
    {
        // Removed constructor injection for now, settings passed via SendMail method

        // Removed old properties like mailHost, etc. as they are in SmtpSettings now

        /// <summary>
        /// Sends an email using the provided SMTP settings.
        /// </summary>
        /// <param name="settings">SMTP configuration object.</param>
        /// <param name="recipient">The primary recipient's email address.</param>
        /// <param name="sender">The sender's email address (optional, uses FromAddress in settings if null/empty).</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The email body.</param>
        /// <param name="isHtml">Whether the body content is HTML.</param>
        /// <param name="cc">CC recipient(s), comma-separated if multiple.</param>
        /// <param name="bcc">BCC recipient(s), comma-separated if multiple.</param>
        public void SendMail(SmtpSettings settings, string recipient, string sender, string subject, string body, bool isHtml = true, string cc = "", string bcc = "")
        {
            // Basic validation of required settings
            if (settings == null) throw new ArgumentNullException(nameof(settings), "SMTP settings must be provided.");
            if (string.IsNullOrEmpty(settings.Host)) throw new InvalidOperationException("SMTP Host setting is missing.");
            if (string.IsNullOrEmpty(recipient)) throw new ArgumentNullException(nameof(recipient), "Recipient email address is required.");
            if (string.IsNullOrEmpty(settings.FromAddress) && string.IsNullOrEmpty(sender)) throw new InvalidOperationException("Sender/FromAddress email address is required.");


            try
            {
                MailMessage objMail = new MailMessage();

                // Set addresses
                objMail.To.Add(new MailAddress(recipient));
                objMail.From = new MailAddress(string.IsNullOrEmpty(sender) ? settings.FromAddress : sender); // Use setting FromAddress as default

                // Set CC/BCC if provided
                if (!string.IsNullOrEmpty(cc)) objMail.CC.Add(cc); // Can add comma-separated string directly
                if (!string.IsNullOrEmpty(bcc)) objMail.Bcc.Add(bcc); // Can add comma-separated string directly

                // Set content
                objMail.Subject = subject;
                objMail.Body = body;
                objMail.IsBodyHtml = isHtml;
                objMail.Priority = MailPriority.Normal;

                // Configure SmtpClient using settings from the passed object
                SmtpClient smtpMailClient = new SmtpClient(settings.Host);
                smtpMailClient.Port = settings.Port;

                // Credentials (only if username/password are provided in settings)
                if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                {
                    smtpMailClient.Credentials = new NetworkCredential(settings.Username, settings.Password);
                }

                // SSL/TLS based on settings
                smtpMailClient.EnableSsl = settings.EnableSsl;

                // Send the email
                smtpMailClient.Send(objMail);

                Console.WriteLine($"Email sent successfully to {recipient}"); // Basic logging
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR sending email to {recipient}: {ex.ToString()}"); // Log detailed error
                // Re-throw the exception so the calling code knows sending failed
                throw;
            }
            finally
            {
                // Dispose MailMessage? SmtpClient is implicitly disposed if not reused.
            }
        }
    }
}
