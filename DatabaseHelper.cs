using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

public static class DatabaseHelper
{
    private static string _connectionString = "Data Source=barriers.db";

    public static void InitializeDatabase()
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

    public static Transaction GetNextTransaction(int laneId, DateTime lastProcessed)
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
                OcrPlate = reader.GetString(3),
                OcrAccuracy = reader.GetInt32(4),
                Direction = reader.GetInt32(5),
                LaneId = reader.GetInt32(6),
                CameraId = reader.GetInt32(7),
                Image1 = reader.IsDBNull(8) ? null : reader.GetString(8),
                Image2 = reader.IsDBNull(9) ? null : reader.GetString(9),
                Image3 = reader.IsDBNull(10) ? null : reader.GetString(10),
                Sent = reader.GetInt32(11),
                SentDateTime = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12)
            };
        }
        return null;
    }

    public static List<Transaction> GetAllTransactions()
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
                OcrPlate = reader.GetString(3),
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

    // For testing, add some sample data
    public static void InsertSampleData()
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
            ('2025-09-23 10:20:00', '2025-09-23 10:20:00', 'MNO345', 96, 0, 3, 103, 'image5_1.jpg', 'image5_2.jpg', 'image5_3.jpg', 0, NULL);
        ";
        command.ExecuteNonQuery();
    }
}
