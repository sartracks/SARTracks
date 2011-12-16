using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.ObjectModel;
using EPPlus = OfficeOpenXml;
using Npoi = NPOI.HSSF.UserModel;

namespace SarTracks.Website
{
    public enum ExcelFileType
    {
        XLS,
        XLSX
    }

    public class ExcelService
    {
        public static ExcelFile Create(ExcelFileType type)
        {
            ExcelFile newFile;
            switch (type)
            {
                case ExcelFileType.XLS:
                    newFile = new NpoiExcelFile();
                    break;
                case ExcelFileType.XLSX:
                    newFile = new EPPlusExcelFile();
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return newFile;
        }

        public static ExcelFile Read(System.IO.Stream inputStream, ExcelFileType type)
        {
            ExcelFile file;
            switch (type)
            {
                case ExcelFileType.XLS:
                    file = new NpoiExcelFile(inputStream);
                    break;
                case ExcelFileType.XLSX:
                    file = new EPPlusExcelFile(inputStream);
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return file;
        }
    }

    #region ExcelFile
    public abstract class ExcelFile : IDisposable
    {        
        public abstract void Save(System.IO.Stream stream);

        public abstract ExcelSheet CreateSheet(string name);

        public abstract ExcelSheet GetSheet(int index);
        public abstract ExcelSheet GetSheet(string name);
        public abstract string Mime { get; }
        public abstract string AddExtension(string filename);

        public abstract void Dispose();
    }

    public class EPPlusExcelFile : ExcelFile
    {
        EPPlus.ExcelPackage _package;
        Dictionary<EPPlus.ExcelWorksheet, ExcelSheet> _sheets = new Dictionary<EPPlus.ExcelWorksheet, ExcelSheet>();

        public EPPlusExcelFile()
        {
            _package = new EPPlus.ExcelPackage();            
        }

        public EPPlusExcelFile(System.IO.Stream stream)
        {
            _package = new EPPlus.ExcelPackage(stream);
        }

        public override ExcelSheet CreateSheet(string name)
        {
            var native = this._package.Workbook.Worksheets.Add(name);
            return ResolveSheet(native);
        }

        public override ExcelSheet GetSheet(int index)
        {
            EPPlus.ExcelWorksheet native = this._package.Workbook.Worksheets[index + 1];
            return ResolveSheet(native);
        }

        public override ExcelSheet GetSheet(string name)
        {
            EPPlus.ExcelWorksheet native = this._package.Workbook.Worksheets[name];
            return ResolveSheet(native);
        }

        private ExcelSheet ResolveSheet(EPPlus.ExcelWorksheet native)
        {
            if (!_sheets.ContainsKey(native))
            {
                _sheets.Add(native, new EPPlusExcelSheet(native));
            }
            return _sheets[native];
        }

        public override void Save(System.IO.Stream stream)
        {
            _package.SaveAs(stream);
        }

        public override string Mime
        {
            get { return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; }
        }

        public override string AddExtension(string filename)
        {
            return filename + ".xlsx";
        }

        public override void Dispose()
        {
            _package.Dispose();
        }
    }

    public class NpoiExcelFile : ExcelFile
    {
        Npoi.HSSFWorkbook _workbook;
        Dictionary<Npoi.HSSFSheet, ExcelSheet> _sheets = new Dictionary<Npoi.HSSFSheet,ExcelSheet>();

        public NpoiExcelFile()
        {
            _workbook = new Npoi.HSSFWorkbook();            
        }

        public NpoiExcelFile(System.IO.Stream input)
        {
            _workbook = new Npoi.HSSFWorkbook(input);
        }


        public override ExcelSheet CreateSheet(string name)
        {
            var native = (Npoi.HSSFSheet)this._workbook.CreateSheet(name);
            return ResolveSheet(native);
        }

        public override ExcelSheet GetSheet(int index)
        {
            Npoi.HSSFSheet native = (Npoi.HSSFSheet)this._workbook.GetSheetAt(index);
            return ResolveSheet(native);
        }

        public override ExcelSheet GetSheet(string name)
        {
            Npoi.HSSFSheet native = (Npoi.HSSFSheet)this._workbook.GetSheet(name);
            return ResolveSheet(native);
        }

        private ExcelSheet ResolveSheet(Npoi.HSSFSheet native)
        {
            if (!_sheets.ContainsKey(native))
            {
                _sheets.Add(native, new NpoiExcelSheet(native));
            }
            return _sheets[native];
        }

        public override void Save(System.IO.Stream stream)
        {
            this._workbook.Write(stream);
        }

        public override string Mime
        {
            get { return "application/vnd.ms-excel"; }
        }

        public override string AddExtension(string filename)
        {
            return filename + ".xls";
        }

        public override void Dispose()
        {
            this._workbook.Dispose();
        }
    }
    #endregion

    #region ExcelSheet
    public abstract class ExcelSheet
    {
        public abstract ExcelCell CellAt(int row, int col);
    }

    public class EPPlusExcelSheet : ExcelSheet
    {
        private EPPlus.ExcelWorksheet _sheet;
        Dictionary<EPPlus.ExcelRange, ExcelCell> _cells = new Dictionary<EPPlus.ExcelRange, ExcelCell>();

        public EPPlusExcelSheet(EPPlus.ExcelWorksheet sheet)
        {
            this._sheet = sheet;
        }

        public override ExcelCell CellAt(int row, int col)
        {
            return ResolveCell(this._sheet.Cells[row + 1, col + 1]);
        }

        private ExcelCell ResolveCell(EPPlus.ExcelRange native)
        {
            if (!_cells.ContainsKey(native))
            {
                _cells.Add(native, new EPPlusExcelCell(native));
            }
            return _cells[native];
        }

    }
    public class NpoiExcelSheet : ExcelSheet
    {
        private Npoi.HSSFSheet _sheet;
        Dictionary<Npoi.HSSFCell, ExcelCell> _cells = new Dictionary<Npoi.HSSFCell, ExcelCell>();

        public NpoiExcelSheet(Npoi.HSSFSheet sheet)
        {
            this._sheet = sheet;
        }

        public override ExcelCell CellAt(int row, int col)
        {
            var dataRow = this._sheet.GetRow(row);
            if (dataRow == null)
            {
                dataRow = this._sheet.CreateRow(row);
            }

            if (col > dataRow.LastCellNum)
            {
                dataRow.CreateCell(col);
            }

            Npoi.HSSFCell cell = dataRow.GetCell(col) as Npoi.HSSFCell;
            if (cell == null)
            {
                cell = (Npoi.HSSFCell)dataRow.CreateCell(col);
            }

            return ResolveCell(cell);
        }

        private ExcelCell ResolveCell(Npoi.HSSFCell native)
        {
            if (!_cells.ContainsKey(native))
            {
                _cells.Add(native, new NpoiExcelCell(native));
            }
            return _cells[native];
        }
    }
    #endregion

    public abstract class ExcelCell
    {
        public abstract void SetValue(double value);

        public abstract double? NumericValue { get; }
        public abstract string StringValue { get; }
        public abstract DateTime? DateValue { get; }
        //public abstract string StringValue { get; }
        //public abstract DateTime? DateValue { get; }
    }

    public class EPPlusExcelCell : ExcelCell
    {
        private EPPlus.ExcelRange _cell;
        public EPPlusExcelCell(EPPlus.ExcelRange cell)
        {
            _cell = cell;
        }
        
        public override void SetValue(double value)
        {
            this._cell.Value = value;
        }

        public override double? NumericValue
        {
            get { double d; if (!double.TryParse(string.Format("{0}", this._cell.Value), out d)) return null; return d; }
        }

        public override DateTime? DateValue
        {
            get { return typeof(DateTime).IsAssignableFrom(this._cell.Value.GetType()) ? (DateTime?)this._cell.Value : null; }
        }

        public override string StringValue
        {
            get { return string.Format("{0}", this._cell.Value); }
        }
    }

    public class NpoiExcelCell : ExcelCell
    {
        private Npoi.HSSFCell _cell;
        public NpoiExcelCell(Npoi.HSSFCell cell)
        {
            _cell = cell;
        }

        public override void SetValue(double value)
        {
            this._cell.SetCellValue(value);
        }

        public override double? NumericValue
        {
            get { return (this._cell.CellType == NPOI.SS.UserModel.CellType.NUMERIC) ? this._cell.NumericCellValue : (double?)null; }
        }

        public override string StringValue
        {
            get
            {
                object o;
                switch(this._cell.CellType)
                {
                    case NPOI.SS.UserModel.CellType.NUMERIC:
                        o = this._cell.NumericCellValue;
                        break;
                    case NPOI.SS.UserModel.CellType.STRING:
                        o = this._cell.StringCellValue;
                        break;
                    case NPOI.SS.UserModel.CellType.BLANK:
                        return null;                       
                    default:
                        throw new NotImplementedException(this._cell.CellType.ToString());
                }
                return string.Format("{0}", o);
            }
        }

        public override DateTime? DateValue
        {
            get { return this._cell.DateCellValue; }
        }
    }
}