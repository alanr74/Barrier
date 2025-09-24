using System;
using System.Collections.Generic;
using Ava.Models;

namespace Ava.Repositories
{
    public interface ITransactionRepository
    {
        void InitializeDatabase();
        void InsertSampleData();
        Transaction GetNextTransaction(int laneId, DateTime lastProcessed);
        List<Transaction> GetAllTransactions();
    }
}
