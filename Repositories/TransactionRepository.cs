using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Ava.Models;

namespace Ava.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly Config _config;

        public TransactionRepository(Config config)
        {
            _config = config;
        }

        private string ConnectionString => $"Data Source={_config.DatabasePath}";

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();

            if (_config.DatabaseInitMode == "recreate")
            {
                // Drop and recreate table (destroys existing data)
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
            }
            else
            {
                // Default "keep" mode: Create table only if it doesn't exist (preserves existing data)
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS transactions (
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
            }

            command.ExecuteNonQuery();
        }

        public Transaction? GetNextTransaction(int laneId, DateTime lastProcessed)
        {
            using var connection = new SqliteConnection(ConnectionString);
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
            using var connection = new SqliteConnection(ConnectionString);
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



        public async Task AddCameraDataAsync(CameraMessage message)
        {
            await AddCameraDataAsync(message, 1, !string.IsNullOrWhiteSpace(message.LogicalDirection) && message.LogicalDirection == "1" ? 1 : 0);
        }

        public async Task AddCameraDataAsync(CameraMessage message, int laneId, int direction)
        {
            // Transform camera message to transaction and insert into existing transactions table
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            // Parse DateTime separately due to syntax constraints
            DateTime dateTime;
            try
            {
                dateTime = string.IsNullOrEmpty(message.FirstSeenWallClock) ?
                           DateTime.Now :
                           DateTime.TryParseExact(message.FirstSeenWallClock, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ?
                             parsedDate : DateTime.Now;
            }
            catch
            {
                dateTime = DateTime.Now;
            }

            // Transform camera data to transaction format
            var transaction = new Transaction
            {
                Created = DateTime.Now, // Use local time to match cron processing
                DateTime = dateTime,
                OcrPlate = message.Vrm ?? string.Empty,
                // Parse confidence as integer, default to 100 if invalid
                OcrAccuracy = int.TryParse(message.Confidence, out var accuracy) ? accuracy : 100,
                // Use provided direction from caller
                Direction = direction,
                // Use provided lane ID
                LaneId = laneId,
                // Parse camera serial as integer, default to 1
                CameraId = int.TryParse(message.CameraSerial, out var cameraId) ? cameraId : 1,
                // Take first 3 images from Images dictionary if available
                Image1 = null,
                Image2 = null,
                Image3 = null,
                Sent = 0,
                SentDateTime = DateTime.MinValue
            };
            var images = message.Images?.Values.ToList();
            if (images != null)
            {
                if (images.Count > 0) transaction.Image1 = images[0];
                if (images.Count > 1) transaction.Image2 = images[1];
                if (images.Count > 2) transaction.Image3 = images[2];
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO transactions (created, datetime, ocr_plate, ocr_accuracy, direction, lane_id, camera_id, image1, image2, image3, sent, sent_datetime)
                VALUES ($created, $datetime, $ocr_plate, $ocr_accuracy, $direction, $lane_id, $camera_id, $image1, $image2, $image3, $sent, $sent_datetime);
            ";

            command.Parameters.AddWithValue("$created", transaction.Created);
            command.Parameters.AddWithValue("$datetime", transaction.DateTime);
            command.Parameters.AddWithValue("$ocr_plate", transaction.OcrPlate);
            command.Parameters.AddWithValue("$ocr_accuracy", transaction.OcrAccuracy);
            command.Parameters.AddWithValue("$direction", transaction.Direction);
            command.Parameters.AddWithValue("$lane_id", transaction.LaneId);
            command.Parameters.AddWithValue("$camera_id", transaction.CameraId);
            command.Parameters.AddWithValue("$image1", transaction.Image1 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$image2", transaction.Image2 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$image3", transaction.Image3 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$sent", transaction.Sent);
            command.Parameters.AddWithValue("$sent_datetime", transaction.SentDateTime);

            await command.ExecuteNonQueryAsync();
        }

        public async Task MarkTransactionSentDirectly(CameraMessage message, int barrierLaneId)
        {
            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            // Find the most recently inserted transaction for this camera message
            // Since we just inserted it, find by most recent created with matching VRM and lane
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE transactions
                SET sent = 1, sent_datetime = $sent_datetime
                WHERE lane_id = $lane_id AND ocr_plate = $ocr_plate AND sent = 0
                ORDER BY created DESC
                LIMIT 1;
            ";

            command.Parameters.AddWithValue("$lane_id", barrierLaneId);
            command.Parameters.AddWithValue("$ocr_plate", message.Vrm ?? string.Empty);
            command.Parameters.AddWithValue("$sent_datetime", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }
    }
}
