namespace YpsiMarketXPrint.API.Models
{
    public enum DeliveryMethod
    {
        Shipping,
        Pickup,
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        ReadyForPickup,
        PickedUp,
        Cancelled,
    }

    public enum UserType
    {
        Customer,
        Admin,
    }
}
