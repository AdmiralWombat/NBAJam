using System.Linq.Expressions;

namespace NBAJam.Models
{
    public class QueryOptions<T> where T : class
    {
        public Expression<Func<T, Object>> OrderBy { get; set; } = null!;
        public Expression<Func<T, bool>> Where { get; set; } = null!;

        private string[] _includes = Array.Empty<string>();

        public string Includes
        {
            set => _includes = value.Replace(" ", "").Split(',');
        }

        public string[] GetIncludes() => _includes;

        public bool HasWhere => Where != null;
        public bool HasOrderBy => OrderBy != null;
    }
}