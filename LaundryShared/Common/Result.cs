using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryShared.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T Value { get; private set; }
        public string ErrorMessage { get; private set; }

        // Constructor مخفي عشان نجبر الاستخدام عن طريق الـ Static Methods
        private Result(bool isSuccess, T value, string errorMessage)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
        }

        // لو العملية نجحت
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null);
        }

        // لو العملية فشلت
        public static Result<T> Failure(string errorMessage)
        {
            return new Result<T>(false, default, errorMessage);
        }
    }
    // هذا الكلاس بدون Generic Type بيستخدم للحالات اللي ما فيش قيمة نرجعها، بس عايزين نعرف إذا العملية نجحت ولا لأ، ولو فشلت نرجع رسالة الخطأ بس

    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string ErrorMessage { get; private set; }

        protected Result(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static Result Success()
        {
            return new Result(true, null);
        }

        public static Result Failure(string errorMessage)
        {
            return new Result(false, errorMessage);
        }
    }
}
