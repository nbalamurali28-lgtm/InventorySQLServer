using System.Data.SqlClient;
using System;
using System.Data;
using System.Text;

namespace InventoryManagement.IL
{
    public class ClsExpensesMaster
    {
        readonly ClsUtility objUtilitiy = new ClsUtility();
        StringBuilder sqlQueryBuilder;
        SqlCommand objSqlCommand;

        public string expense_id { get; set; }
        public string expense_name { get; set; }
        public string expense_code { get; set; }
        public string exp_description { get; set; }
        public string exp_status { get; set; }
        public int is_active { get; set; }
        public string created_date { get; set; }
        public string modified_date { get; set; }
        public string created_by { get; set; }
        public string modified_by { get; set; }

        public DataTable GetExpensesMaster()
        {
            DataTable dt = new DataTable();
            try
            {
                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("SELECT c.expense_id, c.expense_code, c.expense_name, c.is_active ");
                sqlQueryBuilder.Append("FROM expense_master c order by c.expense_name");
                objSqlCommand = new SqlCommand(sqlQueryBuilder.ToString());
                dt = objUtilitiy.GetDataTable(objSqlCommand);
            }
            catch (Exception)
            {
                throw;
            }

            return dt;
        }

        public DataTable GetExpensesList()
        {
            DataTable dt = new DataTable();
            try
            {
                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("SELECT expense_code, expense_name FROM expense_master ORDER BY expense_name");

                objSqlCommand = new SqlCommand(sqlQueryBuilder.ToString());
                dt = objUtilitiy.GetDataTable(objSqlCommand);
            }
            catch (Exception)
            {
                throw;
            }

            return dt;
        }

        public string GetNextExpenseId()
        {
            string nextId = "EXP000001";
            try
            {
                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("SELECT expense_id FROM expense_master");

                objSqlCommand = new SqlCommand(sqlQueryBuilder.ToString());
                DataTable dt = objUtilitiy.GetDataTable(objSqlCommand);

                if (dt != null && dt.Rows.Count > 0)
                {
                    int maxIdNumber = 0;

                    foreach (DataRow row in dt.Rows)
                    {
                        string expenseId = row["expense_id"]?.ToString();
                        if (string.IsNullOrWhiteSpace(expenseId))
                        {
                            continue;
                        }

                        int index = expenseId.Length;
                        while (index > 0 && char.IsDigit(expenseId[index - 1]))
                        {
                            index--;
                        }

                        string numericPart = expenseId.Substring(index);

                        if (!string.IsNullOrEmpty(numericPart) && int.TryParse(numericPart, out int currentNumber))
                        {
                            if (currentNumber > maxIdNumber)
                            {
                                maxIdNumber = currentNumber;
                            }
                        }
                    }

                    nextId = "EXP" + (maxIdNumber + 1).ToString("D6");
                }
            }
            catch (Exception)
            {
                throw;
            }

            return nextId;
        }

        private bool ColumnExists(string tableName, string columnName)
        {
            try
            {
                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND LOWER(COLUMN_NAME) = LOWER(@columnName)");

                objSqlCommand = new SqlCommand(sqlQueryBuilder.ToString());
                objSqlCommand.Parameters.AddWithValue("@tableName", tableName);
                objSqlCommand.Parameters.AddWithValue("@columnName", columnName);

                DataTable dt = objUtilitiy.GetDataTable(objSqlCommand);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return Convert.ToInt32(dt.Rows[0][0]) > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return false;
        }

        private string GetExistingColumnName(string tableName, params string[] columnNames)
        {
            foreach (string columnName in columnNames)
            {
                if (ColumnExists(tableName, columnName))
                {
                    return columnName;
                }
            }

            return null;
        }

        public int CreateExpenses(ClsExpensesMaster objExpensesMaster)
        {
            int rowsAffected = 0;

            try
            {
                objUtilitiy.BeginTransaction();
                objUtilitiy.sqlCommand.Parameters.Clear();

                string createdByColumn = GetExistingColumnName("expense_master", "created_by", "CreatedBy");
                bool hasCreatedBy = !string.IsNullOrEmpty(createdByColumn);

                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("INSERT INTO expense_master (expense_id, expense_name, expense_code, is_active");
                if (hasCreatedBy)
                {
                    sqlQueryBuilder.Append(", " + createdByColumn);
                }
                sqlQueryBuilder.Append(") VALUES (@expense_id, @expense_name, @expense_code, @is_active");
                if (hasCreatedBy)
                {
                    sqlQueryBuilder.Append(", @created_by");
                }
                sqlQueryBuilder.Append(");");

                objUtilitiy.sqlCommand.CommandText = sqlQueryBuilder.ToString();
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_id", objExpensesMaster.expense_id ?? GetNextExpenseId());
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_name", objExpensesMaster.expense_name ?? string.Empty);
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_code", objExpensesMaster.expense_code ?? string.Empty);
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@is_active", objExpensesMaster.is_active);
                if (hasCreatedBy)
                {
                    objUtilitiy.sqlCommand.Parameters.AddWithValue("@created_by", string.IsNullOrWhiteSpace(objExpensesMaster.created_by) ? DBNull.Value : (object)objExpensesMaster.created_by);
                }

                rowsAffected += objUtilitiy.ExecuteNonQueryTransaction();
                objUtilitiy.CommitTransaction();
            }
            catch (Exception)
            {
                objUtilitiy.RollbackTransaction();
                throw;
            }

            return rowsAffected;
        }

        public int UpdateExpense(ClsExpensesMaster objExpenseMaster)
        {
            int rowsAffected = 0;

            try
            {
                objUtilitiy.BeginTransaction();
                objUtilitiy.sqlCommand.Parameters.Clear();

                string updatedByColumn = GetExistingColumnName("expense_master", "modified_by", "modified_by");
                bool hasUpdatedBy = !string.IsNullOrEmpty(updatedByColumn);

                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("UPDATE expense_master SET expense_name = @expense_name, expense_code = @expense_code, is_active = @is_active, updated_dt = @modified_date");
                if (hasUpdatedBy)
                {
                    sqlQueryBuilder.Append(", " + updatedByColumn + " = @updated_by");
                }
                sqlQueryBuilder.Append(" WHERE expense_id = @expense_id");

                objUtilitiy.sqlCommand.CommandText = sqlQueryBuilder.ToString();
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_id", objExpenseMaster.expense_id ?? string.Empty);
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_name", objExpenseMaster.expense_name ?? string.Empty);
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_code", objExpenseMaster.expense_code ?? string.Empty);
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@is_active", objExpenseMaster.is_active);
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@modified_date", objExpenseMaster.modified_date ?? (object)DBNull.Value);
                if (hasUpdatedBy)
                {
                    objUtilitiy.sqlCommand.Parameters.AddWithValue("@updated_by", string.IsNullOrWhiteSpace(objExpenseMaster.modified_by) ? DBNull.Value : (object)objExpenseMaster.modified_by);
                }

                rowsAffected += objUtilitiy.ExecuteNonQueryTransaction();
                objUtilitiy.CommitTransaction();
            }
            catch (Exception)
            {
                objUtilitiy.RollbackTransaction();
                throw;
            }

            return rowsAffected;
        }

        public int DeleteExpense(ClsExpensesMaster objExpenseMaster)
        {
            int rowsAffected = 0;

            try
            {
                objUtilitiy.BeginTransaction();

                sqlQueryBuilder = new StringBuilder();
                sqlQueryBuilder.Append("DELETE FROM expense_master WHERE expense_id = @expense_id");

                objUtilitiy.sqlCommand.CommandText = sqlQueryBuilder.ToString();
                objUtilitiy.sqlCommand.Parameters.AddWithValue("@expense_id", objExpenseMaster.expense_id ?? string.Empty);
                rowsAffected += objUtilitiy.ExecuteNonQueryTransaction();
                objUtilitiy.CommitTransaction();
            }
            catch (Exception)
            {
                objUtilitiy.RollbackTransaction();
            }

            return rowsAffected;
        }
    }
}
