using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Helpers
{
    public static class ExportHelper
    {
        public static void CorrectExportData(TableData obj)
        {
            if (obj.Paging == null) obj.Paging = new PagingData
            {
                Index = 1,
                Size = obj.Export != null ? obj.Export.Limit : 1000
            };
            else
            {
                obj.Paging.Index = 1;
                obj.Paging.Size = obj.Export != null ? obj.Export.Limit : 1000;
            }
            var filters = obj.Filters ?? new List<FilterData>();
            var dateFilter = filters.Where(c => c.Name == "LookupDate").FirstOrDefault();
            if (dateFilter == null && obj.Export != null && !obj.Export.DateRange.IsNullOrEmpty())
            {
                dateFilter = new FilterData
                {
                    Name = "LookupDate",
                    Compare = CompareType.D_Between,
                    Value = obj.Export.DateRange[0],
                    Value2 = obj.Export.DateRange[1],
                };
                obj.Filters.Add(dateFilter);
            }
        }

        public static FileContentResult ExportToCsv(string name, DataTable table)
        {
            var index = 0;
            var workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add(name);
            foreach (var column in table.Columns.Cast<DataColumn>())
            {
                index += 1;
                worksheet.Cell(1, index).Style.Font.Bold = true;
                worksheet.Cell(1, index).Value = column.ColumnName;
                worksheet.Cell(1, index).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(1, index).Style.Fill.BackgroundColor = XLColor.Blue;
                worksheet.Cell(1, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(1, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            index = 0;
            foreach (var row in table.Rows.Cast<DataRow>())
            {
                index += 1;
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    worksheet.Cell(index + 1, i + 1).Value = row[i] != null ? row[i].ToString() : string.Empty;
                }
            }
            worksheet.Rows().AdjustToContents();
            worksheet.Columns().AdjustToContents();

            var lastCellAddress = worksheet.RangeUsed().LastCell().Address;
            var contentText = string.Join(Environment.NewLine, worksheet.Rows(1, lastCellAddress.RowNumber)
                .Select(r => string.Join(",", r.Cells(1, lastCellAddress.ColumnNumber)
                        .Select(cell =>
                        {
                            var cellValue = cell.GetValue<string>();
                            return $"\"{cellValue}\"";
                        }))));
            var contentType = "application/csv";
            var data = Encoding.UTF8.GetBytes(contentText);
            var result = Encoding.UTF8.GetPreamble().Concat(data).ToArray();

            return new FileContentResult(result, contentType);
        }

        public static FileContentResult ExportToExcel(string name, DataTable table)
        {
            var index = 0;
            var workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add(name);
            foreach (var column in table.Columns.Cast<DataColumn>())
            {
                index += 1;
                worksheet.Cell(1, index).Style.Font.Bold = true;
                worksheet.Cell(1, index).Value = column.ColumnName;
                worksheet.Cell(1, index).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(1, index).Style.Fill.BackgroundColor = XLColor.Blue;
                worksheet.Cell(1, index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Cell(1, index).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            index = 0;
            foreach (var row in table.Rows.Cast<DataRow>())
            {
                index += 1;
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var columnName = table.Columns[i].ColumnName;
                    if (columnName.ContainsEx("ngày") ||
                        columnName.ContainsEx("tiền") ||
                        columnName.ContainsEx("thời gian") ||
                        columnName.ContainsEx("điện thoại"))
                        worksheet.Cell(index + 1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    var value = row[i] != null ? row[i].ToString() : string.Empty;
                    worksheet.Cell(index + 1, i + 1).SetValue(value);
                }
            }
            worksheet.Rows().AdjustToContents();
            worksheet.Columns().AdjustToContents(100, 250);

            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return new FileContentResult(content, contentType);
            }
        }

        public static FileContentResult ExportToExcelByWorkbook(XLWorkbook workbook)
        {
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return new FileContentResult(content, contentType);
            }
        }

        public static void ChangeTableColumns(DataTable table, string oldColumn, string newColumn)
        {
            if (table.Columns[oldColumn] != null)
            {
                table.Columns[oldColumn].ColumnName = newColumn;
            }
        }
    }
}
