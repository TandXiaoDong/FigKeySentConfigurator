using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using CommonUtils.FileHelper;

namespace SentConfigurator.Common
{
    public class QuickSigData
    {
        public static DataTable quickSigData;
        private const string COLUMN_DATA1 = "快信号DATA1(十进制)";
        private const string COLUMN_DATA2 = "快信号DATA2(十进制)";

        public static void ExportQuickSigMode()
        {
            DataTable data = new DataTable();
            data.Columns.Add(COLUMN_DATA1);
            data.Columns.Add(COLUMN_DATA2);
            for (int i = 0; i < 3; i++)
            {
                DataRow dr = data.NewRow();
                dr[COLUMN_DATA1] = i + 1;
                dr[COLUMN_DATA2] = i + 1;
                data.Rows.Add(dr);
            }
            ExcelHelper.DataTableToExcel(data, "Sheet1", true);
        }

        public static bool ImportQuickSigData()
        {
            var fileObj = FileSelect.GetSelectFileContent("Microsoft Excel files(*.xls)|*.xls", "打开文件");
            if (fileObj == null)
                return false;
            if (fileObj.FileName == "")
                return false;

            var data = ExcelHelper.ExcelToDataTable("Sheet1", true, fileObj.FileName);
            if (data == null)
                return false;
            if (data.Rows.Count <= 0)
                return false;
            if (quickSigData != null)
                quickSigData.Clear();
            quickSigData = data;
            return true;
        }
    }
}
