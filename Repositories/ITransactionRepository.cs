using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ava.Models;

namespace Ava.Repositories
{
    public interface ITransactionRepository
    {
        void InitializeDatabase();
        List<Transaction> GetAllTransactions();
        Transaction? GetNextTransaction(int laneId, DateTime lastProcessed);
        Task AddCameraDataAsync(CameraMessage message);
    }
}
