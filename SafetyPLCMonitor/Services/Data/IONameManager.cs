// SafetyPLCMonitor/Services/Data/IONameManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using SafetyPLCMonitor.Models;

namespace SafetyPLCMonitor.Services.Data
{
    public class IONameManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, string> _ioNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _ioDescriptions = new Dictionary<string, string>();
        private readonly string _ioNamesFilePath;

        public event EventHandler IONamesChanged;

        public IONameManager(string ioNamesFilePath = null)
        {
            _ioNamesFilePath = ioNamesFilePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SafetyPLCMonitor", "io_names.json");

            LoadIONames();
            Logger.Info($"I/O 이름 관리자 초기화: {_ioNames.Count}개 이름 로드됨");
        }

        public string GetIOName(IOType type, int address)
        {
            string key = GetIOKey(type, address);

            if (_ioNames.TryGetValue(key, out string name))
                return name;

            return GetDefaultIOName(type, address);
        }

        public string GetIODescription(IOType type, int address)
        {
            string key = GetIOKey(type, address);

            if (_ioDescriptions.TryGetValue(key, out string description))
                return description;

            return GetDefaultIODescription(type, address);
        }

        public void SetIOName(IOType type, int address, string name)
        {
            string key = GetIOKey(type, address);

            if (string.IsNullOrEmpty(name))
            {
                if (_ioNames.ContainsKey(key))
                {
                    _ioNames.Remove(key);
                }
            }
            else
            {
                _ioNames[key] = name;
            }

            SaveIONames();
            IONamesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetIODescription(IOType type, int address, string description)
        {
            string key = GetIOKey(type, address);

            if (string.IsNullOrEmpty(description))
            {
                if (_ioDescriptions.ContainsKey(key))
                {
                    _ioDescriptions.Remove(key);
                }
            }
            else
            {
                _ioDescriptions[key] = description;
            }

            SaveIONames();
            IONamesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ImportIONames(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<IONameData>(json);

                if (data != null)
                {
                    _ioNames.Clear();
                    _ioDescriptions.Clear();

                    if (data.Names != null)
                    {
                        foreach (var pair in data.Names)
                        {
                            _ioNames[pair.Key] = pair.Value;
                        }
                    }

                    if (data.Descriptions != null)
                    {
                        foreach (var pair in data.Descriptions)
                        {
                            _ioDescriptions[pair.Key] = pair.Value;
                        }
                    }

                    SaveIONames();
                    IONamesChanged?.Invoke(this, EventArgs.Empty);

                    Logger.Info($"I/O 이름 가져오기 성공: {_ioNames.Count}개 이름, {_ioDescriptions.Count}개 설명");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "I/O 이름 가져오기 오류");
            }
        }

        public void ExportIONames(string filePath)
        {
            try
            {
                var data = new IONameData
                {
                    Names = _ioNames,
                    Descriptions = _ioDescriptions
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Logger.Info($"I/O 이름 내보내기 성공: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "I/O 이름 내보내기 오류");
            }
        }

        private string GetIOKey(IOType type, int address)
        {
            return $"{type}:{address}";
        }

        private string GetDefaultIOName(IOType type, int address)
        {
            switch (type)
            {
                case IOType.DiscreteInput:
                    return $"Input {address}";
                case IOType.DiscreteOutput:
                    return $"Output {address}";
                case IOType.VirtualInput:
                    return $"VInput {address}";
                case IOType.VirtualOutput:
                    return $"VOutput {address}";
                case IOType.InputRegister:
                    return $"InReg {address}";
                case IOType.HoldingRegister:
                    return $"HoldReg {address}";
                default:
                    return $"IO {address}";
            }
        }

        private string GetDefaultIODescription(IOType type, int address)
        {
            switch (type)
            {
                case IOType.DiscreteInput:
                    return $"디스크릿 입력 {address}";
                case IOType.DiscreteOutput:
                    return $"디스크릿 출력 {address}";
                case IOType.VirtualInput:
                    return $"가상 입력 {address}";
                case IOType.VirtualOutput:
                    return $"가상 출력 {address}";
                case IOType.InputRegister:
                    return $"입력 레지스터 {address}";
                case IOType.HoldingRegister:
                    return $"보유 레지스터 {address}";
                default:
                    return $"I/O 포인트 {address}";
            }
        }

        private void LoadIONames()
        {
            try
            {
                if (File.Exists(_ioNamesFilePath))
                {
                    string json = File.ReadAllText(_ioNamesFilePath);
                    var data = JsonConvert.DeserializeObject<IONameData>(json);

                    if (data != null)
                    {
                        _ioNames.Clear();
                        _ioDescriptions.Clear();

                        if (data.Names != null)
                        {
                            foreach (var pair in data.Names)
                            {
                                _ioNames[pair.Key] = pair.Value;
                            }
                        }

                        if (data.Descriptions != null)
                        {
                            foreach (var pair in data.Descriptions)
                            {
                                _ioDescriptions[pair.Key] = pair.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "I/O 이름 로드 오류");
            }
        }

        private void SaveIONames()
        {
            try
            {
                string directory = Path.GetDirectoryName(_ioNamesFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new IONameData
                {
                    Names = _ioNames,
                    Descriptions = _ioDescriptions
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_ioNamesFilePath, json);

                Logger.Debug("I/O 이름 저장 완료");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "I/O 이름 저장 오류");
            }
        }
    }

    public class IONameData
    {
        public Dictionary<string, string> Names { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Descriptions { get; set; } = new Dictionary<string, string>();
    }
}