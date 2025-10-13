using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ava.Models;

namespace Ava.Repositories
{
    public interface ITransactionRepository
    {
        void InitializeDatabase();
        Transaction? GetNextTransaction(int laneId, DateTime lastProcessed);
        List<Transaction> GetAllTransactions();
        Task AddCameraDataAsync(CameraMessage message);
        Task MarkTransactionSentDirectly(CameraMessage message, int barrierLaneId);
    }
}
