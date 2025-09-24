using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Ava.Models;

namespace Ava.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly string _connectionString = "Data Source=barriers.db";

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DROP TABLE IF EXISTS transactions;
                CREATE TABLE transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    created DATETIME NOT NULL,
                    datetime DATETIME NOT NULL,
                    ocr_plate TEXT NOT NULL,
                    ocr_accuracy INTEGER NOT NULL,
                    direction INTEGER NOT NULL,
                    lane_id INTEGER NOT NULL,
                    camera_id INTEGER NOT NULL,
                    image1 TEXT,
                    image2 TEXT,
                    image3 TEXT,
                    sent INTEGER NOT NULL DEFAULT 0,
                    sent_datetime DATETIME
                );
            ";
            command.ExecuteNonQuery();
        }

        public Transaction GetNextTransaction(int laneId, DateTime lastProcessed)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, created, datetime, ocr_plate, ocr_accuracy, direction, lane_id, camera_id, image1, image2, image3, sent, sent_datetime
                FROM transactions
                WHERE lane_id = $laneId AND created > $lastProcessed
                ORDER BY created ASC
                LIMIT 1;
            ";
            command.Parameters.AddWithValue("$laneId", laneId);
            command.Parameters.AddWithValue("$lastProcessed", lastProcessed);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
            return new Transaction
            {
                Id = reader.GetInt32(0),
                Created = reader.GetDateTime(1),
                DateTime = reader.GetDateTime(2),
                OcrPlate = reader.GetString(3) ?? string.Empty,
                OcrAccuracy = reader.GetInt32(4),
                Direction = reader.GetInt32(5),
                LaneId = reader.GetInt32(6),
                CameraId = reader.GetInt32(7),
                Image1 = reader.IsDBNull(8) ? null : reader.GetString(8),
                Image2 = reader.IsDBNull(9) ? null : reader.GetString(9),
                Image3 = reader.IsDBNull(10) ? null : reader.GetString(10),
                Sent = reader.GetInt32(11),
                SentDateTime = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12)
            } ?? null;
            }
            return null;
        }

        public List<Transaction> GetAllTransactions()
        {
            var transactions = new List<Transaction>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, created, datetime, ocr_plate, ocr_accuracy, direction, lane_id, camera_id, image1, image2, image3, sent, sent_datetime FROM transactions ORDER BY created DESC;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),
                    Created = reader.GetDateTime(1),
                    DateTime = reader.GetDateTime(2),
                    OcrPlate = reader.GetString(3) ?? string.Empty,
                    OcrAccuracy = reader.GetInt32(4),
                    Direction = reader.GetInt32(5),
                    LaneId = reader.GetInt32(6),
                    CameraId = reader.GetInt32(7),
                    Image1 = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Image2 = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Image3 = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Sent = reader.GetInt32(11),
                    SentDateTime = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12)
                });
            }
            return transactions;
        }

        public void InsertSampleData()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO transactions (created, datetime, ocr_plate, ocr_accuracy, direction, lane_id, camera_id, image1, image2, image3, sent, sent_datetime) VALUES
                ('2025-09-23 10:00:00', '2025-09-23 10:00:00', 'ABC123', 95, 1, 1, 101, 'image1_1.jpg', 'image1_2.jpg', 'image1_3.jpg', 0, NULL),
                ('2025-09-23 10:05:00', '2025-09-23 10:05:00', 'DEF456', 92, 1, 1, 101, 'image2_1.jpg', 'image2_2.jpg', 'image2_3.jpg', 0, NULL),
                ('2025-09-23 10:10:00', '2025-09-23 10:10:00', 'GHI789', 98, 0, 2, 102, 'image3_1.jpg', 'image3_2.jpg', 'image3_3.jpg', 0, NULL),
                ('2025-09-23 10:15:00', '2025-09-23 10:15:00', 'JKL012', 90, 1, 1, 101, 'image4_1.jpg', 'image4_2.jpg', 'image4_3.jpg', 0, NULL),
                ('2025-09-23 10:20:00', '2025-09-23 10:20:00', 'MNO345', 96, 0, 3, 103, 'image5_1.jpg', 'image5_2.jpg', 'image5_3.jpg', 0, NULL),
                ('2025-09-23 10:25:00', '2025-09-23 10:25:00', 'PQR678', 94, 1, 2, 102, 'image6_1.jpg', 'image6_2.jpg', 'image6_3.jpg', 0, NULL),
                ('2025-09-23 10:30:00', '2025-09-23 10:30:00', 'STU901', 97, 0, 1, 101, 'image7_1.jpg', 'image7_2.jpg', 'image7_3.jpg', 0, NULL),
                ('2025-09-23 10:35:00', '2025-09-23 10:35:00', 'VWX234', 91, 1, 3, 103, 'image8_1.jpg', 'image8_2.jpg', 'image8_3.jpg', 0, NULL),
                ('2025-09-23 10:40:00', '2025-09-23 10:40:00', 'YZA567', 93, 0, 2, 102, 'image9_1.jpg', 'image9_2.jpg', 'image9_3.jpg', 0, NULL),
                ('2025-09-23 10:45:00', '2025-09-23 10:45:00', 'BCD890', 96, 1, 1, 101, 'image10_1.jpg', 'image10_2.jpg', 'image10_3.jpg', 0, NULL),
                ('2025-09-23 10:50:00', '2025-09-23 10:50:00', 'EFG123', 98, 0, 3, 103, 'image11_1.jpg', 'image11_2.jpg', 'image11_3.jpg', 0, NULL),
                ('2025-09-23 10:55:00', '2025-09-23 10:55:00', 'HIJ456', 92, 1, 2, 102, 'image12_1.jpg', 'image12_2.jpg', 'image12_3.jpg', 0, NULL),
                ('2025-09-23 11:00:00', '2025-09-23 11:00:00', 'KLM789', 95, 0, 1, 101, 'image13_1.jpg', 'image13_2.jpg', 'image13_3.jpg', 0, NULL),
                ('2025-09-23 11:05:00', '2025-09-23 11:05:00', 'NOP012', 94, 1, 3, 103, 'image14_1.jpg', 'image14_2.jpg', 'image14_3.jpg', 0, NULL),
                ('2025-09-23 11:10:00', '2025-09-23 11:10:00', 'QRS345', 97, 0, 2, 102, 'image15_1.jpg', 'image15_2.jpg', 'image15_3.jpg', 0, NULL),
                ('2025-09-23 11:15:00', '2025-09-23 11:15:00', 'TUV678', 90, 1, 1, 101, 'image16_1.jpg', 'image16_2.jpg', 'image16_3.jpg', 0, NULL),
                ('2025-09-23 11:20:00', '2025-09-23 11:20:00', 'WXY901', 96, 0, 3, 103, 'image17_1.jpg', 'image17_2.jpg', 'image17_3.jpg', 0, NULL),
                ('2025-09-23 11:25:00', '2025-09-23 11:25:00', 'ZAB234', 93, 1, 2, 102, 'image18_1.jpg', 'image18_2.jpg', 'image18_3.jpg', 0, NULL),
                ('2025-09-23 11:30:00', '2025-09-23 11:30:00', 'CDE567', 98, 0, 1, 101, 'image19_1.jpg', 'image19_2.jpg', 'image19_3.jpg', 0, NULL),
                ('2025-09-23 11:35:00', '2025-09-23 11:35:00', 'FGH890', 91, 1, 3, 103, 'image20_1.jpg', 'image20_2.jpg', 'image20_3.jpg', 0, NULL),
                ('2025-09-23 11:40:00', '2025-09-23 11:40:00', 'IJK123', 95, 0, 2, 102, 'image21_1.jpg', 'image21_2.jpg', 'image21_3.jpg', 0, NULL),
                ('2025-09-23 11:45:00', '2025-09-23 11:45:00', 'LMN456', 94, 1, 1, 101, 'image22_1.jpg', 'image22_2.jpg', 'image22_3.jpg', 0, NULL),
                ('2025-09-23 11:50:00', '2025-09-23 11:50:00', 'OPQ789', 97, 0, 3, 103, 'image23_1.jpg', 'image23_2.jpg', 'image23_3.jpg', 0, NULL),
                ('2025-09-23 11:55:00', '2025-09-23 11:55:00', 'RST012', 92, 1, 2, 102, 'image24_1.jpg', 'image24_2.jpg', 'image24_3.jpg', 0, NULL),
                ('2025-09-23 12:00:00', '2025-09-23 12:00:00', 'UVW345', 96, 0, 1, 101, 'image25_1.jpg', 'image25_2.jpg', 'image25_3.jpg', 0, NULL);
            ";
            command.ExecuteNonQuery();
        }
    }
}
