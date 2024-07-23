namespace Muse.Player.Utils
{
    public class Result<T> : Result
    {
        protected internal Result(T value, bool success, string error)
            : base(success, error)
        {
            Value = value;
        }
        public T Value { get; set; }

        public static implicit operator Result<T>(string error) => Fail<T>(error);
        public static implicit operator Result<T>(T value) => Ok(value);

        public T GetValue()
        {
            if (Success && Value != null)
            {
                return Value;
            }
            else
            {
                throw new ArgumentNullException(nameof(Value));
            }
        }
    }
}
