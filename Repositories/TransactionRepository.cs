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

        public Transaction? GetNextTransaction(int laneId, DateTime lastProcessed)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, created, datetime, ocr_plate, ocr_accuracy, direction, lane_id, camera_id, image1, image2, image3, sent, sent_datetime
                FROM transactions
                WHERE lane_id = $laneId AND created > $lastProcessed
                ORDER BY id ASC
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
            };
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
                -- 25 transactions in 2024 (old data)
                ('2024-09-23 10:00:00', '2024-09-23 10:00:00', 'ABC123', 95, 1, 1, 101, 'image1_1.jpg', 'image1_2.jpg', 'image1_3.jpg', 0, NULL),
                ('2024-09-23 10:03:00', '2024-09-23 10:03:00', 'DEF456', 92, 0, 2, 102, 'image2_1.jpg', 'image2_2.jpg', 'image2_3.jpg', 0, NULL),
                ('2024-09-23 10:06:00', '2024-09-23 10:06:00', 'GHI789', 98, 1, 3, 103, 'image3_1.jpg', 'image3_2.jpg', 'image3_3.jpg', 0, NULL),
                ('2024-09-23 10:09:00', '2024-09-23 10:09:00', 'JKL012', 90, 0, 1, 101, 'image4_1.jpg', 'image4_2.jpg', 'image4_3.jpg', 0, NULL),
                ('2024-09-23 10:12:00', '2024-09-23 10:12:00', 'MNO345', 96, 1, 2, 102, 'image5_1.jpg', 'image5_2.jpg', 'image5_3.jpg', 0, NULL),
                ('2024-09-23 10:15:00', '2024-09-23 10:15:00', 'PQR678', 94, 0, 3, 103, 'image6_1.jpg', 'image6_2.jpg', 'image6_3.jpg', 0, NULL),
                ('2024-09-23 10:18:00', '2024-09-23 10:18:00', 'STU901', 97, 1, 1, 101, 'image7_1.jpg', 'image7_2.jpg', 'image7_3.jpg', 0, NULL),
                ('2024-09-23 10:21:00', '2024-09-23 10:21:00', 'VWX234', 91, 0, 2, 102, 'image8_1.jpg', 'image8_2.jpg', 'image8_3.jpg', 0, NULL),
                ('2024-09-23 10:24:00', '2024-09-23 10:24:00', 'YZA567', 93, 1, 3, 103, 'image9_1.jpg', 'image9_2.jpg', 'image9_3.jpg', 0, NULL),
                ('2024-09-23 10:27:00', '2024-09-23 10:27:00', 'BCD890', 96, 0, 1, 101, 'image10_1.jpg', 'image10_2.jpg', 'image10_3.jpg', 0, NULL),
                ('2024-09-23 10:30:00', '2024-09-23 10:30:00', 'EFG123', 98, 1, 2, 102, 'image11_1.jpg', 'image11_2.jpg', 'image11_3.jpg', 0, NULL),
                ('2024-09-23 10:33:00', '2024-09-23 10:33:00', 'HIJ456', 92, 0, 3, 103, 'image12_1.jpg', 'image12_2.jpg', 'image12_3.jpg', 0, NULL),
                ('2024-09-23 10:36:00', '2024-09-23 10:36:00', 'KLM789', 95, 1, 1, 101, 'image13_1.jpg', 'image13_2.jpg', 'image13_3.jpg', 0, NULL),
                ('2024-09-23 10:39:00', '2024-09-23 10:39:00', 'NOP012', 94, 0, 2, 102, 'image14_1.jpg', 'image14_2.jpg', 'image14_3.jpg', 0, NULL),
                ('2024-09-23 10:42:00', '2024-09-23 10:42:00', 'QRS345', 97, 1, 3, 103, 'image15_1.jpg', 'image15_2.jpg', 'image15_3.jpg', 0, NULL),
                ('2024-09-23 10:45:00', '2024-09-23 10:45:00', 'TUV678', 90, 0, 1, 101, 'image16_1.jpg', 'image16_2.jpg', 'image16_3.jpg', 0, NULL),
                ('2024-09-23 10:48:00', '2024-09-23 10:48:00', 'WXY901', 96, 1, 2, 102, 'image17_1.jpg', 'image17_2.jpg', 'image17_3.jpg', 0, NULL),
                ('2024-09-23 10:51:00', '2024-09-23 10:51:00', 'ZAB234', 93, 0, 3, 103, 'image18_1.jpg', 'image18_2.jpg', 'image18_3.jpg', 0, NULL),
                ('2024-09-23 10:54:00', '2024-09-23 10:54:00', 'CDE567', 98, 1, 1, 101, 'image19_1.jpg', 'image19_2.jpg', 'image19_3.jpg', 0, NULL),
                ('2024-09-23 10:57:00', '2024-09-23 10:57:00', 'FGH890', 91, 0, 2, 102, 'image20_1.jpg', 'image20_2.jpg', 'image20_3.jpg', 0, NULL),
                ('2024-09-23 11:00:00', '2024-09-23 11:00:00', 'IJK123', 95, 1, 3, 103, 'image21_1.jpg', 'image21_2.jpg', 'image21_3.jpg', 0, NULL),
                ('2024-09-23 11:03:00', '2024-09-23 11:03:00', 'LMN456', 94, 0, 1, 101, 'image22_1.jpg', 'image22_2.jpg', 'image22_3.jpg', 0, NULL),
                ('2024-09-23 11:06:00', '2024-09-23 11:06:00', 'OPQ789', 97, 1, 2, 102, 'image23_1.jpg', 'image23_2.jpg', 'image23_3.jpg', 0, NULL),
                ('2024-09-23 11:09:00', '2024-09-23 11:09:00', 'RST012', 92, 0, 3, 103, 'image24_1.jpg', 'image24_2.jpg', 'image24_3.jpg', 0, NULL),
                ('2024-09-23 11:12:00', '2024-09-23 11:12:00', 'UVW345', 96, 1, 1, 101, 'image25_1.jpg', 'image25_2.jpg', 'image25_3.jpg', 0, NULL),
                -- 75 transactions in 2026 (post-startup data)
                ('2026-09-23 10:00:00', '2026-09-23 10:00:00', 'XYZ678', 95, 0, 2, 102, 'image26_1.jpg', 'image26_2.jpg', 'image26_3.jpg', 0, NULL),
                ('2026-09-23 10:03:00', '2026-09-23 10:03:00', 'ABC901', 92, 1, 3, 103, 'image27_1.jpg', 'image27_2.jpg', 'image27_3.jpg', 0, NULL),
                ('2026-09-23 10:06:00', '2026-09-23 10:06:00', 'DEF234', 98, 0, 1, 101, 'image28_1.jpg', 'image28_2.jpg', 'image28_3.jpg', 0, NULL),
                ('2026-09-23 10:09:00', '2026-09-23 10:09:00', 'GHI567', 90, 1, 2, 102, 'image29_1.jpg', 'image29_2.jpg', 'image29_3.jpg', 0, NULL),
                ('2026-09-23 10:12:00', '2026-09-23 10:12:00', 'JKL890', 96, 0, 3, 103, 'image30_1.jpg', 'image30_2.jpg', 'image30_3.jpg', 0, NULL),
                ('2026-09-23 10:15:00', '2026-09-23 10:15:00', 'MNO123', 94, 1, 1, 101, 'image31_1.jpg', 'image31_2.jpg', 'image31_3.jpg', 0, NULL),
                ('2026-09-23 10:18:00', '2026-09-23 10:18:00', 'PQR456', 97, 0, 2, 102, 'image32_1.jpg', 'image32_2.jpg', 'image32_3.jpg', 0, NULL),
                ('2026-09-23 10:21:00', '2026-09-23 10:21:00', 'STU789', 91, 1, 3, 103, 'image33_1.jpg', 'image33_2.jpg', 'image33_3.jpg', 0, NULL),
                ('2026-09-23 10:24:00', '2026-09-23 10:24:00', 'VWX012', 93, 0, 1, 101, 'image34_1.jpg', 'image34_2.jpg', 'image34_3.jpg', 0, NULL),
                ('2026-09-23 10:27:00', '2026-09-23 10:27:00', 'YZA345', 96, 1, 2, 102, 'image35_1.jpg', 'image35_2.jpg', 'image35_3.jpg', 0, NULL),
                ('2026-09-23 10:30:00', '2026-09-23 10:30:00', 'BCD678', 98, 0, 3, 103, 'image36_1.jpg', 'image36_2.jpg', 'image36_3.jpg', 0, NULL),
                ('2026-09-23 10:33:00', '2026-09-23 10:33:00', 'EFG901', 92, 1, 1, 101, 'image37_1.jpg', 'image37_2.jpg', 'image37_3.jpg', 0, NULL),
                ('2026-09-23 10:36:00', '2026-09-23 10:36:00', 'HIJ234', 95, 0, 2, 102, 'image38_1.jpg', 'image38_2.jpg', 'image38_3.jpg', 0, NULL),
                ('2026-09-23 10:39:00', '2026-09-23 10:39:00', 'KLM567', 94, 1, 3, 103, 'image39_1.jpg', 'image39_2.jpg', 'image39_3.jpg', 0, NULL),
                ('2026-09-23 10:42:00', '2026-09-23 10:42:00', 'NOP890', 97, 0, 1, 101, 'image40_1.jpg', 'image40_2.jpg', 'image40_3.jpg', 0, NULL),
                ('2026-09-23 10:45:00', '2026-09-23 10:45:00', 'QRS123', 90, 1, 2, 102, 'image41_1.jpg', 'image41_2.jpg', 'image41_3.jpg', 0, NULL),
                ('2026-09-23 10:48:00', '2026-09-23 10:48:00', 'TUV456', 96, 0, 3, 103, 'image42_1.jpg', 'image42_2.jpg', 'image42_3.jpg', 0, NULL),
                ('2026-09-23 10:51:00', '2026-09-23 10:51:00', 'WXY789', 93, 1, 1, 101, 'image43_1.jpg', 'image43_2.jpg', 'image43_3.jpg', 0, NULL),
                ('2026-09-23 10:54:00', '2026-09-23 10:54:00', 'ZAB012', 98, 0, 2, 102, 'image44_1.jpg', 'image44_2.jpg', 'image44_3.jpg', 0, NULL),
                ('2026-09-23 10:57:00', '2026-09-23 10:57:00', 'CDE345', 91, 1, 3, 103, 'image45_1.jpg', 'image45_2.jpg', 'image45_3.jpg', 0, NULL),
                ('2026-09-23 11:00:00', '2026-09-23 11:00:00', 'FGH678', 95, 0, 1, 101, 'image46_1.jpg', 'image46_2.jpg', 'image46_3.jpg', 0, NULL),
                ('2026-09-23 11:03:00', '2026-09-23 11:03:00', 'IJK901', 94, 1, 2, 102, 'image47_1.jpg', 'image47_2.jpg', 'image47_3.jpg', 0, NULL),
                ('2026-09-23 11:06:00', '2026-09-23 11:06:00', 'LMN234', 97, 0, 3, 103, 'image48_1.jpg', 'image48_2.jpg', 'image48_3.jpg', 0, NULL),
                ('2026-09-23 11:09:00', '2026-09-23 11:09:00', 'OPQ567', 92, 1, 1, 101, 'image49_1.jpg', 'image49_2.jpg', 'image49_3.jpg', 0, NULL),
                ('2026-09-23 11:12:00', '2026-09-23 11:12:00', 'RST890', 96, 0, 2, 102, 'image50_1.jpg', 'image50_2.jpg', 'image50_3.jpg', 0, NULL),
                ('2026-09-23 11:15:00', '2026-09-23 11:15:00', 'UVW123', 98, 1, 3, 103, 'image51_1.jpg', 'image51_2.jpg', 'image51_3.jpg', 0, NULL),
                ('2026-09-23 11:18:00', '2026-09-23 11:18:00', 'XYZ456', 90, 0, 1, 101, 'image52_1.jpg', 'image52_2.jpg', 'image52_3.jpg', 0, NULL),
                ('2026-09-23 11:21:00', '2026-09-23 11:21:00', 'ABC789', 95, 1, 2, 102, 'image53_1.jpg', 'image53_2.jpg', 'image53_3.jpg', 0, NULL),
                ('2026-09-23 11:24:00', '2026-09-23 11:24:00', 'DEF012', 94, 0, 3, 103, 'image54_1.jpg', 'image54_2.jpg', 'image54_3.jpg', 0, NULL),
                ('2026-09-23 11:27:00', '2026-09-23 11:27:00', 'GHI345', 97, 1, 1, 101, 'image55_1.jpg', 'image55_2.jpg', 'image55_3.jpg', 0, NULL),
                ('2026-09-23 11:30:00', '2026-09-23 11:30:00', 'JKL678', 91, 0, 2, 102, 'image56_1.jpg', 'image56_2.jpg', 'image56_3.jpg', 0, NULL),
                ('2026-09-23 11:33:00', '2026-09-23 11:33:00', 'MNO901', 93, 1, 3, 103, 'image57_1.jpg', 'image57_2.jpg', 'image57_3.jpg', 0, NULL),
                ('2026-09-23 11:36:00', '2026-09-23 11:36:00', 'PQR234', 96, 0, 1, 101, 'image58_1.jpg', 'image58_2.jpg', 'image58_3.jpg', 0, NULL),
                ('2026-09-23 11:39:00', '2026-09-23 11:39:00', 'STU567', 98, 1, 2, 102, 'image59_1.jpg', 'image59_2.jpg', 'image59_3.jpg', 0, NULL),
                ('2026-09-23 11:42:00', '2026-09-23 11:42:00', 'VWX890', 92, 0, 3, 103, 'image60_1.jpg', 'image60_2.jpg', 'image60_3.jpg', 0, NULL),
                ('2026-09-23 11:45:00', '2026-09-23 11:45:00', 'YZA123', 95, 1, 1, 101, 'image61_1.jpg', 'image61_2.jpg', 'image61_3.jpg', 0, NULL),
                ('2026-09-23 11:48:00', '2026-09-23 11:48:00', 'BCD456', 94, 0, 2, 102, 'image62_1.jpg', 'image62_2.jpg', 'image62_3.jpg', 0, NULL),
                ('2026-09-23 11:51:00', '2026-09-23 11:51:00', 'EFG789', 97, 1, 3, 103, 'image63_1.jpg', 'image63_2.jpg', 'image63_3.jpg', 0, NULL),
                ('2026-09-23 11:54:00', '2026-09-23 11:54:00', 'HIJ012', 90, 0, 1, 101, 'image64_1.jpg', 'image64_2.jpg', 'image64_3.jpg', 0, NULL),
                ('2026-09-23 11:57:00', '2026-09-23 11:57:00', 'KLM345', 96, 1, 2, 102, 'image65_1.jpg', 'image65_2.jpg', 'image65_3.jpg', 0, NULL),
                ('2026-09-23 12:00:00', '2026-09-23 12:00:00', 'NOP678', 93, 0, 3, 103, 'image66_1.jpg', 'image66_2.jpg', 'image66_3.jpg', 0, NULL),
                ('2026-09-23 12:03:00', '2026-09-23 12:03:00', 'QRS901', 98, 1, 1, 101, 'image67_1.jpg', 'image67_2.jpg', 'image67_3.jpg', 0, NULL),
                ('2026-09-23 12:06:00', '2026-09-23 12:06:00', 'TUV234', 91, 0, 2, 102, 'image68_1.jpg', 'image68_2.jpg', 'image68_3.jpg', 0, NULL),
                ('2026-09-23 12:09:00', '2026-09-23 12:09:00', 'WXY567', 95, 1, 3, 103, 'image69_1.jpg', 'image69_2.jpg', 'image69_3.jpg', 0, NULL),
                ('2026-09-23 12:12:00', '2026-09-23 12:12:00', 'ZAB890', 94, 0, 1, 101, 'image70_1.jpg', 'image70_2.jpg', 'image70_3.jpg', 0, NULL),
                ('2026-09-23 12:15:00', '2026-09-23 12:15:00', 'CDE123', 97, 1, 2, 102, 'image71_1.jpg', 'image71_2.jpg', 'image71_3.jpg', 0, NULL),
                ('2026-09-23 12:18:00', '2026-09-23 12:18:00', 'FGH456', 92, 0, 3, 103, 'image72_1.jpg', 'image72_2.jpg', 'image72_3.jpg', 0, NULL),
                ('2026-09-23 12:21:00', '2026-09-23 12:21:00', 'IJK789', 96, 1, 1, 101, 'image73_1.jpg', 'image73_2.jpg', 'image73_3.jpg', 0, NULL),
                ('2026-09-23 12:24:00', '2026-09-23 12:24:00', 'LMN012', 98, 0, 2, 102, 'image74_1.jpg', 'image74_2.jpg', 'image74_3.jpg', 0, NULL),
                ('2026-09-23 12:27:00', '2026-09-23 12:27:00', 'OPQ345', 90, 1, 3, 103, 'image75_1.jpg', 'image75_2.jpg', 'image75_3.jpg', 0, NULL),
                ('2026-09-23 12:30:00', '2026-09-23 12:30:00', 'RST678', 95, 0, 1, 101, 'image76_1.jpg', 'image76_2.jpg', 'image76_3.jpg', 0, NULL),
                ('2026-09-23 12:33:00', '2026-09-23 12:33:00', 'UVW901', 94, 1, 2, 102, 'image77_1.jpg', 'image77_2.jpg', 'image77_3.jpg', 0, NULL),
                ('2026-09-23 12:36:00', '2026-09-23 12:36:00', 'XYZ234', 97, 0, 3, 103, 'image78_1.jpg', 'image78_2.jpg', 'image78_3.jpg', 0, NULL),
                ('2026-09-23 12:39:00', '2026-09-23 12:39:00', 'ABC567', 91, 1, 1, 101, 'image79_1.jpg', 'image79_2.jpg', 'image79_3.jpg', 0, NULL),
                ('2026-09-23 12:42:00', '2026-09-23 12:42:00', 'DEF890', 93, 0, 2, 102, 'image80_1.jpg', 'image80_2.jpg', 'image80_3.jpg', 0, NULL),
                ('2026-09-23 12:45:00', '2026-09-23 12:45:00', 'GHI123', 96, 1, 3, 103, 'image81_1.jpg', 'image81_2.jpg', 'image81_3.jpg', 0, NULL),
                ('2026-09-23 12:48:00', '2026-09-23 12:48:00', 'JKL456', 98, 0, 1, 101, 'image82_1.jpg', 'image82_2.jpg', 'image82_3.jpg', 0, NULL),
                ('2026-09-23 12:51:00', '2026-09-23 12:51:00', 'MNO789', 92, 1, 2, 102, 'image83_1.jpg', 'image83_2.jpg', 'image83_3.jpg', 0, NULL),
                ('2026-09-23 12:54:00', '2026-09-23 12:54:00', 'PQR012', 95, 0, 3, 103, 'image84_1.jpg', 'image84_2.jpg', 'image84_3.jpg', 0, NULL),
                ('2026-09-23 12:57:00', '2026-09-23 12:57:00', 'STU345', 94, 1, 1, 101, 'image85_1.jpg', 'image85_2.jpg', 'image85_3.jpg', 0, NULL),
                ('2026-09-23 13:00:00', '2026-09-23 13:00:00', 'VWX678', 97, 0, 2, 102, 'image86_1.jpg', 'image86_2.jpg', 'image86_3.jpg', 0, NULL),
                ('2026-09-23 13:03:00', '2026-09-23 13:03:00', 'YZA901', 90, 1, 3, 103, 'image87_1.jpg', 'image87_2.jpg', 'image87_3.jpg', 0, NULL),
                ('2026-09-23 13:06:00', '2026-09-23 13:06:00', 'BCD234', 96, 0, 1, 101, 'image88_1.jpg', 'image88_2.jpg', 'image88_3.jpg', 0, NULL),
                ('2026-09-23 13:09:00', '2026-09-23 13:09:00', 'EFG567', 93, 1, 2, 102, 'image89_1.jpg', 'image89_2.jpg', 'image89_3.jpg', 0, NULL),
                ('2026-09-23 13:12:00', '2026-09-23 13:12:00', 'HIJ890', 98, 0, 3, 103, 'image90_1.jpg', 'image90_2.jpg', 'image90_3.jpg', 0, NULL),
                ('2026-09-23 13:15:00', '2026-09-23 13:15:00', 'KLM123', 91, 1, 1, 101, 'image91_1.jpg', 'image91_2.jpg', 'image91_3.jpg', 0, NULL),
                ('2026-09-23 13:18:00', '2026-09-23 13:18:00', 'NOP456', 95, 0, 2, 102, 'image92_1.jpg', 'image92_2.jpg', 'image92_3.jpg', 0, NULL),
                ('2026-09-23 13:21:00', '2026-09-23 13:21:00', 'QRS789', 94, 1, 3, 103, 'image93_1.jpg', 'image93_2.jpg', 'image93_3.jpg', 0, NULL),
                ('2026-09-23 13:24:00', '2026-09-23 13:24:00', 'TUV012', 97, 0, 1, 101, 'image94_1.jpg', 'image94_2.jpg', 'image94_3.jpg', 0, NULL),
                ('2026-09-23 13:27:00', '2026-09-23 13:27:00', 'WXY345', 92, 1, 2, 102, 'image95_1.jpg', 'image95_2.jpg', 'image95_3.jpg', 0, NULL),
                ('2026-09-23 13:30:00', '2026-09-23 13:30:00', 'ZAB678', 96, 0, 3, 103, 'image96_1.jpg', 'image96_2.jpg', 'image96_3.jpg', 0, NULL),
                ('2026-09-23 13:33:00', '2026-09-23 13:33:00', 'CDE901', 98, 1, 1, 101, 'image97_1.jpg', 'image97_2.jpg', 'image97_3.jpg', 0, NULL),
                ('2026-09-23 13:36:00', '2026-09-23 13:36:00', 'FGH234', 90, 0, 2, 102, 'image98_1.jpg', 'image98_2.jpg', 'image98_3.jpg', 0, NULL),
                ('2026-09-23 13:39:00', '2026-09-23 13:39:00', 'IJK567', 95, 1, 3, 103, 'image99_1.jpg', 'image99_2.jpg', 'image99_3.jpg', 0, NULL),
                ('2026-09-23 13:42:00', '2026-09-23 13:42:00', 'LMN890', 94, 0, 1, 101, 'image100_1.jpg', 'image100_2.jpg', 'image100_3.jpg', 0, NULL);
            ";
            command.ExecuteNonQuery();
        }
    }
}
