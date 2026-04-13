using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MimeKit;

namespace lab2.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Kiểm tra nếu email rỗng thì thoát luôn để tránh lỗi Server
            if (string.IsNullOrEmpty(email)) return;

            var message = new MimeMessage();

            // 1. NGƯỜI GỬI: Phải khớp với email trong Authenticate bên dưới
            message.From.Add(new MailboxAddress("Life and Trees Shop", "thanhnguyen10988@gmail.com"));

            // 2. NGƯỜI NHẬN: Lấy chính xác từ tham số 'email' truyền vào
            message.To.Add(MailboxAddress.Parse(email));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                    // Dùng App Password của bạn
                    await client.AuthenticateAsync("thanhnguyen10988@gmail.com", "pmgkdhgiqdknbuya");

                    await client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nếu cần thiết
                    Console.WriteLine("Lỗi gửi mail: " + ex.Message);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
        }
    }
}