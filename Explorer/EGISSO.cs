using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using System.Drawing;
using System.Text.RegularExpressions;


namespace EGISSOEditor
{
    public static class EGISSO
    {
        public static bool isInizialize { private get; set; }

        private static Excel.Application ExcelApp;
        private delegate string ActionOfLine(string Value);

        public static void Init()
        {
            ExcelApp = new Excel.Application();
            ExcelApp.Visible = false;
            ExcelApp.DisplayAlerts = false;
            isInizialize = true;    
        }

        public static void CloseExcel()
        {
            if (isInizialize)
            {
                ExcelApp.DisplayAlerts = true;
                try
                {
                    ExcelApp.Quit();
                }
                catch {} 
            }
        }

        public static bool AdjustByPatternWorkBook(ref Excel.Workbook book)
        {
            while (book.Sheets.Count < 3)
            {
                book.Sheets.Add();
            }

            try
            {
                Excel.Workbook patternWB = OpenPatternBook();
                for (int i = 1; i <= 3; i++)
                {
                    Excel.Worksheet pSheet = patternWB.Sheets[i];
                    Excel.Worksheet NewSheet = book.Sheets[i];
                    NewSheet.Name = pSheet.Name;

                    Excel.Range PatternRange = pSheet.Range[pSheet.Cells[1, 1], pSheet.Cells[100, 100]];
                    PatternRange.Copy();
                    Excel.Range SelectRange = NewSheet.Range[NewSheet.Cells[1, 1], NewSheet.Cells[1, 1]];
                    SelectRange.PasteSpecial();
                }
                patternWB?.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async static Task CombineFilesAsync(List<FileEGISSO> files, string savePath, IProgress<GetProcessInformation> processInfo, CancellationToken cancel)
        {
            if (!isInizialize)
                return;

            if (!(files.Count > 0))
                return;

            await Task.Run(() =>
            {
                GetProcessInformation processInformation = new GetProcessInformation("Обединение файлов", "", 1, files.Count, 0, 0); 

                int cuntRowPaste = 6, countRow = 0;

                Excel.Workbook newWorkBook = ExcelApp.Workbooks.Add(Type.Missing);
                AdjustByPatternWorkBook(ref newWorkBook);

                Excel.Range selectRange;
                Excel.Worksheet newWorkSheet = newWorkBook.Sheets[1];

                Excel.Workbook workbook;
                Excel.Worksheet worksheet;
                for (int i = 0; i < files.Count; i++)
                {
                    workbook = ExcelApp.Workbooks.Open(files[i].TempDirectory);
                    worksheet = workbook.Sheets[1];

                    processInformation.Change("Обединение файлов", workbook.Name, i+1, files.Count, 0, 100);
                    processInfo?.Report(processInformation);

                    countRow = recountRow(workbook);
                    selectRange = worksheet.Range[worksheet.Cells[7, 1], worksheet.Cells[countRow, 56]];
                    selectRange.Copy();
                    Excel.Range range1 = newWorkSheet.Range[newWorkSheet.Cells[cuntRowPaste + 1, 1], newWorkSheet.Cells[cuntRowPaste + 1, 1]];
                    range1.PasteSpecial();
                    cuntRowPaste += countRow - 6;

                    processInformation.Change("Обединение файлов", workbook.Name, i+1, files.Count, 100, 100);
                    processInfo?.Report(processInformation);

                    workbook.Close();

                    if (cancel != null)
                    {
                        if (cancel.IsCancellationRequested)
                        {
                            newWorkBook.Close();
                            cancel.ThrowIfCancellationRequested();
                        }
                    } 
                }

                newWorkBook.SaveAs(savePath);
                newWorkBook.Close();
            });
        }

        public async static Task ProcessFilesAsync(List<FileEGISSO> files, ActionOnFile action, IProgress<GetProcessInformation> processInfo, IProgress<FileEGISSO> hasProcessFile, CancellationToken cancel)
        {
            if (!isInizialize) 
                return;

            if (!(files.Count > 0))
                return;

            string processName = (action == ActionOnFile.PatternCorrection) ? "Корректировка шаблона" : "Поиск ошибок";
            GetProcessInformation processInformation = new GetProcessInformation(processName, "", 0, files.Count, 0, 0);

            Excel.Workbook workbook;
            
            for (int i = 0; i < files.Count; i++)
            {
                Progress<GetProcessInformation> ProcessProgressInfo = new Progress<GetProcessInformation>((info) => {
                    processInformation.Change(processName, info.CurrentFileName, i + 1, files.Count, info.CurrentFileProgress, info.TotalFilesProgress);
                    processInfo?.Report(processInformation);
                });

                workbook = ExcelApp.Workbooks.Open(files[i].TempDirectory);
               
                if (action == ActionOnFile.PatternCorrection)
                    await Task.Run(()=>PatternCorrection(workbook, ProcessProgressInfo, cancel));
                else if (action == ActionOnFile.ErrorChecking)
                    await Task.Run(() => ErrorChecking(workbook, ProcessProgressInfo, cancel));
                hasProcessFile.Report(files[i]);

                if (cancel != null)
                    if (cancel.IsCancellationRequested)
                        return;
            }
        }

        private static void PatternCorrection(Excel.Workbook workbook, IProgress<GetProcessInformation> processInfo, CancellationToken cancel)
        {
            Excel.Worksheet worksheet = workbook.Sheets[1];
            int countRow = recountRow(workbook);
            
            Excel.Workbook patternWB = null;
            Excel.Worksheet patternSheet = null;
            bool isOpenPattern = false;
            try
            {
                patternWB = OpenPatternBook();
                patternSheet = patternWB.Sheets[1];
                isOpenPattern = true;
            }
            catch { }

            int progressCount = isOpenPattern == true ? 59 : 3;

            reportProgressProcess(0, progressCount);
            if (checkIsCancel(cancel, workbook)) return;

            Excel.Worksheet newSheet = workbook.Sheets.Add();
            Excel.Range range = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[countRow, 56]];
            range.Copy();
            newSheet.Paste();
            worksheet.Delete();
            newSheet.Name = "Формат";

            range = newSheet.Range[newSheet.Cells[1, 1], newSheet.Cells[countRow, 56]];

            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeTop).Weight = Excel.XlBorderWeight.xlThin;
            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeBottom).Weight = Excel.XlBorderWeight.xlThin;
            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeRight).Weight = Excel.XlBorderWeight.xlThin;
            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeLeft).Weight = Excel.XlBorderWeight.xlThin;
            range.Borders.get_Item(Excel.XlBordersIndex.xlInsideHorizontal).Weight = Excel.XlBorderWeight.xlThin;
            range.Borders.get_Item(Excel.XlBordersIndex.xlInsideVertical).Weight = Excel.XlBorderWeight.xlThin;

            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeTop).LineStyle = Excel.XlLineStyle.xlContinuous;
            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeBottom).LineStyle = Excel.XlLineStyle.xlContinuous;
            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeRight).LineStyle = Excel.XlLineStyle.xlContinuous;
            range.Borders.get_Item(Excel.XlBordersIndex.xlEdgeLeft).LineStyle = Excel.XlLineStyle.xlContinuous;
            range.Borders.get_Item(Excel.XlBordersIndex.xlInsideHorizontal).LineStyle = Excel.XlLineStyle.xlContinuous;
            range.Borders.get_Item(Excel.XlBordersIndex.xlInsideVertical).LineStyle = Excel.XlLineStyle.xlContinuous;

            reportProgressProcess(1, progressCount);
            if (checkIsCancel(cancel, workbook)) return;

            range = newSheet.Range[newSheet.Cells[7, 1], newSheet.Cells[countRow, 56]];
            range.Interior.Color = ColorTranslator.ToOle(Color.White);
            range.Cells.Font.Color = ColorTranslator.ToOle(Color.Black);
            range.Cells.Font.Size = 10;
            range.Cells.Font.Name = "Times New Roman";

            range.VerticalAlignment = Excel.XlVAlign.xlVAlignBottom;
            range.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            range.Cells.Font.Bold = false;
            range.Cells.Font.Italic = false;
            range.Cells.Font.Strikethrough = false;
            range.Cells.Font.Underline = false;

            reportProgressProcess(2, progressCount);
            if (checkIsCancel(cancel, workbook)) return;

            if (isOpenPattern)
            {
                Excel.Range PatternRange = patternSheet.Range[patternSheet.Cells[1, 1], patternSheet.Cells[6, 56]];
                PatternRange.Copy();
                range = newSheet.Range[newSheet.Cells[1, 1], newSheet.Cells[1, 1]];
                range.PasteSpecial();

                for (int i = 1; i <= 56; i++)
                {
                    PatternRange = patternSheet.Range[patternSheet.Cells[7, i], patternSheet.Cells[7, i]];
                    range = newSheet.Range[newSheet.Cells[7, i], newSheet.Cells[countRow, i]];
                    range.Cells.NumberFormat = PatternRange.Cells.NumberFormat;
                    //range = newSheet.Range[newSheet.Cells[7, i], newSheet.Cells[7, i]];
                    range.ColumnWidth = PatternRange.ColumnWidth;
                    reportProgressProcess(i + 2, progressCount);
                    if (checkIsCancel(cancel, workbook)) return;
                }

                PatternRange = patternSheet.Range[patternSheet.Cells[7, 1], patternSheet.Cells[7, 1]];
                range = newSheet.Range[newSheet.Cells[7, 1], newSheet.Cells[countRow, 1]];
                range.RowHeight = PatternRange.RowHeight;
                reportProgressProcess(59, progressCount);
                if (checkIsCancel(cancel, workbook)) return;
            }
            workbook.Save();
            workbook.Close();
            patternWB?.Close();

            void reportProgressProcess(int currentProgress, int totalProgress)
            {
                GetProcessInformation processInformation = new GetProcessInformation("Корректировка шаблона", workbook.Name, 0, 0, currentProgress, totalProgress);
                Console.WriteLine($"{currentProgress} - {totalProgress} : { (float)currentProgress / (float)totalProgress}" );
                processInfo?.Report(processInformation);
            }
            bool checkIsCancel(CancellationToken cancelToken, Excel.Workbook currentWorkBook)
            {
                if (cancelToken != null)
                {
                   if (cancelToken.IsCancellationRequested)
                    {
                        currentWorkBook.Close();
                        return true;
                    }
                }
                return false;
            }
        }

        private static void ErrorChecking(Excel.Workbook workbook, IProgress<GetProcessInformation> processInfo, CancellationToken cancel)
        {
            Excel.Worksheet worksheet = workbook.Sheets[1];
            Excel.Range selectRange;
            string selectValue;

            int[][] ArrayIndexColumns = new int[10][];
            ArrayIndexColumns[0] = new int[2] { 3, 17 }; //Снилсы *
            ArrayIndexColumns[1] = new int[6] { 4, 5, 18, 19, 20, 6 }; // ФИО *
            ArrayIndexColumns[2] = new int[2] { 7, 21 }; // Пол *
            ArrayIndexColumns[3] = new int[5] { 8, 22, 33, 34, 35 }; // Даты *
            ArrayIndexColumns[4] = new int[2] { 9, 23 }; //Место рождения
            ArrayIndexColumns[5] = new int[2] { 10, 24 }; //Контактный телефон 
            ArrayIndexColumns[6] = new int[2] { 11, 25 }; //Гражданство ОКСМ
            ArrayIndexColumns[7] = new int[2] { 12, 26 }; //Документ, удостоверяющий личность
            ArrayIndexColumns[8] = new int[2] { 31, 32 }; //Идентификаторы
            
            int countRow = recountRow(workbook);

            reportProgressProcess(1, countRow - 6);
            if (checkIsCancel(cancel, workbook)) 
                return;

            selectRange = worksheet.Range[worksheet.Cells[7, 1], worksheet.Cells[countRow, 56]];
            selectRange.Interior.Color = ColorTranslator.ToOle(Color.White);

            for (int i = 7; i <= countRow; i++)
            {
                //Нумерация
                worksheet.Cells[i, 1] = i - 6;

                //OSZ Код
                if (!Regex.IsMatch(returnValueRange(i, 2, out selectRange), @"^\d{4}\.\d{6}$"))
                    selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));

                //СНИЛС
                for (int j = 0; j < ArrayIndexColumns[0].Length; j++)
                {
                    if (!checkSNILS(returnValueRange(i, ArrayIndexColumns[0][j], out selectRange,(str) => { string temp = str; return temp.Replace(" ", ""); }), out string result))
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                    else
                        worksheet.Cells[i, ArrayIndexColumns[0][j]] = result;
                }

                //ФИО
                for (int j = 0; j < ArrayIndexColumns[1].Length - 2; j++)
                {
                    if (!Regex.IsMatch(returnValueRange(i, ArrayIndexColumns[1][j], out selectRange), "^[А-яЁё\\s\\-]{1,100}$"))
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                }

                //Отчество
                for (int j = ArrayIndexColumns[1].Length - 2; j < ArrayIndexColumns[1].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[1][j], out selectRange);
                    if (!Regex.IsMatch(selectValue, "(^[А-яЁё\\s\\-]{1,100}$)|^$"))
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                }

                //Пол
                for (int j = 0; j < ArrayIndexColumns[2].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[2][j], out selectRange);
                    if (!Regex.IsMatch(selectValue, @"(^Female$|^Male$)"))
                    {
                        if (checkWordReg(selectValue, "Female", RegexOptions.IgnoreCase, false) || checkWordReg(selectValue, "жен", RegexOptions.IgnoreCase, false))
                            worksheet.Cells[i, ArrayIndexColumns[2][j]] = "Female";
                        else if (checkWordReg(selectValue, "Male", RegexOptions.IgnoreCase, true) || checkWordReg(selectValue, "муж", RegexOptions.IgnoreCase, false))
                            worksheet.Cells[i, ArrayIndexColumns[2][j]] = "Male";
                        else
                            selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                    }
                }

                //Даты
                Match DataMatch;
                for (int j = 0; j < ArrayIndexColumns[3].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[3][j], out selectRange);
                    if (!Regex.IsMatch(selectValue, @"^[0-9]{1,5}$"))
                    {
                        DataMatch = Regex.Match(selectValue, @"((0[1-9]|[12]\d)\.(0[1-9]|1[012])|30\.(0[13-9]|1[012])|31\.(0[13578]|1[02]))\.(19|20)\d\d");
                        if (DataMatch.Success == true)
                        {
                            string dateStr = DataMatch.Value;
                            DateTime dateCells = new DateTime(int.Parse(dateStr.Substring(6, 4)), int.Parse(dateStr.Substring(3, 2)), int.Parse(dateStr.Substring(0, 2)));
                            selectRange.Value2 = dateCells.ToOADate();
                        }
                            
                        else
                            selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                    }
                }

                //Место рождения
                for (int j = 0; j < ArrayIndexColumns[4].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[4][j], out selectRange, (str) => { string temp = str; return temp.Trim(); });
                    if (!Regex.IsMatch(selectValue, "([а-яА-ЯёЁ\\-0-9№(][а-яА-ЯёЁ\\-\\s',.0-9()№\"\\\\/]{1,499})|^$"))
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                }

                //Контактный телефон
                for (int j = 0; j < ArrayIndexColumns[5].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[5][j], out selectRange, (str) => { string temp = str; return temp.Replace(" ",""); });
                    if (!Regex.IsMatch(selectValue, "(^\\d{8,11}$)|^$"))
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                }

                //Гражданство ОКСМ 
                for (int j = 0; j < ArrayIndexColumns[6].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[6][j], out selectRange, (string str) => {return str.Trim(); });
                    if (!Regex.IsMatch(selectValue, "(^\\d{1,3}$)|^$"))
                    {
                        if (Regex.IsMatch(selectValue, "рос", RegexOptions.IgnoreCase))
                            selectRange.Value2 = "643";
                        else
                            selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                    }
                }

                //Сведения о локальных МСЗ и категориях
                for (int j = 0; j < ArrayIndexColumns[8].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[8][j], out selectRange, (str) => { return str.Replace(" ", ""); });
                    if (!Regex.IsMatch(selectValue, "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"))
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                }

                //Сведения о сроках действия назначения
                for (int j = 33; j<=34; j++)
                {
                    string dateComparison = returnValueRange(i, j, out selectRange);
                    if (selectRange.Interior.Color != ColorTranslator.ToOle(Color.FromArgb(255, 83, 83)))
                    {
                        selectValue = returnValueRange(i, j+1, out selectRange);
                        if (selectRange.Interior.Color != ColorTranslator.ToOle(Color.FromArgb(255, 83, 83)))
                        {
                            if (int.Parse(dateComparison) > int.Parse(selectValue))
                                selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                        }
                    }
                }

                //Сведения о нуждаемости
                selectValue = returnValueRange(i, 36, out selectRange, (str) => { str = str.Replace(" ", ""); return str == "" ? "0" : str; });
                if (!Regex.IsMatch(selectValue, "^([01]{1})$"))
                    selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));

                //Документ, удостоверяющий личность
                for (int j = 0; j < ArrayIndexColumns[7].Length; j++)
                {
                    selectValue = returnValueRange(i, ArrayIndexColumns[7][j], out selectRange);
                    if (Regex.IsMatch(selectValue, "^[1-8]{1}$"))
                    {
                        string patternSeries = "", patternNumber = ""; byte SeriesCountNumber = 0, NumberCountNumber = 0; bool CheckCountNumber = false, CheckCountSeries = false;
                        bool HasValue = true;

                        switch (selectValue)
                        {
                            case "1": patternSeries = "^\\d{4}$"; patternNumber = "^\\d{6}$"; SeriesCountNumber = 4; NumberCountNumber = 6; CheckCountNumber = true; CheckCountSeries = true; break;
                            case "2": patternSeries = "^.{1,20}$"; patternNumber = "^[0-9а-яА-ЯA-Za-z]{1,25}$"; break;
                            case "3": patternSeries = "^\\d{2}$"; patternNumber = "^\\d{7}$"; SeriesCountNumber = 2; NumberCountNumber = 7; CheckCountNumber = true; CheckCountSeries = true; break;
                            case "4": patternSeries = "^[А-Я]{2}&"; patternNumber = "^\\d{7}&"; NumberCountNumber = 7; CheckCountNumber = true; break;
                            case "5": patternSeries = "^[IVXLCDM]{1,4}[\\-][А-Я]{2}$"; patternNumber = "^\\d{6}$"; NumberCountNumber = 6; CheckCountNumber = true; break;
                            default: HasValue = false; break;
                        }

                        if (HasValue)
                        {
                            //CheckCountSeries, SeriesCountNumber
                            if (!Regex.IsMatch(returnValueRange(i, ArrayIndexColumns[7][j] + 1, out selectRange,
                                (stre) => { string str = stre.Replace(" ", ""); return CheckCountSeries == true ? str.PadLeft(SeriesCountNumber, '0') : str; }), patternSeries))
                                selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));

                            if (!Regex.IsMatch(returnValueRange(i, ArrayIndexColumns[7][j] + 2, out selectRange,
                                (stre) => { string str = stre.Replace(" ", ""); return CheckCountNumber == true ? str.PadLeft(NumberCountNumber, '0') : str; }), patternNumber))
                                selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));

                            selectValue = returnValueRange(i, ArrayIndexColumns[7][j]+3, out selectRange);
                            if (!Regex.IsMatch(selectValue, @"^[0-9]{1,5}$"))
                            {
                                DataMatch = Regex.Match(selectValue, @"((0[1-9]|[12]\d)\.(0[1-9]|1[012])|30\.(0[13-9]|1[012])|31\.(0[13578]|1[02]))\.(19|20)\d\d");
                                if (DataMatch.Success == true)
                                    selectRange.Value2 = DataMatch.Value;
                                else
                                    selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                            }

                            if (!Regex.IsMatch(returnValueRange(i, ArrayIndexColumns[7][j] + 4, out selectRange, (str) => { string temp = str; return temp.Trim(); }),
                                "[а-яА-ЯёЁ\\-0-9№(][а-яА-ЯёЁ\\-\\s',.0-9()№\"\\\\/]{1,499}"))
                                selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                        }
                    }
                    else
                        if (selectValue.Length > 0)
                        selectRange.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(255, 83, 83));
                }
                reportProgressProcess(i-6, countRow - 6);
                if (checkIsCancel(cancel, workbook)) 
                    return;
            }

            workbook.Save();
            workbook.Close();

            void reportProgressProcess(int currentProgress, int totalProgress)
            {
                GetProcessInformation processInformation = new GetProcessInformation("Поиск Ошибок", workbook.Name, 0, 0, currentProgress, totalProgress);
                processInfo?.Report(processInformation);
            }

            bool checkIsCancel(CancellationToken cancelToken, Excel.Workbook currentWorkBook)
            {
                if (cancelToken != null)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        currentWorkBook.Close();
                        return true;
                    }
                }
                return false;
            }

            string returnValueRange(int i, int j, out Excel.Range ReturnRange, ActionOfLine ExecuteDelegateForLine = null)
            {
                string ReturnStr;
                Excel.Range RangeValue = worksheet.Cells[i, j] as Excel.Range; 
                ReturnRange = RangeValue;
                try
                {
                    ReturnStr = RangeValue.Value2.ToString();
                }
                catch
                {
                    ReturnStr = "";
                }
                if (ExecuteDelegateForLine != null)
                {
                    ReturnStr = ExecuteDelegateForLine.Invoke(ReturnStr);
                    RangeValue.Value2 = ReturnStr;
                }
                return ReturnStr;
            }
        }

        private static bool checkSNILS(string value, out string result)
        {
            value = value.Replace("-", "");
            result = "";

            if (Regex.IsMatch(value, @"^\d{9,11}$"))
                value = value.PadLeft(11, '0');
            else
                return false;

            if (int.Parse(value.Substring(0, 9)) > 1001998)
            {
                int ControlNumber = 0;

                for (int i = 0; i < 9; i++)
                    ControlNumber += (9 - i) * int.Parse(value[i] + "");

                if (ControlNumber > 100) ControlNumber %= 101;
                if (ControlNumber == 100) ControlNumber = 0;

                if (ControlNumber == int.Parse(value.Substring(9, 2)))
                {
                    result = value;
                    return true;
                }
                else
                {
                    result = value.Substring(0, 9) + ControlNumber;
                    return false;
                }
            }
            else
                return false;

        }

        private static bool checkWordReg(string value, string reqVal, RegexOptions option, bool isBorderWord)
        {
            for (int i = 0; i < reqVal.Length; i++)
            {
                string Pattern = reqVal.Substring(0, i) + "." + reqVal.Substring(i + 1, reqVal.Length - (i + 1));
                if (isBorderWord) Pattern = "^" + Pattern + "$";
                if (Regex.IsMatch(value, Pattern, option))
                    return true;
            }
            return false;
        }

        private static int recountRow(Excel.Workbook workbook)
        {
            Excel.Worksheet sheet = workbook.Sheets[1];
            int Count = 6;
            while (true)
            {
                Count++;
                byte isEnd = 0;
                for (int i = 1; i <= 5; i++)
                {
                    Excel.Range forYach = sheet.Cells[Count, i] as Excel.Range;
                    try
                    {
                        string yach = forYach.Value2.ToString();
                        if (yach == "")
                            isEnd += 1;
                        else
                            break;
                    }
                    catch
                    {
                        isEnd += 1;
                    }
                }
                if (isEnd >= 5)
                {
                    Count--;
                    break;
                }
            }
            return Count;
        }

        private static Excel.Workbook OpenPatternBook()
        {
            try
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string path = Path.GetDirectoryName(location);

                return ExcelApp.Workbooks.Open($"{path}\\Шаблон.xlsx"); ;
            }
            catch
            {
                throw new Exception("Шаблон не найден!");
            }
        }
    }
}