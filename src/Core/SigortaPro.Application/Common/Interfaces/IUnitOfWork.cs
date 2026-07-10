namespace SigortaPro.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Birden fazla store'a (örn. Identity kullanıcısı + Domain Customer kaydı) yapılan yazma işlemlerinin
    // atomik yürütülmesi için tek bir veritabanı transaction'ı içinde çalıştırır. EF detayları dışarı sızmaz
    // (ADR-017). Operasyon içinde bir hata oluşursa transaction geri alınır.
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
