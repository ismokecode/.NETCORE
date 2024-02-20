namespace SWSS_v1.API
{
    public class APIResponse<T>
    {
        public APIResponse(int statusCode, List<string> success, List<string> errors, T result)
        {
            this._statusCode = statusCode;
            this._result = result;
            this._success = success;
            this._errors = errors;
        }
        public int _statusCode { get; }
        public T _result { get; }
        public List<string> _success = new List<string>();
        public List<string> _errors = new List<string>();

    }
}
