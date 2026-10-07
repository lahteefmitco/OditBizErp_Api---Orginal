using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Authentication;
using MictcoWebService.Common;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    [ApiController]
    [Authorize(Roles = UserRoles.User)]
    public class CRMController : ControllerBase
    {
        [HttpGet("last-crm-enquiry-id")]
        public string lastCrmEnquiryId()
        {
            UserSqlServer usqlre = new UserSqlServer(this);
            string sql = "select MAX(ce_id) as max_entry_no,min(ce_id) as min_entry_no from crm_enquiry";
            DataTable dt = usqlre.dbReaderFill(sql);
            usqlre.close();
            return ReportModelContext.searializeDt(dt);
        }

        [HttpGet("crm-enquiry-data")]
        public async Task<IActionResult> crmEnquiryData()
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                string query = " select cec_id as value,cec_name as label from crm_enquiry_category";
                 query += " select cet_id as value,cet_name as label from crm_enquiry_type";
                 query += " select cep_id as value,cep_name as label,cep_max_limit as limit from crm_enquiry_peroid";
                query += " select cpc_id as value,cpc_name as label from crm_product_category";
                query += " select cls_id as value,cls_name as label from crm_lead_source";
                query += " select ces_id as value,ces_name as label from crm_enquiry_status";
                DataSet dt = usqlre.dbreadDataset(query);
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                hash.Add("category", dt.Tables["Table"]);
                hash.Add("type", dt.Tables["Table1"]);
                hash.Add("period", dt.Tables["Table2"]);
                hash.Add("product", dt.Tables["Table3"]);
                hash.Add("source", dt.Tables["Table4"]);
                hash.Add("status", dt.Tables["Table5"]);

                return Ok(ReportModelContext.searializeDt(hash));
            }
            catch (Exception e)
            {
                return Ok(new { status = false, message = e.ToString() });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

        }

        [HttpPost("save-crm-enquiry")]
        public async Task<IActionResult> saveCrmEnquiry([FromBody] CrmEnquiry model)
        {
            UserSqlServer usqlre = new UserSqlServer(this);

            

            if (usqlre.OpenConnection())
            {
                try
                {
                    DataSet dataset = new DataSet();

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "Sp_Save_Crm_Enquiry";
                    SqlParameter parm = new SqlParameter("@return", SqlDbType.Int);
                    parm.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(parm);

                    cmd.Parameters.AddWithValue("@ce_salesman_id", model.salesmanId);
                    cmd.Parameters.AddWithValue("@ce_date", model.date);
                    cmd.Parameters.AddWithValue("@ce_time", model.time);
                    cmd.Parameters.AddWithValue("@ce_name", model.customerName);
                    cmd.Parameters.AddWithValue("@ce_office", model.business);
                    cmd.Parameters.AddWithValue("@ce_place", model.place);
                    cmd.Parameters.AddWithValue("@ce_mobile", model.mobile);
                    cmd.Parameters.AddWithValue("@ce_email", model.email);
                    cmd.Parameters.AddWithValue("@ce_remark", model.remark);
                    cmd.Parameters.AddWithValue("@ce_cec_id", model.categoryId);
                    cmd.Parameters.AddWithValue("@ce_cet_id", model.typeId);
                    cmd.Parameters.AddWithValue("@ce_last_modified", model.cdate);
                    cmd.Parameters.AddWithValue("@ce_created_by", Convert.ToInt32(usqlre.userId));
                    cmd.Parameters.AddWithValue("@ce_type", model.type);
                    cmd.Parameters.AddWithValue("@ce_cls_id", model.sourceId);
                    cmd.Parameters.AddWithValue("@ce_product_catogery", model.productCatogery);
                    cmd.Parameters.AddWithValue("@ce_reference", model.reference);
                    cmd.Parameters.AddWithValue("@ct_cep_id", model.periodId);
                    cmd.Parameters.AddWithValue("@ct_ces_id", model.status);
                    cmd.Parameters.AddWithValue("@ce_next_followup_date", model.nextDate);

                    cmd.Parameters.AddWithValue("@StatementType", "Insert");
                    cmd.CommandTimeout = 60;
                    cmd.Connection = usqlre.shop;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dr.Dispose();
                    dr.Close();

                    int enquiryId = Convert.ToInt32(parm.Value);
                    return Ok(new { status = true, entryno = enquiryId });
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    return Ok(new { status = false, message = ex.ToString() });
                }
                finally
                {
                    if (usqlre != null)
                        usqlre.close();
                }
            }
            return Ok(new { status = false });
        }

        [HttpPost("crm-enquiry-report")]
        public async Task<IActionResult> crmEnquiryReport([FromBody] CrmEnquiry model)
        {
            UserSqlServer usqlre = null;
            try
            {
                usqlre = new UserSqlServer(this);
                String query = "EXEC [dbo].[Sp_Crm_Enquiry_Report]\n" +
                   "\t\t@search = '" + model.search + "',\n" +
                       "\t\t@fromDate = '" + model.fromDate + "',\n" +
                       "\t\t@toDate = '" + model.toDate + "',\n" +
                       "\t\t@type = '" + model.type + "',\n" +
                       "\t\t@reportType = '" + model.reportType + "'\n";
                DataTable dt = usqlre.dbReaderFill(query);
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                return Ok(ReportModelContext.searializeDt(dt));
            }
            catch (Exception e)
            {
                return Ok(new { status = false, message = e.ToString() });
            }
            finally
            {
                if (usqlre != null)
                    usqlre.close();
            }

            //UserSqlServer sqlh = null;
            //try
            //{
            //    sqlh = new UserSqlServer(this);
            //    //DataTable select = sqlh.getReportColumns("crmEnquiryReport");
            //    string sql = "";
                //if (select.Rows.Count > 0)
                //{
                    //sql = "EXEC [dbo].[Sp_Crm_Enquiry_Report]\n" +
                    //   "\t\t@search = '" + model.search + "',\n" +
                    //   "\t\t@fromDate = '" + model.fromDate + "',\n" +
                    //   "\t\t@toDate = '" + model.toDate + "'\n" +
                    //   "\t\t@type = '" + model.type + "'\n" +
                    //   "\t\t@reportType = '" + model.reportType + "'\n";
                       //"\t\t@select_col = '" + select.Rows[0]["grc_m_select"].ToString() + "'\n" +
                       //"\t\t@statementType = '" + model.statementType + "'\n";
                    //sql = "select " + select.Rows[0]["grc_m_select"].ToString() + " from crm_enquiry where ce_next_followup_date >= '" + model.fromDate + "' AND ce_next_followup_date <= '" + model.toDate + "' and ce_type = '" + model.type + "' ";
                //}
                //DataTable dt = sqlh.dbReaderFill(sql);
                //Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();
                //hash.Add("crm", dt);
                //DataTable dts = new DataTable();
                //dts.Columns.Add("column");
                //dts.Columns.Add("columnas");
                //dts.Columns.Add("width");
                //dts.Columns.Add("align");

                //if (select.Rows.Count > 0)
                //{
                //    DataRow row = dts.NewRow();
                //    row["column"] = select.Rows[0]["grc_m_column_name"].ToString();
                //    row["columnas"] = select.Rows[0]["grc_m_as_name"].ToString();
                //    row["width"] = select.Rows[0]["grc_m_width"].ToString();
                //    row["align"] = select.Rows[0]["grc_m_align"].ToString();
                //    dts.Rows.Add(row);
                //}

                //hash.Add("select", dts);
            //    sqlh.close();
            //    return Ok(ReportModelContext.searializeDt(dt));
            //}
            //catch (Exception e)
            //{
            //    return Ok(new { status = false, message = e.ToString() });
            //}
            //finally
            //{
            //    if (sqlh != null)
            //        sqlh.close();
            //}

        }

        [HttpGet("crm-enquiry/{id}")]
        public async Task<IActionResult> crmEnquiry(int id)
        {
            UserSqlServer sqlh = null;
            try
            {
                sqlh = new UserSqlServer(this);
                string query = " select ce_name as name,ce_mobile as mobile,convert(varchar(11), ce_created_date, 106) as createdDate,ce_place as place,convert(varchar(11), ce_next_followup_date , 106) as nextDate,ce_type as type,ce_id as enquiryId from crm_enquiry where ce_id = " + id + "";
                query += " select cep_id as value,cep_name as label,cep_max_limit as limit from crm_enquiry_peroid left join crm_transaction on ct_cep_id = cep_id where ct_id = " + id + "";
                query += " select ces_id as value,ces_name as label from crm_enquiry_status left join crm_transaction on ces_id = ct_ces_id where ct_id = " + id + "";
                query += " select cec_id as value,cec_name as label from crm_enquiry_category left join crm_enquiry on ce_cec_id = cec_id where ce_id= " + id + "";
                query += " select convert(varchar(11), ct_date, 106) as cdate,gu_name as lastModifiedBy,ct_remark as remark,ct_ce_id,ct_id as transactionId from crm_transaction left join crm_enquiry on ct_ce_id = ce_id left join gnl_users on gu_user_id = ct_last_modified_by where ct_ce_id= " + id + " order by ct_id asc ";
                DataSet dt = sqlh.dbreadDataset(query);
                Dictionary<string, DataTable> hash = new Dictionary<string, DataTable>();

                hash.Add("crmEnquiry", dt.Tables["Table"]);
                hash.Add("period", dt.Tables["Table1"]);
                hash.Add("status", dt.Tables["Table2"]);
                hash.Add("category", dt.Tables["Table3"]);
                hash.Add("transaction", dt.Tables["Table4"]);
                return Ok(ReportModelContext.searializeDt(hash));
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (sqlh != null)
                    sqlh.close();
            }

        }

        [HttpPost("followup-crm-enquiry")]
        public async Task<IActionResult> followupCrmEnquiry([FromBody] CrmEnquiry model)
        {
            UserSqlServer csqlre = null;
            try
            {
                csqlre = new UserSqlServer(this);
                using (csqlre.shop)
                {

                    string updateQuery = " update crm_enquiry set ce_last_modified=@ce_last_modified,ce_next_followup_date=@ce_next_followup_date where ce_id=@ce_id ";
                    using (SqlCommand cmd = new SqlCommand(updateQuery))
                    {
                        cmd.Connection = csqlre.shop;
                        cmd.Parameters.Add("@ce_last_modified", SqlDbType.VarChar, 30).Value = model.cdate;
                        cmd.Parameters.Add("@ce_next_followup_date", SqlDbType.VarChar, 30).Value = model.nextDate;
                        cmd.Parameters.Add("@ce_id", SqlDbType.Int, 30).Value = model.id;
                        csqlre.OpenConnection();
                        cmd.ExecuteNonQuery();
                    }
                   
                    string update = " insert into crm_transaction(ct_cep_id, ct_date, ct_last_modified_by, ct_remark, ct_ces_id,ct_ce_id) values(@ct_cep_id, @ct_date, @ct_last_modified_by, @ct_remark, @ct_ces_id,@ct_ce_id)";
                    using (SqlCommand cmd = new SqlCommand(update))
                    {
                        cmd.Connection = csqlre.shop;
                        cmd.Parameters.Add("@ct_cep_id", SqlDbType.Int, 30).Value = model.periodId;
                        cmd.Parameters.Add("@ct_date", SqlDbType.VarChar, 30).Value = model.cdate;
                        cmd.Parameters.Add("@ct_last_modified_by", SqlDbType.Int, 30).Value = Convert.ToInt32(csqlre.userId);
                        cmd.Parameters.Add("@ct_remark", SqlDbType.VarChar, 500).Value = model.remark;
                        cmd.Parameters.Add("@ct_ces_id", SqlDbType.Int, 30).Value = model.status;
                        cmd.Parameters.Add("@ct_ce_id", SqlDbType.Int, 30).Value = model.id;
                        csqlre.OpenConnection();
                        cmd.ExecuteNonQuery();
                    }

                }

                return Ok(new { status = true });
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, message = ex.ToString() });
            }
            finally
            {
                if (csqlre != null)
                    csqlre.close();
            }
        }

        [HttpPost("crm-check-phone")]
        public async Task<IActionResult> crmCheckPhone([FromBody] CrmEnquiry model)
        {
            SqlServerHelper sqlh = null;
            try
            {
                sqlh = new SqlServerHelper(this);
                string sql = " select ce_mobile from crm_enquiry where ce_mobile = '" + model.mobile + "'";
                DataTable dt = sqlh.dbReaderFill(sql);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                        return Ok(new { status = false, message = "Customer Already Saved" });
                }
                return Ok(new { status = true });
            }
            catch
            {
                return Ok(new { status = false });
            }
            finally
            {
                if (sqlh != null)
                    sqlh.close();
            }

        }

    }
}