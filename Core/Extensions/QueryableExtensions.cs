using Core.Utilities.Results;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Extensions
{
    /// <summary>
    /// IQueryable koleksiyonları üzerinde sayfalama işlemlerini kolaylaştıran genişletme metotları sınıfı.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Bir IQueryable sorgusunu veritabanı seviyesinde sayfalayarak PaginatedList tipinde döner.
        /// </summary>
        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source, 
            int pageIndex, 
            int pageSize)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
