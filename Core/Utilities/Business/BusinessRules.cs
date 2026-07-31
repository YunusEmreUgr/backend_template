using Core.Utilities.Results;

namespace Core.Utilities.Business
{
    /// <summary>
    /// Servis metodlarında birden fazla iş kuralını zincirleme kontrol etmek için yardımcı sınıf.
    /// 
    /// Kullanım:
    ///   var result = BusinessRules.Run(
    ///       CheckIfProductNameExists(dto.Name),
    ///       CheckIfCategoryIsValid(dto.CategoryId),
    ///       CheckIfUserHasLimit(userId)
    ///   );
    ///   if (result != null) return result; // İlk başarısız kural döner
    /// 
    /// Bu pattern:
    ///   - Kodun okunabilirliğini artırır
    ///   - Guard clause zincirleme yerine temiz bir API sunar
    ///   - İlk başarısız kural sonraki kontroller yapılmadan döner (short-circuit)
    /// </summary>
    public static class BusinessRules
    {
        /// <summary>
        /// Verilen IResult'ları sırayla kontrol eder.
        /// İlk başarısız olan sonucu döner. Hepsi başarılıysa null döner.
        /// </summary>
        public static IResult? Run(params IResult?[] logics)
        {
            foreach (var logic in logics)
            {
                if (logic != null && !logic.Success)
                    return logic;
            }

            return null; // Tüm kurallar geçti
        }
    }
}
