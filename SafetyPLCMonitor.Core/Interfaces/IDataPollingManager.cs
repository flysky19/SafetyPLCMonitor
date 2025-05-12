using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyPLCMonitor.Core.Events;
using SafetyPLCMonitor.Core.Models;

namespace SafetyPLCMonitor.Core.Interfaces
{
    public interface IDataPollingManager : IDisposable
    {
        /// <summary>
        /// 폴링 태스크 실행 완료 이벤트
        /// </summary>
        event EventHandler<PollingTaskEventArgs> TaskExecuted;  // 이 줄 추가

        /// <summary>
        /// 폴링 활성화 여부
        /// </summary>
        bool IsPollingActive { get; }

        /// <summary>
        /// 현재 폴링 태스크 목록
        /// </summary>
        IReadOnlyList<PollingTask> PollingTasks { get; }

        /// <summary>
        /// 폴링 시작 또는 재개
        /// </summary>
        Task StartPollingAsync();

        /// <summary>
        /// 폴링 일시 중지
        /// </summary>
        void PausePolling();

        /// <summary>
        /// 폴링 태스크 추가
        /// </summary>
        /// <param name="task">추가할 폴링 태스크</param>
        void AddPollingTask(PollingTask task);

        /// <summary>
        /// 폴링 태스크 제거
        /// </summary>
        /// <param name="taskId">제거할 태스크 ID</param>
        /// <returns>제거 성공 여부</returns>
        bool RemovePollingTask(string taskId);

        /// <summary>
        /// 모든 폴링 태스크 제거
        /// </summary>
        void ClearPollingTasks();

        /// <summary>
        /// 특정 태스크 즉시 실행
        /// </summary>
        /// <param name="taskId">실행할 태스크 ID</param>
        /// <returns>실행 성공 여부</returns>
        Task<bool> ExecuteTaskImmediatelyAsync(string taskId);
    }
}