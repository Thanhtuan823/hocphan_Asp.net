using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MimeKit;

namespace lab2.Services
{
    public class EmailSender : IEmailSender
    {
        // Hàm này sẽ được gọi mỗi khi bạn muốn gửi Email từ Controller
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            // Bạn thay "Tên Shop" và "Email của bạn" vào đây
            message.From.Add(new MailboxAddress("Life and Trees Shop", "LifeandTrees@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                // Kết nối Server Gmail (Cổng 587 là chuẩn bảo mật TLS)
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                // QUAN TRỌNG: Dùng App Password (16 ký tự) của Google, không phải mật khẩu thường
                await client.AuthenticateAsync("thanhnguyen10988@gmail.com", "pmgkdhgiqdknbuya");

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}