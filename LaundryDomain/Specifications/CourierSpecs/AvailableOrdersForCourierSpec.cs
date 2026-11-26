using LaundryDomain.Models.OrderModules;
using LaundryShared.Enums.CustomerEnums;

namespace LaundryDomain.Specifications.CourierSpecs
{
    /// <summary>
    /// الأوردرات المتاحة لكابتن معين في مغسلته:
    ///   - لا يوجد كابتن مُسنَد (CourierId == null)
    ///   - الحالة Accepted (اللاندري قبلت، لسه مفيش كابتن) أو ReadyForPickup (legacy)
    ///   - تنتمي للمغسلة المسجل فيها الكابتن
    /// نفس شروط PendingOrderForAcceptSpec لضمان التوافق.
    /// </summary>
    public class AvailableOrdersForCourierSpec : BaseSpecification<Order, int>
    {
        public AvailableOrdersForCourierSpec(int laundryId, int skip, int take)
            : base(o => o.LaundryId == laundryId &&
                        o.CourierId == null &&
                        (o.Status == OrderStatus.Accepted || o.Status == OrderStatus.ReadyForPickup))
        {
            AddInclude(o => o.Customer);
            AddInclude(o => o.Laundry);
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.Payment);
            AddInclude("OrderItems.Service");
            AddOrderByDescending(o => o.PickupTime);
            ApplyPaging(skip, take);
        }
    }

    public class AvailableOrdersForCourierCountSpec : BaseSpecification<Order, int>
    {
        public AvailableOrdersForCourierCountSpec(int laundryId)
            : base(o => o.LaundryId == laundryId &&
                        o.CourierId == null &&
                        (o.Status == OrderStatus.Accepted || o.Status == OrderStatus.ReadyForPickup))
        {
        }
    }
}
