using System;
using System.Collections.Generic;
using SilkThread.App.Services;
using SilkThread.Core.Execution;
using SilkThread.Core.Models;
using SilkThread.Core.RestorePointManager;
using Xunit;

namespace SilkThread.Tests;

public class ExecutionAuditAndRestorePointsTests
{
    [Fact]
    public void TweakExecutionService_LogsRegistryModificationCorrectly()
    {
        string? logged = null;
        Action<string> handler = msg => logged = msg;
        TweakExecutionService.Instance.OperationLogged += handler;

        try
        {
            TweakExecutionService.Instance.LogRegistry(
                "HKLM",
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                "TcpAckFrequency",
                1,
                "DWORD"
            );

            Assert.NotNull(logged);
            Assert.Equal(@"[REGISTRY] Set HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters -> TcpAckFrequency = 1 (DWORD)", logged);
        }
        finally
        {
            TweakExecutionService.Instance.OperationLogged -= handler;
        }
    }

    [Fact]
    public void TweakExecutionService_LogsServiceAlterationCorrectly()
    {
        string? logged = null;
        Action<string> handler = msg => logged = msg;
        TweakExecutionService.Instance.OperationLogged += handler;

        try
        {
            TweakExecutionService.Instance.LogService("DiagTrack", "Disabled", "Stopped");

            Assert.NotNull(logged);
            Assert.Equal("[SERVICE] DiagTrack -> Set StartupType = Disabled, State = Stopped", logged);
        }
        finally
        {
            TweakExecutionService.Instance.OperationLogged -= handler;
        }
    }

    [Fact]
    public void TweakExecutionService_LogsPowerShellExecutionCorrectly()
    {
        string? logged = null;
        Action<string> handler = msg => logged = msg;
        TweakExecutionService.Instance.OperationLogged += handler;

        try
        {
            TweakExecutionService.Instance.LogPowerShell("Set-ProcessMitigation -System -Disable CFG", 0);

            Assert.NotNull(logged);
            Assert.Equal("[POWERSHELL] Set-ProcessMitigation -System -Disable CFG -> ExitCode: 0", logged);
        }
        finally
        {
            TweakExecutionService.Instance.OperationLogged -= handler;
        }
    }

    [Fact]
    public void SettingsService_LastOptimizationProfileIndex_PersistsAndDefaultsToZero()
    {
        var settings = new AppSettingsModel();
        Assert.Equal(0, settings.LastOptimizationProfileIndex);

        settings.LastOptimizationProfileIndex = 2;
        Assert.Equal(2, settings.LastOptimizationProfileIndex);
    }

    [Theory]
    [InlineData("SilkThread Batch Optimization (Safe)", null, null, "Перед оптимизацией SilkThread")]
    [InlineData("Before Game Tweaks", "0", "100", "Установка ПО")]
    [InlineData("System Update", "1", "100", "Удаление ПО")]
    [InlineData("Nvidia Graphics Driver Update", "10", "100", "Установка драйвера")]
    [InlineData("Windows Settings Customization", "12", "100", "Изменение параметров")]
    [InlineData("User Checkpoint", null, null, "Создано пользователем")]
    public void RestorePointManager_ResolvesHumanReadableTypeCorrectly(
        string description,
        string? rawType,
        string? eventType,
        string expectedType)
    {
        var result = RestorePointManager.ResolveHumanReadableType(rawType, eventType, description);
        Assert.Equal(expectedType, result);
    }

    [Fact]
    public void RestorePointManager_ParsesWmiCimDateTimeCorrectly()
    {
        // 2026-11-10 20:45:30 in CIM datetime format
        string cimString = "20261110204530.000000+180";
        var dt = RestorePointManager.ParseCreationTime(cimString);

        Assert.Equal(2026, dt.Year);
        Assert.Equal(11, dt.Month);
        Assert.Equal(10, dt.Day);
        Assert.Equal(20, dt.Hour);
        Assert.Equal(45, dt.Minute);
        Assert.Equal(30, dt.Second);

        var item = new RestorePointItem(42, "SilkThread Point", dt, "Перед оптимизацией SilkThread");
        Assert.Equal("10.11.2026 20:45:30", item.FormattedCreationTime);
        Assert.Equal("Перед оптимизацией SilkThread", item.EventTypeString);
    }
}
