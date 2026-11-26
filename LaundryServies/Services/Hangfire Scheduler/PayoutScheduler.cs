using LaundryDomain.IRepositories;
using LaundryDomain.Models.Wallet;
using LaundryDomain.Specifications.LaundrySpecsandwalletSpecs;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Services.Hangfire_Scheduler
{
    /// <summary>
    /// بيشتغل كل يوم خميس أوتوماتيك
    /// بيعمل Payout لكل المغاسل اللي عندها رصيد
    /// </summary>
    public class PayoutScheduler
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKashierService _kashierService;
        private readonly INotificationService _notificationService;

        public PayoutScheduler(
            IUnitOfWork unitOfWork,
            IKashierService kashierService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _kashierService = kashierService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// ده الـ Job اللي Hangfire بيشغله كل يوم خميس
        /// بيعمل Payout لكل المغاسل اللي عندها رصيد
        /// </summary>
        public async Task ProcessWeeklyPayoutsAsync()
        {
            // بنجيب كل المغاسل اللي عندها رصيد
            var walletsWithBalance = await _unitOfWork.Repository<LaundryWallet, int>()
                .GetAllWithSpecAsync(new WalletsWithBalanceSpec());

            foreach (var wallet in walletsWithBalance)
            {
                try
                {
                    // بنخزن المبلغ المتاح قبل ما يتصفر
                    var payoutAmount = wallet.AvailableBalance;

                    // بنعمل Payout لكل مغسلة
                    var result = await _kashierService.ProcessPayoutAsync(wallet.Id);

                    if (result.IsSuccess)
                    {
                        // بنسجل العملية في جدول الـ Payouts
                        var payout = new LaundryPayout
                        {
                            LaundryId = wallet.LaundryId,
                            Amount = payoutAmount,
                            Status = "Completed",
                            Method = "Kashier",
                            Notes = "Weekly scheduled payout by system",
                            CreatedBy = "WeeklyScheduler",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Repository<LaundryPayout, int>().AddAsync(payout);
                        await _unitOfWork.CompleteAsync();

                        // بنبعت Notification لصاحب المغسلة
                        await _notificationService.SendNotificationAsync(
                            wallet.Laundry.AdminId,
                            "Weekly Payout Completed ✅",
                            $"Your weekly payout of {payoutAmount:F2} EGP has been transferred successfully"
                        );
                    }
                    else
                    {
                        // لو فشل بنبعت Notification بالمشكلة
                        await _notificationService.SendNotificationAsync(
                            wallet.Laundry.AdminId,
                            "Payout Failed ❌",
                            $"Your weekly payout failed. Please contact support."
                        );
                    }
                }
                catch (Exception ex)
                {
                    // بنسجل الـ Error ونكمل على المغسلة الجاية
                    Console.WriteLine($"Payout failed for wallet {wallet.Id}: {ex.Message}");
                }
            }
        }
    }
}
