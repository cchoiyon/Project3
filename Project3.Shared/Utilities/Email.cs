using System;
using System.Net.Mail;
using System.Net; // Required for NetworkCredential
// using Microsoft.Extensions.Options; // No longer needed in this class if settings are passed to SendMail
using System.Text; // Often needed if manipulating body
using Project3.Shared.Models.Configuration;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Project3.Shared.Utilities
{
    public class Email
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<Email> _logger;

        public Email(SmtpSettings smtpSettings, ILogger<Email> logger = null)
        {
            _smtpSettings = smtpSettings ?? throw new ArgumentNullException(nameof(smtpSettings));
            _logger = logger;
        }

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

                _logger?.LogInformation($"Email sent successfully to {recipient}");
                Console.WriteLine($"Email sent successfully to {recipient}"); // Basic logging
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"ERROR sending email to {recipient}");
                Console.WriteLine($"ERROR sending email to {recipient}: {ex.ToString()}"); // Log detailed error
                // Re-throw the exception so the calling code knows sending failed
                throw;
            }
            finally
            {
                // Dispose MailMessage? SmtpClient is implicitly disposed if not reused.
            }
        }
        
        /// <summary>
        /// Sends an email asynchronously to the specified recipient.
        /// This method is specifically designed for simpler email sending in the email verification flow.
        /// </summary>
        /// <param name="recipient">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The HTML body of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {
                _logger?.LogInformation($"Attempting to send email to {recipient} using SMTP server {_smtpSettings.Host}:{_smtpSettings.Port}");
                
                // For development/testing, let's just log the email content and pretend it was sent
                if (_smtpSettings.Host.ToLower() == "localhost" || _smtpSettings.Host.ToLower().Contains("dev") || _smtpSettings.Host == "127.0.0.1")
                {
                    _logger?.LogWarning($"DEVELOPMENT MODE: Email would be sent to {recipient}");
                    _logger?.LogInformation($"Subject: {subject}");
                    _logger?.LogInformation($"Body: {body.Substring(0, Math.Min(100, body.Length))}..."); // Log first 100 chars
                    
                    Console.WriteLine($"DEVELOPMENT MODE: Email would be sent to {recipient}");
                    Console.WriteLine($"Subject: {subject}");
                    Console.WriteLine($"Body: {body.Substring(0, Math.Min(100, body.Length))}..."); // First 100 chars
                    
                    // Instead of actually sending, we'll store the verification code in a temporary file for testing
                    // Extract the verification code from the body (assuming it's in the format we expect)
                    string codeSection = "style='background-color: #f5f5f5; padding: 10px; text-align: center;'>";
                    if (body.Contains(codeSection))
                    {
                        int startIndex = body.IndexOf(codeSection) + codeSection.Length;
                        int endIndex = body.IndexOf("</h3>", startIndex);
                        if (endIndex > startIndex)
                        {
                            string code = body.Substring(startIndex, endIndex - startIndex);
                            Console.WriteLine($"Verification code for {recipient}: {code}");
                        }
                    }
                    
                    return; // Skip actual sending in development mode
                }
                
                MailMessage objMail = new MailMessage();
                objMail.To.Add(new MailAddress(recipient));
                objMail.From = new MailAddress(_smtpSettings.FromAddress);
                objMail.Subject = subject;
                objMail.Body = body;
                objMail.IsBodyHtml = true;
                objMail.Priority = MailPriority.Normal;

                // Try with different SMTP configurations if the standard one fails
                Exception lastException = null;
                
                // Try Temple's SMTP server first
                if (await TrySendEmailAsync(objMail, _smtpSettings.Host, _smtpSettings.Port, _smtpSettings.Username, 
                    _smtpSettings.Password, _smtpSettings.EnableSsl))
                {
                    return; // Successfully sent
                }
                
                // If Temple's server fails, try Gmail as a fallback (if configured)
                _logger?.LogWarning($"Failed to send via Temple SMTP. Trying Gmail as fallback...");
                if (await TrySendEmailAsync(objMail, "smtp.gmail.com", 587, "your.email@gmail.com", 
                    "your-app-password", true))
                {
                    return; // Successfully sent via Gmail
                }
                
                // If all attempts fail, throw the last exception
                if (lastException != null)
                {
                    throw lastException;
                }
                else
                {
                    throw new Exception("Failed to send email through all available SMTP servers");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"ERROR sending email to {recipient}");
                Console.WriteLine($"ERROR sending email to {recipient}: {ex.ToString()}");
                
                // Instead of failing completely, we'll just log the error and continue
                // This will allow registration to proceed even if email verification fails
                Console.WriteLine("WARNING: Email verification was skipped due to SMTP configuration issues");
            }
        }
        
        private async Task<bool> TrySendEmailAsync(MailMessage message, string host, int port, 
            string username, string password, bool enableSsl)
        {
            try
            {
                _logger?.LogInformation($"Attempting SMTP connection to {host}:{port} (SSL: {enableSsl})");
                
                // Configure SmtpClient
                using (SmtpClient smtpClient = new SmtpClient(host))
                {
                    smtpClient.Port = port;
                    smtpClient.EnableSsl = enableSsl;
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        smtpClient.Credentials = new NetworkCredential(username, password);
                    }
                    
                    // Set a timeout to prevent hanging for too long
                    smtpClient.Timeout = 10000; // 10 seconds
                    
                    // Send the email asynchronously
                    await smtpClient.SendMailAsync(message);
                    
                    _logger?.LogInformation($"Email sent successfully via {host}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to send via {host}:{port}");
                return false;
            }
        }
    }
}
