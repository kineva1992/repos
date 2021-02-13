using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using sportsstores.Infrastructure;

namespace sportsstores.Models
{
    public class SessionCart : Cart
    {
        public static Cart GetCart(IServiceProvider serviceProvider) {
            ISession session = serviceProvider.GetRequiredService<IHttpContextAccessor>()?
                .HttpContext.Session;
            SessionCart cart = session?.GetJson<SessionCart>("Cart")
                ?? new SessionCart();
            return cart;
        }
        [JsonIgnore]
        public ISession Session { get; set; }
        public override void AddItem(Product product, int quality) {
            base.AddItem(product, quality);
            Session.SetJson("Cart", this);
        }
        public override void RemoveLine(Product product) {
            base.RemoveLine(product);
            Session.SetJson("Cart", this);
        }
        public override void Clear() { 
            base.Clear();
            Session.Remove("Cart");
        }

    }
}
