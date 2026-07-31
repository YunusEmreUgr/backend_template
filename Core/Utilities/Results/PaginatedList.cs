using System;
using System.Collections.Generic;

namespace Core.Utilities.Results
{
    /// <summary>
    /// Sayfalanmış veri listesini ve sayfalama meta bilgilerini barındıran sınıf.
    /// JSON serileştirmesinde dizi şeklinde değil, nesne şeklinde serileşerek 
    /// frontend tarafına meta bilgileri (sayfa sayısı, toplam kayıt vb.) kayıpsız iletir.
    /// </summary>
    /// <typeparam name="T">Veri tipi</typeparam>
    public class PaginatedList<T>
    {
        /// <summary>Sayfadaki veriler</summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>Mevcut sayfa numarası (1-indexed)</summary>
        public int PageIndex { get; }

        /// <summary>Sayfa boyutu (Kayıt sayısı)</summary>
        public int PageSize { get; }

        /// <summary>Veritabanındaki toplam kayıt sayısı</summary>
        public int TotalCount { get; }

        /// <summary>Toplam sayfa sayısı</summary>
        public int TotalPages { get; }

        /// <summary>Önceki sayfa var mı?</summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>Sonraki sayfa var mı?</summary>
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(IReadOnlyList<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex < 1 ? 1 : pageIndex;
            PageSize = pageSize < 1 ? 10 : pageSize;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)PageSize);
            Items = items;
        }
    }
}
