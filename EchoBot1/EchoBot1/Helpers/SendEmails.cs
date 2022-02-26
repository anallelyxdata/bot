using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EchoBot1.Helpers
{
    public class SendEmails
    {
        public static async Task SendAsync(string subject, string body)
        {
            using (var message = new MailMessage("chatbotbohn@outlook.com", "exel@x-data.mx")
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                using (var client = new SmtpClient()
                {
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = "smtp.outlook.com",
                    Credentials = new NetworkCredential("chatbotbohn@outlook.com", "Xd@ta1234"),
                })
                {
                    client.EnableSsl = true;
                    await client.SendMailAsync(message);
                }
            }
        }
    }
}
