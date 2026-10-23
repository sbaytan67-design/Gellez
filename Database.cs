using Microsoft.Data.SqlClient;

namespace GellezWeb
{
    public static class Database
    {
        public static string ConnectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;" +
            @"AttachDbFilename=|DataDirectory|\GellezDB.mdf;" +
            @"Integrated Security=True;Encrypt=False";

        public static bool EmailExists(string email)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @email", con))
            {
                cmd.Parameters.AddWithValue("@email", email);
                con.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public static void CreateUser(string fullName, string email, string password)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO Users (FullName, Email, Password) VALUES (@n, @e, @p)", con))
            {
                cmd.Parameters.AddWithValue("@n", fullName);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@p", password);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static string? CheckLogin(string email, string password)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT FullName FROM Users WHERE Email = @e AND Password = @p", con))
            {
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@p", password);
                con.Open();
                object? result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }

        // ---------- Owner / Staff ----------
        private static void EnsureStaffTable(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand(
                @"IF OBJECT_ID('dbo.Staff', 'U') IS NULL
                  CREATE TABLE dbo.Staff (
                      Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                      FullName NVARCHAR(100) NOT NULL,
                      Email NVARCHAR(150) NOT NULL UNIQUE,
                      Password NVARCHAR(100) NOT NULL,
                      Role NVARCHAR(20) NOT NULL
                  )", con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static bool StaffEmailExists(string email)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureStaffTable(con);
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Staff WHERE Email = @e", con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public static void CreateStaff(string fullName, string email, string password, string role)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureStaffTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Staff (FullName, Email, Password, Role) VALUES (@n, @e, @p, @r)", con))
                {
                    cmd.Parameters.AddWithValue("@n", fullName);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@p", password);
                    cmd.Parameters.AddWithValue("@r", role);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static (string FullName, string Role)? CheckStaffLogin(string email, string password)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureStaffTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT FullName, Role FROM Staff WHERE Email = @e AND Password = @p", con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@p", password);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return (reader.GetString(0), reader.GetString(1));
                        return null;
                    }
                }
            }
        }

        public record AppointmentItem(int Id, string CustomerName, string Service, string StylistName, DateTime When, string Status);

        private static void EnsureAppointmentsTable(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand(
                @"IF OBJECT_ID('dbo.Appointments', 'U') IS NULL
                  CREATE TABLE dbo.Appointments (
                      Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                      CustomerName NVARCHAR(100) NOT NULL,
                      CustomerEmail NVARCHAR(150) NOT NULL,
                      Service NVARCHAR(150) NOT NULL,
                      StylistEmail NVARCHAR(150) NULL,
                      StylistName NVARCHAR(100) NULL,
                      AppointmentTime DATETIME NOT NULL,
                      Status NVARCHAR(20) NOT NULL DEFAULT 'Booked',
                      Price DECIMAL(10,2) NULL
                  );
                  IF COL_LENGTH('dbo.Appointments', 'Price') IS NULL
                      ALTER TABLE dbo.Appointments ADD Price DECIMAL(10,2) NULL;", con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static List<AppointmentItem> GetTodaysAppointments(string? staffEmail, bool all)
        {
            var list = new List<AppointmentItem>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureAppointmentsTable(con);

                string sql = @"SELECT Id, CustomerName, Service, ISNULL(StylistName, ''), AppointmentTime, Status
                               FROM Appointments
                               WHERE CAST(AppointmentTime AS date) = CAST(GETDATE() AS date)
                                 AND Status <> 'Cancelled'";
                if (!all) sql += " AND (StylistEmail = @s OR StylistEmail IS NULL)";
                sql += " ORDER BY AppointmentTime";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    if (!all) cmd.Parameters.AddWithValue("@s", staffEmail ?? "");
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new AppointmentItem(
                                r.GetInt32(0), r.GetString(1), r.GetString(2),
                                r.GetString(3), r.GetDateTime(4), r.GetString(5)));
                        }
                    }
                }
            }
            return list;
        }

        public static void AddAppointment(string customerName, string customerEmail, string service,
            string? stylistEmail, string? stylistName, DateTime when)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureAppointmentsTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Appointments (CustomerName, CustomerEmail, Service, StylistEmail, StylistName, AppointmentTime)
                                                VALUES (@cn, @ce, @sv, @se, @sn, @t)", con))
                {
                    cmd.Parameters.AddWithValue("@cn", customerName);
                    cmd.Parameters.AddWithValue("@ce", customerEmail);
                    cmd.Parameters.AddWithValue("@sv", service);
                    cmd.Parameters.AddWithValue("@se", (object?)stylistEmail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@sn", (object?)stylistName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@t", when);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void ClearSampleAppointments()
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureAppointmentsTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Appointments WHERE CustomerEmail = 'sample@example.com'", con))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ---------- Staff specialties ----------
        public static readonly string[] AllSpecialties =
            { "Haircut", "Hair Color", "Rebond", "Curl", "Hair Treatment", "Manicure", "Pedicure", "Foot Spa" };

        public record StaffInfo(string FullName, string Email, string Role, List<string> Specialties);

        private static void EnsureSpecialtiesTable(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand(
                @"IF OBJECT_ID('dbo.StaffSpecialties', 'U') IS NULL
                  CREATE TABLE dbo.StaffSpecialties (
                      Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                      StaffEmail NVARCHAR(150) NOT NULL,
                      Specialty NVARCHAR(50) NOT NULL
                  )", con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static List<string> GetSpecialties(string email)
        {
            var list = new List<string>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureSpecialtiesTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Specialty FROM StaffSpecialties WHERE StaffEmail = @e ORDER BY Id", con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read()) list.Add(r.GetString(0));
                    }
                }
            }
            return list;
        }

        public static void SaveSpecialties(string email, IEnumerable<string> specialties)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureSpecialtiesTable(con);
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    using (SqlCommand del = new SqlCommand(
                        "DELETE FROM StaffSpecialties WHERE StaffEmail = @e", con, tx))
                    {
                        del.Parameters.AddWithValue("@e", email);
                        del.ExecuteNonQuery();
                    }

                    foreach (string s in specialties)
                    {
                        using (SqlCommand ins = new SqlCommand(
                            "INSERT INTO StaffSpecialties (StaffEmail, Specialty) VALUES (@e, @s)", con, tx))
                        {
                            ins.Parameters.AddWithValue("@e", email);
                            ins.Parameters.AddWithValue("@s", s);
                            ins.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }

        public static List<StaffInfo> GetStaffList(bool stylistsOnly)
        {
            var result = new List<StaffInfo>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureStaffTable(con);
                EnsureSpecialtiesTable(con);

                string sql = "SELECT FullName, Email, Role FROM Staff";
                if (stylistsOnly) sql += " WHERE Role = 'Staff'";
                sql += " ORDER BY FullName";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        result.Add(new StaffInfo(r.GetString(0), r.GetString(1), r.GetString(2), new List<string>()));
                }

                foreach (var s in result)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT Specialty FROM StaffSpecialties WHERE StaffEmail = @e ORDER BY Id", con))
                    {
                        cmd.Parameters.AddWithValue("@e", s.Email);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read()) s.Specialties.Add(r.GetString(0));
                        }
                    }
                }
            }
            return result;
        }

        public static StaffInfo? GetStaff(string email)
        {
            return GetStaffList(false).FirstOrDefault(s => s.Email == email);
        }

        //-------Hair recommendations----------
        public record HairRecItem(string FaceShape, string ChosenStyle, string Recommended, DateTime CreatedAt);

        private static void EnsureHairRecTable(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand(
                @"IF OBJECT_ID('dbo.HairRecommendations', 'U') IS NULL
                  CREATE TABLE dbo.HairRecommendations (
                      Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                      CustomerEmail NVARCHAR(150) NOT NULL,
                      FaceShape NVARCHAR(20) NOT NULL,
                      ChosenStyle NVARCHAR(100) NULL,
                      Recommended NVARCHAR(500) NOT NULL,
                      CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                  )", con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static void SaveHairRecommendation(string email, string faceShape, string? chosenStyle, string recommended)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureHairRecTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO HairRecommendations (CustomerEmail, FaceShape, ChosenStyle, Recommended)
                      VALUES (@e, @f, @c, @r)", con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@f", faceShape);
                    cmd.Parameters.AddWithValue("@c", (object?)chosenStyle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@r", recommended);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static HairRecItem? GetLatestHairRecommendation(string email)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureHairRecTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT TOP 1 FaceShape, ISNULL(ChosenStyle, ''), Recommended, CreatedAt
                      FROM HairRecommendations WHERE CustomerEmail = @e ORDER BY Id DESC", con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                            return new HairRecItem(r.GetString(0), r.GetString(1), r.GetString(2), r.GetDateTime(3));
                        return null;
                    }
                }
            }
        }

        //---------- Service & prices ---------
        public record ServiceItem(int Id, string Name, decimal Price);

        private static void EnsureServicesTable(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand(
                @"IF OBJECT_ID('dbo.Services', 'U') IS NULL
                  BEGIN
                      CREATE TABLE dbo.Services (
                          Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                          Name NVARCHAR(100) NOT NULL UNIQUE,
                          Price DECIMAL(10,2) NOT NULL DEFAULT 0
                      );
                      INSERT INTO dbo.Services (Name, Price) VALUES
                          ('Haircut', 150), ('Hair Color', 800), ('Rebond', 2500), ('Curl', 1500),
                          ('Hair Treatment', 700), ('Manicure', 200), ('Pedicure', 250), ('Foot Spa', 400);
                  END", con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static List<ServiceItem> GetServices()
        {
            var list = new List<ServiceItem>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureServicesTable(con);
                using (SqlCommand cmd = new SqlCommand("SELECT Id, Name, Price FROM Services ORDER BY Id", con))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(new ServiceItem(r.GetInt32(0), r.GetString(1), r.GetDecimal(2)));
                }
            }
            return list;
        }

        public static bool AddService(string name, decimal price)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureServicesTable(con);
                using (SqlCommand chk = new SqlCommand("SELECT COUNT(*) FROM Services WHERE Name = @n", con))
                {
                    chk.Parameters.AddWithValue("@n", name);
                    if (Convert.ToInt32(chk.ExecuteScalar()) > 0) return false;
                }
                using (SqlCommand cmd = new SqlCommand("INSERT INTO Services (Name, Price) VALUES (@n, @p)", con))
                {
                    cmd.Parameters.AddWithValue("@n", name);
                    cmd.Parameters.AddWithValue("@p", price);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        public static void UpdateServicePrice(int id, decimal price)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureServicesTable(con);
                using (SqlCommand cmd = new SqlCommand("UPDATE Services SET Price = @p WHERE Id = @i", con))
                {
                    cmd.Parameters.AddWithValue("@p", price);
                    cmd.Parameters.AddWithValue("@i", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void RemoveService(int id)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureServicesTable(con);
                EnsureSpecialtiesTable(con);
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    string? name = null;
                    using (SqlCommand get = new SqlCommand("SELECT Name FROM Services WHERE Id = @i", con, tx))
                    {
                        get.Parameters.AddWithValue("@i", id);
                        object? o = get.ExecuteScalar();
                        if (o != null) name = o.ToString();
                    }

                    if (name != null)
                    {
                        using (SqlCommand d1 = new SqlCommand("DELETE FROM StaffSpecialties WHERE Specialty = @n", con, tx))
                        {
                            d1.Parameters.AddWithValue("@n", name);
                            d1.ExecuteNonQuery();
                        }
                        using (SqlCommand d2 = new SqlCommand("DELETE FROM Services WHERE Id = @i", con, tx))
                        {
                            d2.Parameters.AddWithValue("@i", id);
                            d2.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }

        // ---------- Reports ----------
        public record ReportAppt(DateTime When, string Customer, string Service, string Stylist, string Status, decimal Price);
        public record ReportRow(string Label, int Count, decimal Sales);
        public record ReportData(int Total, int Cancelled, decimal Sales,
            List<ReportRow> ByService, List<ReportRow> ByStylist, List<ReportRow> ByDay, List<ReportAppt> Items);

        public static ReportData GetReport(DateTime from, DateTime to)
        {
            var items = new List<ReportAppt>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureServicesTable(con);
                EnsureAppointmentsTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT a.AppointmentTime, a.CustomerName, a.Service, ISNULL(a.StylistName, 'Unassigned'), a.Status, COALESCE(a.Price, s.Price, 0)
                      FROM Appointments a
                      LEFT JOIN Services s ON s.Name = a.Service
                      WHERE a.AppointmentTime >= @f AND a.AppointmentTime < @t
                      ORDER BY a.AppointmentTime", con))
                {
                    cmd.Parameters.AddWithValue("@f", from);
                    cmd.Parameters.AddWithValue("@t", to);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            items.Add(new ReportAppt(r.GetDateTime(0), r.GetString(1), r.GetString(2),
                                r.GetString(3), r.GetString(4), r.GetDecimal(5)));
                        }
                    }
                }
            }

            var active = items.Where(i => i.Status != "Cancelled").ToList();

            var byService = active.GroupBy(i => i.Service)
                .Select(g => new ReportRow(g.Key, g.Count(), g.Sum(x => x.Price)))
                .OrderByDescending(x => x.Sales).ThenBy(x => x.Label).ToList();

            var byStylist = active.GroupBy(i => i.Stylist)
                .Select(g => new ReportRow(g.Key, g.Count(), g.Sum(x => x.Price)))
                .OrderByDescending(x => x.Sales).ThenBy(x => x.Label).ToList();

            var byDay = new List<ReportRow>();
            for (var day = from.Date; day < to; day = day.AddDays(1))
            {
                var onDay = active.Where(i => i.When.Date == day).ToList();
                byDay.Add(new ReportRow(day.ToString("ddd, MMM d"), onDay.Count, onDay.Sum(x => x.Price)));
            }

            return new ReportData(active.Count, items.Count - active.Count, active.Sum(i => i.Price),
                byService, byStylist, byDay, items);
        }

        //---------- Booking -----------
        public record BookedSlot(string? StylistEmail, string CustomerEmail, DateTime When);
        public record CustAppt(int Id, string Service, string StylistName, DateTime When, string Status, decimal Price);

        public static List<StaffInfo> GetEligibleStylists(string serviceName)
        {
            return GetStaffList(true)
                .Where(s => s.Specialties.Any(x => string.Equals(x, serviceName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static List<BookedSlot> GetBookedSlots(DateTime date)
        {
            var list = new List<BookedSlot>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureAppointmentsTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT StylistEmail, CustomerEmail, AppointmentTime FROM Appointments
                      WHERE CAST(AppointmentTime AS date) = @d AND Status <> 'Cancelled'", con))
                {
                    cmd.Parameters.AddWithValue("@d", date.Date);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new BookedSlot(r.IsDBNull(0) ? null : r.GetString(0), r.GetString(1), r.GetDateTime(2)));
                        }
                    }
                }
            }
            return list;
        }

        public static bool BookAppointment(string customerName, string customerEmail, string service, decimal price,
    string? stylistEmail, string? stylistName, DateTime when, out string error)
        {
            error = "";
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureAppointmentsTable(con);
                using (SqlTransaction tx = con.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    if (stylistEmail != null)
                    {
                        using (SqlCommand chk = new SqlCommand(
                            @"SELECT COUNT(*) FROM Appointments
                              WHERE StylistEmail = @se AND Status <> 'Cancelled'
                                AND AppointmentTime > DATEADD(MINUTE, -60, @t)
                                AND AppointmentTime < DATEADD(MINUTE, 60, @t)", con, tx))
                        {
                            chk.Parameters.AddWithValue("@se", stylistEmail);
                            chk.Parameters.AddWithValue("@t", when);
                            if (Convert.ToInt32(chk.ExecuteScalar()) > 0)
                            {
                                error = "Sorry, that time was just taken. Please pick another time.";
                                return false;
                            }
                        }
                    }

                    using (SqlCommand chk2 = new SqlCommand(
                        @"SELECT COUNT(*) FROM Appointments
                          WHERE CustomerEmail = @ce AND Status <> 'Cancelled'
                            AND AppointmentTime > DATEADD(MINUTE, -60, @t)
                            AND AppointmentTime < DATEADD(MINUTE, 60, @t)", con, tx))
                    {
                        chk2.Parameters.AddWithValue("@ce", customerEmail);
                        chk2.Parameters.AddWithValue("@t", when);
                        if (Convert.ToInt32(chk2.ExecuteScalar()) > 0)
                        {
                            error = "You already have an appointment around that time.";
                            return false;
                        }
                    }

                    using (SqlCommand ins = new SqlCommand(
                        @"INSERT INTO Appointments (CustomerName, CustomerEmail, Service, StylistEmail, StylistName, AppointmentTime, Price)
                          VALUES (@cn, @ce, @sv, @se, @sn, @t, @p)", con, tx))
                    {
                        ins.Parameters.AddWithValue("@cn", customerName);
                        ins.Parameters.AddWithValue("@ce", customerEmail);
                        ins.Parameters.AddWithValue("@sv", service);
                        ins.Parameters.AddWithValue("@se", (object?)stylistEmail ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@sn", (object?)stylistName ?? DBNull.Value);
                        ins.Parameters.AddWithValue("@t", when);
                        ins.Parameters.AddWithValue("@p", price);
                        ins.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return true;
                }
            }
        }

        public static List<CustAppt> GetCustomerAppointments(string email, bool upcoming)
        {
            var list = new List<CustAppt>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureServicesTable(con);
                EnsureAppointmentsTable(con);

                string where = upcoming
                    ? "a.AppointmentTime >= GETDATE() AND a.Status = 'Booked'"
                    : "(a.AppointmentTime < GETDATE() OR a.Status <> 'Booked')";
                string order = upcoming ? "a.AppointmentTime ASC" : "a.AppointmentTime DESC";

                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT a.Id, a.Service, ISNULL(a.StylistName, ''), a.AppointmentTime, a.Status, COALESCE(a.Price, s.Price, 0)
                      FROM Appointments a
                      LEFT JOIN Services s ON s.Name = a.Service
                      WHERE a.CustomerEmail = @e AND " + where + " ORDER BY " + order, con))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new CustAppt(r.GetInt32(0), r.GetString(1), r.GetString(2),
                                r.GetDateTime(3), r.GetString(4), r.GetDecimal(5)));
                        }
                    }
                }
            }
            return list;
        }

        public static bool CancelAppointment(int id, string customerEmail)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                EnsureAppointmentsTable(con);
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE Appointments SET Status = 'Cancelled'
                      WHERE Id = @i AND CustomerEmail = @e AND Status = 'Booked'
                        AND AppointmentTime > DATEADD(HOUR, 2, GETDATE())", con))
                {
                    cmd.Parameters.AddWithValue("@i", id);
                    cmd.Parameters.AddWithValue("@e", customerEmail);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}