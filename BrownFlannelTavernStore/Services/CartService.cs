using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Services;

public class CartService
{
    private const string CartSessionKey = "ShoppingCart";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    public List<CartItem> GetCart()
    {
        return Session.GetObject<List<CartItem>>(CartSessionKey) ?? [];
    }

    public void AddToCart(CartItem item)
    {
        var cart = GetCart();
        var existing = cart.FirstOrDefault(c =>
            c.ProductId == item.ProductId && c.VariantId == item.VariantId);

        if (existing != null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            cart.Add(item);
        }

        Session.SetObject(CartSessionKey, cart);
    }

    public void RemoveFromCart(int productId, int? variantId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.ProductId == productId && c.VariantId == variantId);
        Session.SetObject(CartSessionKey, cart);
    }

    public void UpdateQuantity(int productId, int? variantId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c =>
            c.ProductId == productId && c.VariantId == variantId);

        if (item != null)
        {
            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = quantity;
        }

        Session.SetObject(CartSessionKey, cart);
    }

    public void ClearCart()
    {
        Session.Remove(CartSessionKey);
    }

    public decimal GetTotal() => GetCart().Sum(c => c.Total);

    public int GetItemCount() => GetCart().Sum(c => c.Quantity);
}
