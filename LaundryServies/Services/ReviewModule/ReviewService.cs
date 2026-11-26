using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.SystemModules;
using LaundryDomain.Specifications.OrderSpecs;
using LaundryDomain.Specifications.ReviewSpecs;
using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.Reviews;
using LaundryShared.Common;
using LaundryShared.DTO.Reviews;
using LaundryShared.Enums.CustomerEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaundryServies.Services.ReviewModule
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ISentimentService _sentimentService;

        public ReviewService(IUnitOfWork unitOfWork, ISentimentService sentimentService)
        {
            _unitOfWork = unitOfWork;
            _sentimentService = sentimentService;
        }


        // POST /api/reviews
        public async Task<Result<ReviewDto>> AddReviewAsync(string customerId, AddReviewRequest request)
        {
            // 1. Check Order exists and belongs to customer
            var orderSpec = new OrderWithDetailsSpec(request.OrderId, customerId);
            var order = await _unitOfWork.Repository<Order, int>()
                                         .GetEntityWithSpecAsync(orderSpec);

            if (order == null)
                return Result<ReviewDto>.Failure("Order not found.");

            // 2. Check Order is Delivered
            if (order.Status != OrderStatus.Delivered)
                return Result<ReviewDto>.Failure("You can only rate completed orders.");

            // 3. Check not already rated
            var existingReviewSpec = new ReviewByOrderSpec(request.OrderId, customerId);
            var existingReview = await _unitOfWork.Repository<Review, int>()
                                                  .GetEntityWithSpecAsync(existingReviewSpec);

            if (existingReview != null)
                return Result<ReviewDto>.Failure("You have already rated this order.");

            // 4. Save Review
            var review = new Review
            {
                CustomerId = customerId,
                LaundryId = request.LaundryId,
                OrderId = request.OrderId,
                Rating = request.Rating,
                Comment = request.Comment,
                Tags = JsonSerializer.Serialize(request.Tags)
            };
            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                var sentiment = await _sentimentService.AnalyzeSentimentAsync(request.Comment, request.Rating);
                if (sentiment != null)
                {
                    review.Sentiment = sentiment.Sentiment;
                    review.SentimentScore = sentiment.ConfidenceScore;
                    review.SentimentSummary = sentiment.Summary;
                }
            }

            await _unitOfWork.Repository<Review, int>().AddAsync(review);

            // 5. Update Laundry AverageRating
            await UpdateLaundryRatingAsync(request.LaundryId);

            await _unitOfWork.CompleteAsync();

            return Result<ReviewDto>.Success(new ReviewDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                LaundryId = review.LaundryId,
                Rating = review.Rating,
                Comment = review.Comment,
                Sentiment = review.Sentiment,
                SentimentScore = review.SentimentScore,
                SentimentSummary = review.SentimentSummary,
                Tags = request.Tags,
                CreatedAt = review.CreatedAt
            });
        }


        // GET /api/reviews/order/:orderId
        public async Task<Result<OrderReviewCheckDto>> CheckOrderReviewAsync(string customerId, int orderId)
        {
            var spec = new ReviewByOrderSpec(orderId, customerId);
            var review = await _unitOfWork.Repository<Review, int>()
                                          .GetEntityWithSpecAsync(spec);

            if (review == null)
                return Result<OrderReviewCheckDto>.Success(new OrderReviewCheckDto
                {
                    HasRating = false
                });

            return Result<OrderReviewCheckDto>.Success(new OrderReviewCheckDto
            {
                HasRating = true,
                Rating = review.Rating,
                Comment = review.Comment
            });
        }

        // PUT /api/reviews/:reviewId
        public async Task<Result<ReviewDto>> UpdateReviewAsync(string customerId, int reviewId, UpdateReviewRequest request)
        {
            var spec = new ReviewWithDetailsSpec(reviewId, customerId);
            var review = await _unitOfWork.Repository<Review, int>()
                                          .GetEntityWithSpecAsync(spec);

            if (review == null)
                return Result<ReviewDto>.Failure("Review not found.");

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            review.Tags = JsonSerializer.Serialize(request.Tags);
            review.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Review, int>().Update(review);

            // Update Laundry AverageRating
            await UpdateLaundryRatingAsync(review.LaundryId);

            await _unitOfWork.CompleteAsync();

            return Result<ReviewDto>.Success(new ReviewDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                LaundryId = review.LaundryId,
                LaundryName = review.Laundry?.Name,
                CustomerName = review.Customer?.Name,
                Rating = review.Rating,
                Comment = review.Comment,
                Tags = request.Tags,
                CreatedAt = review.CreatedAt
            });
        }

        // DELETE /api/reviews/:reviewId
        public async Task<Result<bool>> DeleteReviewAsync(string customerId, int reviewId)
        {
            var spec = new ReviewWithDetailsSpec(reviewId, customerId);
            var review = await _unitOfWork.Repository<Review, int>()
                                          .GetEntityWithSpecAsync(spec);

            if (review == null)
                return Result<bool>.Failure("Review not found.");

            var laundryId = review.LaundryId;

            _unitOfWork.Repository<Review, int>().Delete(review);

            // Update Laundry AverageRating
            await UpdateLaundryRatingAsync(laundryId);

            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        // GET /api/reviews/my-reviews
        public async Task<Result<List<ReviewDto>>> GetMyReviewsAsync(string customerId)
        {
            var spec = new ReviewsByCustomerSpec(customerId);
            var reviews = await _unitOfWork.Repository<Review, int>()
                                           .GetAllWithSpecAsync(spec);

            var data = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                LaundryId = r.LaundryId,
                LaundryName = r.Laundry?.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                Tags = string.IsNullOrEmpty(r.Tags)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(r.Tags),
                CreatedAt = r.CreatedAt
            }).ToList();

            return Result<List<ReviewDto>>.Success(data);
        }

        // GET /api/laundries/:laundryId/reviews
        public async Task<Result<Pagination<ReviewDto>>> GetLaundryReviewsAsync(int laundryId, int pageIndex, int pageSize, int? rating = null)
        {
            var spec = new ReviewsByLaundrySpec(laundryId, pageIndex, pageSize, rating);
            var countSpec = new ReviewsByLaundryCountSpec(laundryId, rating);

            var reviews = await _unitOfWork.Repository<Review, int>()
                                           .GetAllWithSpecAsync(spec);

            var count = await _unitOfWork.Repository<Review, int>()
                                         .CountAsync(countSpec);

            var data = reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                LaundryId = r.LaundryId,
                CustomerName = r.Customer?.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                Tags = string.IsNullOrEmpty(r.Tags)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(r.Tags),
                CreatedAt = r.CreatedAt
            }).ToList();

            var pagination = new Pagination<ReviewDto>(pageIndex, pageSize, count, data);

            return Result<Pagination<ReviewDto>>.Success(pagination);
        }

        // GET /api/laundries/:laundryId/rating-summary
        public async Task<Result<RatingSummaryDto>> GetRatingSummaryAsync(int laundryId)
        {
            var spec = new ReviewsByLaundryCountSpec(laundryId);
            var reviews = await _unitOfWork.Repository<Review, int>()
                                           .GetAllWithSpecAsync(spec);

            var reviewsList = reviews.ToList();

            var distribution = new Dictionary<int, int>
            {
                { 5, reviewsList.Count(r => r.Rating == 5) },
                { 4, reviewsList.Count(r => r.Rating == 4) },
                { 3, reviewsList.Count(r => r.Rating == 3) },
                { 2, reviewsList.Count(r => r.Rating == 2) },
                { 1, reviewsList.Count(r => r.Rating == 1) }
            };

            var summary = new RatingSummaryDto
            {
                AverageRating = reviewsList.Any() ? reviewsList.Average(r => r.Rating) : 0,
                TotalReviews = reviewsList.Count,
                RatingDistribution = distribution
            };

            return Result<RatingSummaryDto>.Success(summary);
        }

        // Private Helper - Update Laundry Average Rating
        private async Task UpdateLaundryRatingAsync(int laundryId)
        {
            var laundry = await _unitOfWork.Repository<Laundry, int>()
                                           .GetByIdAsync(laundryId);

            if (laundry == null) return;

            var reviewsSpec = new ReviewsByLaundryCountSpec(laundryId);
            var reviews = await _unitOfWork.Repository<Review, int>()
                                           .GetAllWithSpecAsync(reviewsSpec);

            var reviewsList = reviews.ToList();

            laundry.AverageRating = reviewsList.Any()
                ? (float)reviewsList.Average(r => r.Rating)
                : 0;

            laundry.TotalReviews = reviewsList.Count;
            laundry.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Laundry, int>().Update(laundry);
        }
    }
}
