namespace SWSS_v1.API
{
    public class APIResponse<T> where T:class
    {
        public APIResponse(int statusCode, List<string> success, List<string> errors, T result, IEnumerable<T> results=null)
        {
            this._statusCode = statusCode;
            this._result = result;
            this._success = success;
            this._errors = errors;
            this._result = result;
            this._results = results;
        }
        public int _statusCode { get; }
        public T _result { get; }
        public IEnumerable<T> _results{ get; }
        public List<string> _success = new List<string>();
        public List<string> _errors = new List<string>();

    }
    public class APIResponse_V<T> where T : class
    {
        public int _statusCode { get; set; }
        public T _result { get; set; }
        public IEnumerable<T> _results { get; set; }
        public List<string> _success { get; set; }
        public List<string> _errors { get; set; }
        public string exception { get; set; }
    }
}
