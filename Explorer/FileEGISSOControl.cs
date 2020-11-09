using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace EGISSOEditor
{
    static class FileEGISSOControl
    {
        public static Action StartAProcessEvent;
        public static Action EndAProcessEvent;

        public static List<FileEGISSO> FilesEGISSO = new List<FileEGISSO>();
        private static List<FileEGISSO> selectFilesEGISSO = new List<FileEGISSO>();

        public static void Init()
        {
            try
            {
                EGISSO.Init();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void AddFiles (string filePath, out string newFilePath)
        {
            newFilePath = "";
            if (File.Exists(filePath))
            {
                string directoryProgramm = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string newFileDirectory = directoryProgramm + "\\temp";
                string fileName = Path.GetFileName(filePath);

                if (filePath.IndexOf(directoryProgramm) != -1)
                    throw new Exception($"Ошибка добавления файла!");

                foreach (FileEGISSO file in FilesEGISSO)
                {
                    if (filePath == file.PrimaryDirectory) 
                        throw new Exception($"Файл {filePath} уже добавлен!"); 
                    else if (fileName == file.FileName)
                        throw new Exception($"Файл c именем {fileName} уже добавлен!");
                }

                newFilePath = newFileDirectory + "\\" + fileName;
                
                if (!Directory.Exists(directoryProgramm+"\\temp"))
                    Directory.CreateDirectory(directoryProgramm + "\\temp");
                if (File.Exists(newFileDirectory))
                    try
                    {
                        File.Delete(newFileDirectory);
                    }
                    catch {
                        throw new Exception("Ошибка добавления файла!");
                    }

                FilesEGISSO.Add(new FileEGISSO(filePath, newFilePath));
                File.Copy(filePath, newFilePath);
            }
            else
                throw new Exception($"Файл {filePath} не существует!"); 
        }

        public async static Task ProcessFilesAsync(ActionOnFile action, IProgress<GetProcessInformation> processInfo, CancellationToken cancel)
        {
            Progress<FileEGISSO> hasProcessFile = new Progress<FileEGISSO>((file)=>
            { 
                int index = GetIndexFileFromPath(file.TempDirectory, true);
                FilesEGISSO[index].isChangeFile = true;
            });

            await Task.Run(()=> StartAProcessEvent?.Invoke());
            try
            {
                await EGISSO.ProcessFilesAsync(selectFilesEGISSO, action, processInfo, hasProcessFile, cancel);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                EndAProcessEvent?.Invoke();
            }
        }

        public async static Task CombineFileAsync(string newFileName, IProgress<GetProcessInformation> processInfo, CancellationToken cancel)
        {
            StartAProcessEvent?.Invoke();
            try
            {
                await EGISSO.CombineFilesAsync(selectFilesEGISSO, newFileName, processInfo, cancel);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                EndAProcessEvent?.Invoke();
            }
        }

        public static void AddSelectFile(string file)
        {
            int index = GetIndexFileFromPath(file, true);
            if (index == -1)
            {
                throw new Exception($"Файл {file} не добавлен!");
            }
            else if (!File.Exists(file))
            {
                throw new Exception($"Файл {file} не существует!");
            }

            for (int i = 0; i < selectFilesEGISSO.Count; i++)
            {
                if (file == selectFilesEGISSO[i].TempDirectory)
                {
                    throw new Exception($"Файл {file} уже добавлен!");
                }
            }

            selectFilesEGISSO.Add(FilesEGISSO[index]);
        }

        public static void RemoveSelectFile(string file)
        {
            for (int i = 0; i < selectFilesEGISSO.Count; i++)
            {
                if (selectFilesEGISSO[i].TempDirectory == file)
                {
                    selectFilesEGISSO.RemoveAt(i);
                    break;
                }
            }   
        }

        public static void ClearSelectFile()
        {
            selectFilesEGISSO.Clear();
        }

        public static void Save(string fileName)
        {
            try
            {
                Save(GetIndexFileFromPath(fileName, true));
            }
            catch (Exception ex) 
            { 
                throw new Exception(ex.Message); 
            }
        }

        public static void Save(int index)
        {
            if (index >= 0 && index < FilesEGISSO.Count)
            {
                if (File.Exists(FilesEGISSO[index].PrimaryDirectory))
                {
                    try
                    {
                        using (var fs = File.Open(FilesEGISSO[index].PrimaryDirectory, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    }
                    catch
                    {
                        throw new Exception($"Файл {FilesEGISSO[index].PrimaryDirectory} запущен другим процессом!");
                    }
                }
                if (File.Exists(FilesEGISSO[index].TempDirectory))
                {
                    try
                    {
                        using (var fs = File.Open(FilesEGISSO[index].TempDirectory, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                        
                        File.Delete(FilesEGISSO[index].PrimaryDirectory);
                        File.Copy(FilesEGISSO[index].TempDirectory, FilesEGISSO[index].PrimaryDirectory);
                        FilesEGISSO[index].isChangeFile = false;
                    }
                    catch
                    {
                        throw new Exception($"Файл {FilesEGISSO[index].TempDirectory} запущен другим процессом!");
                    }
                }
                else
                    throw new Exception("Файл удален!");
            }
            else
                throw new Exception("Файл не найден!");
        }

        public static void SaveAs(string fileName, string newFileName)
        {
            try
            {
                SaveAs(GetIndexFileFromPath(fileName, true), newFileName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void SaveAs(int index, string newFileName)
        {
            if (index >= 0 && index < FilesEGISSO.Count)
            {
                if (File.Exists(FilesEGISSO[index].TempDirectory))
                {
                    try
                    {
                        using (var fs = File.Open(FilesEGISSO[index].TempDirectory, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                        File.Copy(FilesEGISSO[index].TempDirectory, newFileName);
                        FilesEGISSO[index].PrimaryDirectory = newFileName;
                        FilesEGISSO[index].isChangeFile = false;
                    }
                    catch
                    {
                        throw new Exception($"Файл {FilesEGISSO[index].TempDirectory} запущен другим процессом!");
                    }
                }
                else
                    throw new Exception("Файл удален!");
            }
            else
                throw new Exception("Файл не найден!");
        }

        public static void Close(string fileName)
        {
            try
            {
                Close(GetIndexFileFromPath(fileName, true));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }

        public static void Close(int index)
        {
            if (index >= 0 && index < FilesEGISSO.Count)
            {
                if (File.Exists(FilesEGISSO[index].TempDirectory))
                {
                    try
                    {
                        using (var fs = File.Open(FilesEGISSO[index].TempDirectory, FileMode.Open, FileAccess.Read, FileShare.None)){ }
                           
                        File.Delete(FilesEGISSO[index].TempDirectory);
                    }
                    catch
                    {
                        throw new Exception($"Файл {FilesEGISSO[index].TempDirectory} запущен другим процессом!");
                    }
                }
                FilesEGISSO.RemoveAt(index);
            }
            else
                throw new Exception("Файл не найден!");
        }

        public static bool GetInfоFileIsSave(string fileName)
        {
            int index = GetIndexFileFromPath(fileName, true);
            if (index != -1)
                return FilesEGISSO[index].isChangeFile;
            return false;
        }

        private static int GetIndexFileFromPath(string fileName, bool isCurrentPath = false)
        {
            int indexCloseFile = -1;
            for (int i = 0; i < FilesEGISSO.Count; i++)
            {
                if (fileName == FilesEGISSO[i].TempDirectory)
                {
                    indexCloseFile = i;
                    break;
                }
            }
            return indexCloseFile;
        } 
    }
}
