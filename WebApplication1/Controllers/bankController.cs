using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Transactions;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class bankController : Controller
    {
        private readonly string con = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\ASUS\\OneDrive\\Documents\\BankDB.mdf;";
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult custRegister()
        {
            List<BranchTB> branches = new List<BranchTB>();
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select * from branchTB", conn);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                branches.Add(new BranchTB
                {
                    branchId = Convert.ToInt32(reader["branchId"]),
                    branchName = reader["branchName"].ToString()
                });
            }
            conn.Close();
            ViewBag.branches = branches;
            return View();
        }

        [HttpPost]
        public IActionResult custRegister(CustomerTB customer)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand($"insert into customerTB values({customer.acno},'{customer.name}',{customer.balance},'{customer.loginPwd}',{customer.transPwd},{customer.branchId})", conn);
            int result = cmd.ExecuteNonQuery();
            conn.Close();
            if (result > 0)
            {
                TempData["success"] = "Registration Successful";
                return RedirectToAction("custLogin");
            }
            else
            {
                TempData["fail"] = "Registration Failed";
                return RedirectToAction("custRegister");
            }

        }
        public IActionResult custLogin()
        {
            return View();
        }
        [HttpPost]
        public IActionResult custLogin(loginCustomer lc)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select count(*) from customerTB where acno = @Acno and loginPwd = @LoginPwd", conn);
            cmd.Parameters.AddWithValue("@Acno", lc.acno);
            cmd.Parameters.AddWithValue("@LoginPwd", lc.loginPwd);
            int result = Convert.ToInt32(cmd.ExecuteScalar());
            if (result == 1)
            {
                HttpContext.Session.SetInt32("acno", lc.acno);
                return RedirectToAction("custDashboard");
                //TempData["successLogin"] = "Login Successful";
            }
            else
            {
                TempData["fail"] = "Invalid Login Account or Password";
            }
            return View();
        }
        public IActionResult custDashboard()
        {
            if (HttpContext.Session.GetInt32("acno") == null)
                return RedirectToAction("custLogin");
            using (SqlConnection conn = new SqlConnection(con))
            {
                conn.Open();
                DataTable dt = new DataTable();
                SqlDataAdapter odp = new SqlDataAdapter("select * from customerTB where acno = @acno", conn);
                odp.SelectCommand.Parameters.AddWithValue("@acno", HttpContext.Session.GetInt32("acno"));
                odp.Fill(dt);
                return View(dt);
            }
        }
        public IActionResult UpdateProfile()
        {
            using (SqlConnection conn = new SqlConnection(con))
            {
                conn.Open();
                string query = "select * from customerTB where acno = @acno";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@acno", HttpContext.Session.GetInt32("acno"));
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        CustomerTB cust = new CustomerTB();
                        cust.acno = Convert.ToInt32(reader[0].ToString());
                        cust.name = reader[1].ToString();
                        cust.balance = Convert.ToInt32(reader[2].ToString());
                        cust.loginPwd = reader[3].ToString();
                        cust.transPwd = Convert.ToInt32(reader[4].ToString());
                        cust.branchId = Convert.ToInt32(reader[5].ToString());
                        ViewBag.cust = cust;
                    }

                }
            }
            return View();
        }
        [HttpPost]
        public IActionResult UpdateProfile(CustomerTB customer)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand($"update customerTB set name = '{customer.name}',balance = {customer.balance},loginPwd = '{customer.loginPwd}',transPwd = {customer.transPwd},branchId = {customer.branchId} where acno = {customer.acno}", conn);
            int result = cmd.ExecuteNonQuery();
            conn.Close();
            if (result > 0)
            {
                TempData["successUpdate"] = "Updation Successful";
                return RedirectToAction("custDashboard");
            }
            else
            {
                TempData["fail"] = "Updation Failed";
                return RedirectToAction("UpdateProfile");
            }
        }
        public IActionResult Transfer()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Transfer(Transfer t)
        {
            var amt = 0;
            var transPwd = "";
            var fromAcno = HttpContext.Session.GetInt32("acno");
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand sql = new SqlCommand("select transPwd,balance from customerTB where acno=@acno", conn);
            sql.Parameters.AddWithValue("@acno", fromAcno);
            var data = sql.ExecuteReader();
            if(fromAcno == t.toAcno)
            {
                TempData["failTransfer"] = "Choose other Account not Yours";
                return View();
            }
            if (data.Read()) { 
                transPwd = data[0].ToString();
                amt = Convert.ToInt32(data[1].ToString());
            }
            data.Close();
            if(amt < t.amount)
            {
                TempData["failTransfer"] = "Insufficient Balance";
                return View();
            }
            if (Convert.ToInt32(transPwd) == t.transPwd)
            {
                SqlTransaction tran = conn.BeginTransaction();
                SqlCommand cmd = new SqlCommand("update customerTB set balance = balance - @amt where acno = @fromAcno", conn, tran);
                SqlCommand cmd1 = new SqlCommand("update customerTB set balance = balance + @amt where acno = @toAcno", conn, tran);
                SqlCommand cmd2 = new SqlCommand("insert into transactionTB(fromAcno,toAcno,amount) VALUES(@fromAcno,@toAcno,@amt)", conn, tran);

                cmd.Parameters.AddWithValue("@amt", t.amount);
                cmd.Parameters.AddWithValue("@fromAcno", fromAcno);
                cmd1.Parameters.AddWithValue("@amt", t.amount);
                cmd1.Parameters.AddWithValue("@toAcno", t.toAcno);
                cmd2.Parameters.AddWithValue("@fromAcno", fromAcno);
                cmd2.Parameters.AddWithValue("@amt", t.amount);
                cmd2.Parameters.AddWithValue("@toAcno", t.toAcno);

                bool result1 = Convert.ToBoolean(cmd.ExecuteNonQuery());
                bool result2 = Convert.ToBoolean(cmd1.ExecuteNonQuery());
                bool result3 = Convert.ToBoolean(cmd2.ExecuteNonQuery());


                if (result1 && result2 && result3)
                {
                    tran.Commit();
                    conn.Close();
                    TempData["successTransfer"] = "Transaction Successful";
                    return RedirectToAction("custDashboard");
                }
                else
                {
                    tran.Rollback();
                    conn.Close();
                    TempData["failTransfer"] = "Transaction Unsuccessful";
                    return View();
                }

            }
            else
            {
                TempData["failTransfer"] = "Invalid Transaction Password";
                return View();
            }
        }
        public IActionResult Transactions()
        {
            var acno = HttpContext.Session.GetInt32("acno");
            List<Transactions> transactions = new List<Transactions>();
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            using (SqlCommand cmd = new SqlCommand("select toAcno,amount from transactionTB where fromAcno = @acno", conn))
            {
                cmd.Parameters.AddWithValue("@acno", acno);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Transactions t = new Transactions();
                    t.acno = Convert.ToInt32(reader[0].ToString());
                    t.amount = Convert.ToInt32(reader[1].ToString());
                    t.type = "DR";
                    transactions.Add(t);
                }
                reader.Close();
            }
            using (SqlCommand cmd = new SqlCommand("select fromAcno,amount from transactionTB where toAcno = @acno", conn))
            {
                cmd.Parameters.AddWithValue("@acno", acno);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Transactions t = new Transactions();
                    t.acno = Convert.ToInt32(reader[0].ToString());
                    t.amount = Convert.ToInt32(reader[1].ToString());
                    t.type = "CR";
                    transactions.Add(t);
                }
            }
            ViewBag.transactions = transactions;
            return View();
        }
        public IActionResult custLogOut()
        {
            //HttpContext.Session.Remove("acno");
            HttpContext.Session.Clear();
            return RedirectToAction("Index","Home");
        }
        public IActionResult staffRegister()
        {
            List<BranchTB> branches = new List<BranchTB>();
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select * from branchTB", conn);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                branches.Add(new BranchTB
                {
                    branchId = Convert.ToInt32(reader["branchId"]),
                    branchName = reader["branchName"].ToString()
                });
            }
            conn.Close();
            ViewBag.branches = branches;
            return View();
        }
        [HttpPost]
        public IActionResult staffRegister(staffTB s)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand($"insert into staffTB values({s.staffId},'{s.name}','{s.loginPwd}',{s.branchId})", conn);
            int result = cmd.ExecuteNonQuery();
            conn.Close();
            if (result > 0)
            {
                TempData["success"] = "Staff Registration Successful";
                return RedirectToAction("staffLogin");
            }
            else
            {
                TempData["fail"] = "Registration Failed";
                return RedirectToAction("staffRegister");
            }
        }
        public IActionResult staffLogin()
        {
            return View();
        }
        [HttpPost]
        public IActionResult staffLogin(loginStaff login)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select count(*) from staffTB where staffId = @id and loginPwd = @LoginPwd", conn);
            cmd.Parameters.AddWithValue("@id", login.staffId);
            cmd.Parameters.AddWithValue("@LoginPwd", login.loginPwd);
            int result = Convert.ToInt32(cmd.ExecuteScalar());
            if (result == 1)
            {
                HttpContext.Session.SetInt32("staffId", login.staffId);
                return RedirectToAction("staffDashboard");
                //TempData["successLogin"] = "Login Successful";
            }
            else
            {
                TempData["fail"] = "Invalid Login Account or Password";
            }
            return View();
        }
        public IActionResult staffDashboard()
        {
            return View();
        }
        [HttpPost]
        public IActionResult staffDashboard(string accountNo)
        {
            CustomerTB customer = null;
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select * from customerTB where acno = @acno", conn);
            cmd.Parameters.AddWithValue("@acno", accountNo);
            SqlDataReader reader = cmd.ExecuteReader();
            if(reader.Read())
            {
                customer = new CustomerTB
                {
                    acno = Convert.ToInt32(reader[0].ToString()),
                    name = reader[1].ToString() ?? "",
                    balance = Convert.ToInt32(reader[2].ToString()),
                    branchId = Convert.ToInt32(reader[5].ToString()),
                };
            }
            ViewBag.customer = customer;
            return View();
        }
        public IActionResult UpdateStaffProfile()
        {
            using (SqlConnection conn = new SqlConnection(con))
            {
                conn.Open();
                string query = "select * from staffTB where staffId = @sid";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sid", HttpContext.Session.GetInt32("staffId"));
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        staffTB staff = new staffTB();
                        staff.staffId = Convert.ToInt32(reader[0].ToString());
                        staff.name = reader[1].ToString() ?? "";
                        staff.loginPwd = reader[2].ToString() ?? "";
                        staff.branchId = Convert.ToInt32(reader[3].ToString());
                        ViewBag.staff = staff;
                    }
                }
            }
            return View();
        }
        [HttpPost]
        public IActionResult UpdateStaffProfile(staffTB staff)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand($"update staffTB set name = '{staff.name}',loginPwd = '{staff.loginPwd}',branchId = {staff.branchId} where staffId = {staff.staffId}", conn);
            int result = cmd.ExecuteNonQuery();
            conn.Close();
            if (result > 0)
            {
                TempData["successUpdate"] = "Updation Successful";
                return RedirectToAction("staffDashboard");
            }
            else
            {
                TempData["fail"] = "Updation Failed";
                return RedirectToAction("UpdateProfile");
            }
        }

        public IActionResult viewByBranch()
        {
            List<BranchTB> branches = new List<BranchTB>();
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select * from branchTB", conn);
            SqlDataReader dataReader = cmd.ExecuteReader();
            while (dataReader.Read())
            {
                branches.Add(new BranchTB
                {
                    branchId = Convert.ToInt32(dataReader[0].ToString()),
                    branchName = dataReader[1].ToString() ?? ""
                });
            }
            ViewBag.branches = branches;
            return View();
        }
        [HttpPost]
        public IActionResult viewByBranch(int branchId)
        {
            SqlConnection conn = new SqlConnection(con);
            conn.Open();
            SqlCommand cmd = new SqlCommand("select count(*) from customerTB where branchId = @id", conn);
            cmd.Parameters.AddWithValue("@id", branchId);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            TempData["custCount"] = count;
            cmd = new SqlCommand("select count(*) from staffTB where branchId = @id", conn);
            cmd.Parameters.AddWithValue("@id", branchId);
            count = Convert.ToInt32(cmd.ExecuteScalar());
            TempData["staffCount"] = count;
            return RedirectToAction("viewByBranch");
        }
    }
}
