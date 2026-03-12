using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace lab2.Models
{
    public class SessionCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Lấy giỏ hàng từ Session (nếu chưa có thì tạo mới)
        public static SessionCart GetCart(IServiceProvider services)
        {
            var session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Session;
            var cart = session?.GetObjectFromJson<SessionCart>("Cart") ?? new SessionCart();
            return cart;
        }
        public void Clear()
        {
            Items.Clear();
        }

        // Lưu giỏ hàng vào Session
        public void Save(IServiceProvider services)
        {
            var session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Session;
            session?.SetObjectAsJson("Cart", this);
        }

        public int TotalQuantity => Items.Sum(i => i.Quantity);
        public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);
    }

    // Extension methods để lưu/đọc object từ Session dưới dạng JSON
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}