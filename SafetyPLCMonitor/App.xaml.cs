using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using SafetyPLCMonitor.Services.Data;
using SafetyPLCMonitor.Core.Interfaces;
using SafetyPLCMonitor.Communication;

namespace SafetyPLCMonitor
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // 의존성 주입을 위한 서비스 제공자
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 로그 디렉토리 생성
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // NLog 설정 초기화
            InitializeLogging();

            // 의존성 주입 설정
            ConfigureServices();

            // 전역 예외 처리
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Logger.Fatal(ex, "처리되지 않은 예외 발생");
                MessageBox.Show($"치명적인 오류가 발생했습니다: {ex?.Message}\n\n애플리케이션이 종료됩니다.",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            this.DispatcherUnhandledException += (s, args) =>
            {
                Logger.Error(args.Exception, "처리되지 않은 UI 예외 발생");
                args.Handled = true;
                MessageBox.Show($"오류가 발생했습니다: {args.Exception.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // 시작 로그
            Logger.Info("===== 애플리케이션 시작 =====");
            Logger.Info($"버전: {GetType().Assembly.GetName().Version}");
            Logger.Info($"위치: {AppDomain.CurrentDomain.BaseDirectory}");
        }

        private void ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            // 서비스 등록
            // 싱글톤으로 등록 (애플리케이션 수명 동안 하나의 인스턴스만 유지)
            // Modbus 클라이언트 (IP와 포트는 나중에 설정)
            serviceCollection.AddSingleton<IModbusClient>(provider =>
                new ModbusTcpClient("127.0.0.1", 502));

            // 폴링 매니저
            serviceCollection.AddSingleton<IDataPollingManager>(provider =>
                new DataPollingManager(provider.GetRequiredService<IModbusClient>()));

            // I/O 이름 관리자
            serviceCollection.AddSingleton<IONameManager>();

            // PLC 정보 관리자
            serviceCollection.AddSingleton<PlcInfoManager>(provider =>
                new PlcInfoManager(provider.GetRequiredService<IModbusClient>()));

            // 서비스 제공자 설정
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void InitializeLogging()
        {
            // 초기 NLog 설정 파일 로드 확인
            string nlogConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config");
            if (!File.Exists(nlogConfigPath))
            {
                // 설정 파일이 없는 경우 기본 설정 생성
                CreateDefaultNLogConfig();
            }
        }

        // 기본 NLog 설정 생성
        private void CreateDefaultNLogConfig()
        {
            var config = new LoggingConfiguration();

            // 파일 로그 타겟
            var fileTarget = new FileTarget("file")
            {
                FileName = "${basedir}/logs/${shortdate}.log",
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${threadid:padding=-3} | ${logger:shortName=true:padding=-30} | ${message}${onexception:${newline}${exception:format=tostring}}"
            };
            config.AddTarget(fileTarget);

            // 콘솔 로그 타겟
            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${message}"
            };
            config.AddTarget(consoleTarget);

            // 에러 로그 타겟
            var errorFileTarget = new FileTarget("errorFile")
            {
                FileName = "${basedir}/logs/errors_${shortdate}.log",
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${threadid:padding=-3} | ${logger:shortName=true:padding=-30} | ${message}${newline}${exception:format=tostring}"
            };
            config.AddTarget(errorFileTarget);

            // 통신 로그 타겟
            var commFileTarget = new FileTarget("communicationFile")
            {
                FileName = "${basedir}/logs/communication_${shortdate}.log",
                Layout = "${longdate} | ${threadid:padding=-3} | ${message}"
            };
            config.AddTarget(commFileTarget);

            // 메모리 타겟 (UI 표시용)
            var memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${longdate} | ${level:uppercase=true:padding=-5} | ${message}"
            };
            config.AddTarget(memoryTarget);

            // 규칙 추가
            config.AddRule(LogLevel.Trace, LogLevel.Debug, commFileTarget, "SafetyPLCMonitor.Communication.*");
            config.AddRule(LogLevel.Error, LogLevel.Fatal, errorFileTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, memoryTarget);

            // 설정 적용
            LogManager.Configuration = config;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("===== 애플리케이션 종료 =====");
            LogManager.Shutdown();

            base.OnExit(e);
        }
    }
}