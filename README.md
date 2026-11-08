# SilkThread — Windows Gaming Optimizer

![Platform](https://img.shields.io/badge/Platform-Windows%2011%20x64-blue)
![Framework](https://img.shields.io/badge/.NET-10.0-purple)
![UI](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-violet)
![Architecture](https://img.shields.io/badge/Architecture-MVVM%20%2B%20Clean%20Modular-green)
![Tests](https://img.shields.io/badge/Tests-100%25%20Passing-brightgreen)

**SilkThread** — это современное высокопроизводительное нативное приложение для глубокой, интеллектуальной и безопасной оптимизации Windows 11 для игр.

---

## ⚡ Ключевые возможности

1. **Интеллектуальный сканер конфигурации ПК**:
   - **Процессор (CPU)**: определение модели, ядер, логических потоков, архитектуры, тактовой частоты, поддержки VBS (Core Isolation) и инструкций.
   - **Видеокарта (GPU)**: модель, производитель (NVIDIA, AMD, Intel), видеопамять (VRAM), версия драйвера, поддержка DirectX 12 и статус аппаратного планирования HAGS (Hardware-Accelerated GPU Scheduling).
   - **Оперативная память (RAM)**: общий и доступный объём, тип (DDR4 / DDR5), рабочая частота (МГц), количество и конфигурация модулей.
   - **Накопители (Storage)**: точные модели, типы носителей (NVMe / SSD / HDD), свободное и общее пространство, состояние TRIM.
   - **Операционная система (OS)**: редакция, номер сборки Windows 11, статус Game Mode, активная схема электропитания.
   - **Форм-фактор**: определение ноутбука / настольного ПК, наличие аккумулятора, состояние питания от сети.
   - **Фоновые процессы**: обнаружение запущенных игровых клиентов (Steam, CS2, Valorant, Riot, Epic Games, Battle.net) и ресурсоёмких фоновых программ.

2. **Библиотека из 165 реальных твиков (15 категорий)**:
   - **CPU Optimization** (11 твиков): отключение Power Throttling, анпаркинг ядер CPU, кванты Win32PrioritySeparation, расписание MMCSS, оптимизации Intel Speed Shift и AMD Ryzen Thread DPC.
   - **GPU Optimization** (11 твиков): HAGS, глобальный Variable Refresh Rate (VRR), профили NVIDIA PowerMizer и Low Latency Pre-Rendered Frames, AMD Anti-Lag и Shader Cache, оптимизация TDR watchdog.
   - **RAM Optimization** (11 твиков): блокировка ядра в ОЗУ (DisablePagingExecutive), отключение очистки Pagefile при завершении, оптимизация SVCHost Split Threshold, тюнинг кэша Prefetcher для NVMe.
   - **Gaming Performance** (11 твиков): Windows Game Mode, отключение Game DVR и фоновой видеозаписи, обход задержек оконного композитора (DirectFlip / FSE), отключение авто-приглушения звука.
   - **Windows Performance** (11 твиков): снижение задержки меню до 10 мс, отключение прозрачности и акриловых размытий, отключение фонового выполнения UWP-приложений, отключение P2P-раздачи обновлений.
   - **Input & Latency** (11 твиков): отключение акселерации мыши (чистый raw input 1:1), максимальная скорость повтора клавиатуры, отключение всплывающих окон StickyKeys / FilterKeys, отключение USB Selective Suspend.
   - **Network Optimization** (11 твиков): отключение алгоритма Нагла (TCP No Delay & instant ACK), отключение индекса троттлинга сети (NetworkThrottlingIndex = -1), снятие 20% QoS резерва, отключение отрицательного кэша DNS.
   - **Storage Optimization** (11 твиков): отключение создания коротких имён NTFS 8.3, отключение обновления меток времени последнего доступа (Last Access Update), активация TRIM, оптимизация размера дампа до 256KB minidump.
   - **Background Processes** (11 твиков): отключение службы телеметрии DiagTrack, отключение фонового предзапуска Microsoft Edge, отключение Windows Error Reporting, отключение виджетов Windows 11.
   - **Windows Services** (11 твиков): отключение Diagnostic Policy Service, Distributed Link Tracking, Remote Registry, Downloaded Maps Manager, Fax, Touch Keyboard на настольных ПК.
   - **Power Management** (11 твиков): активация схемы максимальной производительности, разблокировка скрытых параметров PCIe Link State Power Management (ASPM), отключение гибридной гибернации Fast Startup.
   - **Windows Explorer & UI** (11 твиков): отображение расширений файлов, скрытых файлов, запуск папок в отдельном процессе, открытие Проводника на «Этот компьютер», включение компактного режима, возврат классического контекстного меню.
   - **Security & Privacy** (11 твиков): отключение рекламного ID, истории активности, телеметрии рукописного ввода и ввода с клавиатуры, отключение датчика геолокации, ограничение телеметрии до уровня Security.
   - **System Maintenance** (11 твиков): запрет автоматической перезагрузки при вошедшем пользователе, запрет Windows Update на перезапись видеодрайверов, отключение ночных таймеров автоматического обслуживания.
   - **Advanced Tweaks** (11 твиков): тонкая настройка квантов приоритета Win32 (0x28), реальное время для DWM Window Manager, динамическое управление пулом сессий, оптимизация порога декоммита кучи.

3. **Интеллектуальная фильтрация несовместимых твиков**:
   - Автоматически скрывает твики NVIDIA, если в ПК установлена видеокарта AMD / Intel.
   - Скрывает твики SSD, если системный диск — механический HDD.
   - Скрывает агрессивные настольные твики на ноутбуках при работе от батареи.
   - Позволяет в окне инспектора просмотреть причину несовместимости любого твика.

4. **Система безопасности и индивидуального отката (Undo Rollback)**:
   - Интеграция с Windows System Restore API (`srclient.dll` и WMI): создание точек восстановления перед массовой оптимизацией.
   - **Точный индивидуальный откат**: перед внесением изменений SilkThread считывает и сохраняет в локальную базу SQLite исходное значение параметра.
   - При откате удаляет созданные ключи/значения, если их изначально не существовало, либо восстанавливает точный исходный тип и данные.
   - Поддерживает индивидуальный откат каждого твика в один клик, а также массовый откат всех применённых изменений.

5. **Премиальный нативный интерфейс (Minimal Dark UI)**:
   - WinUI 3, Windows App SDK и системный Mica Backdrop с акриловыми полупрозрачными панелями.
   - Минималистичная неоновая палитра **Silk Amethyst** (`#8B5CF6`) и глубокий тёмный фон (`#0D0D12`).
   - Модульный **Bento Grid** дашборд с адаптивными карточками телеметрии оборудования и состояния системы.
   - Технология **IOST (Input/Output System Tuning)** для максимального снижения системных задержек (DPC latency, USB polling, Win32 Priority Separation).
   - Наглядная оценка Gaming Optimization Index (0-100).
   - Мастер быстрой оптимизации с готовыми профилями («Safe Gaming Essentials», «Competitive Latency», «Maximum Performance»).
   - Живой терминальный лог выполнения операций в реальном времени.

---

## 🛠 Технологический стек

* **Язык**: C# 13 / .NET 10 (Target: `net10.0-windows10.0.26100.0`)
* **Интерфейс**: WinUI 3 (Windows App SDK)
* **Архитектура**: MVVM (CommunityToolkit.Mvvm)
* **Внедрение зависимостей**: Microsoft.Extensions.DependencyInjection
* **Локальная база данных**: SQLite (`Microsoft.Data.Sqlite`)
* **Системные операции**: Windows API, Registry API, ServiceController, WMI (`System.Management`), Power Management API
* **Тестирование**: xUnit (`SilkThread.Tests`)

---

## 📁 Структура проекта

```
SilkThread/
├── SilkThread.slnx
├── src/
│   ├── SilkThread.Core/               # Ядро бизнес-логики и системных операций
│   │   ├── Models/                    # Модели данных, HardwareProfile, TweakDefinition
│   │   ├── HardwareDetection/         # Интеллектуальный сканер конфигурации ПК
│   │   ├── CompatibilityEngine/       # Модуль проверки совместимости твиков
│   │   ├── TweakEngine/               # Исполнитель применения и отката твиков
│   │   ├── TweakDatabase/             # Каталог из 165 реальных твиков (15 категорий)
│   │   ├── BackupManager/             # Хранилище исходных состояний (SQLite)
│   │   ├── RestorePointManager/       # Управление точками восстановления Windows
│   │   └── SystemOperations/          # Безопасные операции с реестром, службами, питанием
│   │
│   └── SilkThread.App/                # Нативное приложение WinUI 3
│       ├── Services/                  # Навигация, настройки
│       ├── ViewModels/                # MVVM ViewModel для каждой страницы
│       ├── Views/                     # XAML страницы (Dashboard, Tweaks, Scanner и др.)
│       ├── Styles/                    # Фирменные стили и палитра SilkStyles
│       └── Converters/                # ValueConverters для XAML
│
├── tests/
│   └── SilkThread.Tests/              # Набор автоматических тестов xUnit
└── dist/
    └── SilkThread/                    # Скомпилированная Release-версия (Standalone WinUI 3)
```

---

## 🚀 Запуск и сборка

### Быстрый запуск приложения (1 клик):
Дважды кликните по файлу `Run-SilkThread.bat` или `SilkThread.cmd` в корневой папке проекта, либо запустите готовый бинарный файл:
```powershell
.\dist\SilkThread\SilkThread.App.exe
```

### Запуск тестов:
```powershell
dotnet test
```

### Запуск приложения через .NET CLI:
```powershell
dotnet run --project src/SilkThread.App/SilkThread.App.csproj
```

### Сборка готового автономного Release-пакета:
```powershell
dotnet publish src/SilkThread.App/SilkThread.App.csproj -c Release -r win-x64 --self-contained true -o dist/SilkThread
```

### Сборка единого инсталлятора (SilkThread_Setup.exe):
```powershell
.\scripts\publish-release.ps1 -BuildOnly
```
Скрипт автоматически собирает приложение, упаковывает полезную нагрузку и компилирует автономный однофайловый установщик `dist\SilkThread_Setup.exe`.

### 📦 Автообновления через GitHub Releases
Приложение отслеживает манифест `version.json` из репозитория GitHub. Каждый новый релиз с тегом `v*.*.*` автоматически собирается через GitHub Actions и прикрепляет файл `SilkThread_Setup.exe` к странице релиза.
