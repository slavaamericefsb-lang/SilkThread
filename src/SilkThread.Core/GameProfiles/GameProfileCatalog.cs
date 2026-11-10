using System;
using System.Collections.Generic;

namespace SilkThread.Core.GameProfiles;

public static class GameProfileCatalog
{
    private static readonly List<GameProfile> _profiles = new()
    {
        // 1. Counter-Strike 2
        new GameProfile
        {
            Id = "cs2",
            Title = "Counter-Strike 2",
            ShortTitle = "CS2",
            Genre = "Тактический шутер",
            GenreCategory = GameGenreCategory.Shooter,
            EngineBadge = "Source 2",
            Launcher = "Steam",
            AccentColorHex = "#F59E0B",
            IconGlyph = "\uE7FC",
            Description = "Флагманский соревновательный тактический шутер Valve на движке Source 2 с субтиковой архитектурой.",
            ExecutableNames = new[] { "cs2.exe" },
            LaunchOptions = "-novid -fullscreen +fps_max 0 -nojoy +exec autoexec.cfg",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-novid", "Пропуск вступительного видео Valve / Source 2", "Мгновенная загрузка в главное меню без ожидания"),
                new LaunchArgItem("-fullscreen", "Принудительный эксклюзивный полный экран", "Снижает задержку дисплейного планировщика DWM до минимума"),
                new LaunchArgItem("+fps_max 0", "Снятие ограничения кадровой частоты", "Минимизирует задержку генерации кадров (frametime lag)"),
                new LaunchArgItem("-nojoy", "Отключение фонового опроса контроллеров", "Освобождает процессорный поток и исключает микростаттеры"),
                new LaunchArgItem("+exec autoexec.cfg", "Автовыполнение файла autoexec.cfg", "Гарантирует загрузку соревновательных биндов и сетевых рейтов")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Эксклюзивный полный экран", "Минимальная задержка вывода кадров на монитор"),
                new VideoSettingItem("Разрешение / Формат", "1920x1080 (или 1280x960 4:3 растянутый)", "Увеличенные пропорции моделей противников и высокий FPS"),
                new VideoSettingItem("Качество теней", "Низкое / Очень низкое", "Устраняет просадки при раскидках дымов и гранат"),
                new VideoSettingItem("Качество текстур и моделей", "Низкое / Среднее", "Четкая видимость контуров оперативников без лагов VRAM"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "Включено + Boost", "Максимальная частота GPU и минимальная задержка ввода"),
                new VideoSettingItem("Окружающая окклюзия (AO)", "Отключено", "Убирает затемнения в углах, делая противников ярче"),
                new VideoSettingItem("Вертикальная синхронизация", "Отключено", "Исключает буферизацию и инпутлаг при прицеливании"),
                new VideoSettingItem("Сглаживание (MSAA)", "CMAA2 или 2x MSAA", "Гладкие края без временного мыла TAA")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("autoexec.cfg", @"<Steam>\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg\autoexec.cfg", "Кастомные бинды, прицел, viewmodel и настройки тиков"),
                new ConfigFileInfo("cs2_video.txt", @"<Steam>\userdata\<AccountID>\730\local\cfg\cs2_video.txt", "Файл низкоуровневых параметров графического движка Source 2")
            }
        },

        // 2. Dota 2
        new GameProfile
        {
            Id = "dota2",
            Title = "Dota 2",
            ShortTitle = "Dota 2",
            Genre = "MOBA",
            GenreCategory = GameGenreCategory.Moba,
            EngineBadge = "Source 2",
            Launcher = "Steam",
            AccentColorHex = "#EF4444",
            IconGlyph = "\uE7FC",
            Description = "Культовая киберспортивная MOBA от Valve, оптимизированная для массовых замесов 5 на 5 без просадок 1% Low FPS.",
            ExecutableNames = new[] { "dota2.exe" },
            LaunchOptions = "-novid -high -map dota -nojoy -console +fps_max 0",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-novid", "Пропуск заставки Valve", "Быстрый вход в главное меню игры"),
                new LaunchArgItem("-high", "Высокий приоритет процесса в Windows", "Защита от фоновых прерываний и просадок FPS в командных драках"),
                new LaunchArgItem("-map dota", "Предзагрузка карты в RAM при запуске", "Полностью устраняет первый лаг/статтер при загрузке матча"),
                new LaunchArgItem("-nojoy", "Отключение подсистемы джойстиков", "Экономия оперативной памяти и снижение системных прерываний"),
                new LaunchArgItem("-console", "Включение консоли разработчика (~)", "Быстрый доступ к командам сети, net_graph и пинга"),
                new LaunchArgItem("+fps_max 0", "Разблокировка частоты кадров", "Плавность анимаций способностей на 144–360 Гц мониторах")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Эксклюзивный полный экран", "Снижение задержки отклика курсора мыши"),
                new VideoSettingItem("Разрешение", "Нативное (100% масштаб рендеринга)", "Четкое позиционирование крипов и героев"),
                new VideoSettingItem("Качество теней", "Выключено или Низкое", "Критический прирост 1% Low FPS во время ультимейтов"),
                new VideoSettingItem("Анимированные портреты", "Отключено", "Снижение нагрузки на UI-поток интерфейса HUD"),
                new VideoSettingItem("Сглаживание", "Отключено или FXAA", "Резкое изображение без размытия мелких деталей"),
                new VideoSettingItem("Вертикальная синхронизация", "Отключено", "Мгновенная реакция на клики при микроконтроле"),
                new VideoSettingItem("NVIDIA Reflex", "Включено + Boost", "Минимизация задержки клика до регистрации заклинания")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("autoexec.cfg", @"<Steam>\steamapps\common\dota 2 beta\game\dota\cfg\autoexec.cfg", "Быстрое применение способностей (Quickcast), бинды камеры и рейты"),
                new ConfigFileInfo("video.txt", @"<Steam>\steamapps\common\dota 2 beta\game\dota\cfg\video.txt", "Конфиг видеопараметров движка Source 2")
            }
        },

        // 3. Valorant
        new GameProfile
        {
            Id = "valorant",
            Title = "Valorant",
            ShortTitle = "Valorant",
            Genre = "Тактический шутер",
            GenreCategory = GameGenreCategory.Shooter,
            EngineBadge = "Riot Vanguard / UE4",
            Launcher = "Riot Games",
            AccentColorHex = "#FF4655",
            IconGlyph = "\uE7FC",
            Description = "Соревновательный тактический шутер Riot Games с античитом нулевого кольца Vanguard и жестким фокусом на время реакции.",
            ExecutableNames = new[] { "VALORANT-Win64-Shipping.exe", "VALORANT.exe" },
            LaunchOptions = "\"C:\\Riot Games\\Riot Client\\RiotClientServices.exe\" --launch-product=valorant --launch-patchline=live",
            LaunchArgs = new[]
            {
                new LaunchArgItem("--launch-product=valorant", "Прямой запуск Valorant через Riot Client", "Обход главного окна лаунчера и лишних фоновых страниц"),
                new LaunchArgItem("--launch-patchline=live", "Прямое подключение к стабильной ветке Live", "Быстрая аутентификация без зависания проверки обновлений"),
                new LaunchArgItem("DirectFlip Mode", "Аппаратный оверлей прямого сканирования", "Включается SilkThread в реестре, обходя ограничения Vanguard"),
                new LaunchArgItem("Disable FSO", "Отключение оптимизации во весь экран Windows", "Устраняет конфликт буферов обмена Windows 11 DWM")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Полный экран (Fullscreen)", "Исключительный доступ графического процессора к дисплею"),
                new VideoSettingItem("Разрешение", "Нативное (1920x1080)", "Valorant не растягивает 4:3 на хитбоксы, нативное дает лучшую четкость"),
                new VideoSettingItem("Многопоточный рендеринг", "Включено", "Обязательно для высокой загрузки всех ядер CPU"),
                new VideoSettingItem("Качество материалов и деталей", "Низкое", "Максимальная контрастность моделек агентов на фоне стен"),
                new VideoSettingItem("Виньетирование и искажения", "Отключено", "Устраняет темные пятна и искажения прицела"),
                new VideoSettingItem("V-Sync", "Отключено", "Убирает критический буферный инпутлаг"),
                new VideoSettingItem("Сглаживание", "MSAA 2x / 4x", "Четкие контуры агентов без мыла"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "Включено + Boost", "Удерживает частоту GPU на пике для моментального флика")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("RiotUserSettings.ini", @"%localappdata%\VALORANT\Saved\Config\<RiotID>\Windows\RiotUserSettings.ini", "Параметры пользовательского интерфейса, подсветка врагов и привязки")
            }
        },

        // 4. Apex Legends
        new GameProfile
        {
            Id = "apex",
            Title = "Apex Legends",
            ShortTitle = "Apex",
            Genre = "Battle Royale",
            GenreCategory = GameGenreCategory.BattleRoyale,
            EngineBadge = "Modified Source",
            Launcher = "Steam / EA App",
            AccentColorHex = "#E03A3E",
            IconGlyph = "\uE7FC",
            Description = "Динамичная соревновательная королевская битва Respawn с высокими требованиями к трекингу и стабильности 190+ FPS.",
            ExecutableNames = new[] { "r5apex.exe" },
            LaunchOptions = "-novid -high +fps_max 0 -forcenovsync -no-browser",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-novid", "Пропуск вступительных видеороликов", "Экономия времени при старте и рестарте игры"),
                new LaunchArgItem("-high", "Высокий приоритет планировщика Windows", "Стабилизация времени кадра во время перестрелок в дыму"),
                new LaunchArgItem("+fps_max 0", "Снятие ограничения частоты кадров", "Возможность играть на 240+ Гц (или фиксация +fps_max 190/240)"),
                new LaunchArgItem("-forcenovsync", "Принудительное выключение V-Sync в движке", "Устраняет скрытую синхронизацию кадров движка Source"),
                new LaunchArgItem("-no-browser", "Отключение встроенного Chromium браузера", "Экономия 200–400 МБ RAM и предотвращение фонового троттлинга")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Эксклюзивный полный экран", "Минимальная задержка вывода кадров"),
                new VideoSettingItem("Поле зрения (FOV)", "104 – 110", "Оптимальный периферический обзор и плавный трекинг"),
                new VideoSettingItem("V-Sync", "Отключено", "Устраняет задержку отклика прицела"),
                new VideoSettingItem("Сглаживание", "TSAA или Отключено", "Четкая видимость противников на дальних дистанциях"),
                new VideoSettingItem("Объемный свет (Volumetric)", "Отключено", "Убирает ослепляющие лучи солнца и туман"),
                new VideoSettingItem("Качество теней", "Низкое", "Максимальная видимость кемперов в затемненных зданиях"),
                new VideoSettingItem("NVIDIA Reflex", "Включено + Boost", "Критично для точного контроля отдачи оружия")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("videoconfig.txt", @"%USERPROFILE%\Saved Games\Respawn\Apex\local\videoconfig.txt", "Отключение теней и динамических частиц на уровне движка"),
                new ConfigFileInfo("autoexec.cfg", @"<Steam>\steamapps\common\Apex Legends\cfg\autoexec.cfg", "Кастомные бинды суперглайда, тап-стрейфа и оптимизация")
            }
        },

        // 5. Fortnite
        new GameProfile
        {
            Id = "fortnite",
            Title = "Fortnite",
            ShortTitle = "Fortnite",
            Genre = "Battle Royale",
            GenreCategory = GameGenreCategory.BattleRoyale,
            EngineBadge = "Unreal Engine 5.4",
            Launcher = "Epic Games",
            AccentColorHex = "#00B7FF",
            IconGlyph = "\uE7FC",
            Description = "Популярнейшая соревновательная игра на Unreal Engine 5.4 со строительством и скоростными бокс-файтами.",
            ExecutableNames = new[] { "FortniteClient-Win64-Shipping.exe" },
            LaunchOptions = "-useallavailablecores -nosplash -notexturestreaming -limitclientticks",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-useallavailablecores", "Использование всех доступных ядер CPU", "Максимальное параллельное распределение физики и отрисовки"),
                new LaunchArgItem("-nosplash", "Отключение загрузочной заставки", "Ускорение старта клиента"),
                new LaunchArgItem("-notexturestreaming", "Отключение стриминга текстур на лету", "Предотвращает падения FPS и подгрузку размытых текстур"),
                new LaunchArgItem("-limitclientticks", "Стабилизация тикрейта сетевого клиента", "Сглаживает сетевую задержку пакетов во время активных боев")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим рендеринга", "Режим производительности (Performance)", "Сверхвысокий FPS и упрощенная геометрия"),
                new VideoSettingItem("Режим отображения", "Эксклюзивный полный экран", "Минимальная задержка нажатия кнопок редактирования"),
                new VideoSettingItem("Разрешение 3D", "100%", "Резкая картинка без эффекта лесенок"),
                new VideoSettingItem("Дальность прорисовки", "Средняя или Дальняя", "Четкая видимость игроков на дальних расстояниях"),
                new VideoSettingItem("Текстуры", "Низкие", "С отключением High-Res Textures в Epic Games Launcher"),
                new VideoSettingItem("Сетки (Meshes)", "Низкие", "Прозрачные и легко читаемые деревянные и кирпичные постройки"),
                new VideoSettingItem("NVIDIA Reflex", "Включено + Boost", "Минимизация задержки редактирования стен и выстрела")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("GameUserSettings.ini", @"%localappdata%\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini", "Конфигурация рендерера Performance, отключение динамического масштабирования")
            }
        },

        // 6. Call of Duty: Warzone
        new GameProfile
        {
            Id = "warzone",
            Title = "Call of Duty: Warzone",
            ShortTitle = "Warzone",
            Genre = "Battle Royale / Шутер",
            GenreCategory = GameGenreCategory.BattleRoyale,
            EngineBadge = "IW 9.0 Engine",
            Launcher = "Battle.net / Steam",
            AccentColorHex = "#22C55E",
            IconGlyph = "\uE7FC",
            Description = "Масштабная королевская битва Call of Duty на движке IW 9.0 с реалистичной баллистикой и высокой нагрузкой на систему.",
            ExecutableNames = new[] { "cod.exe" },
            LaunchOptions = "-d3d11 -high -fullscreen +fps_max 0",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-d3d11", "Принудительный конвейер DirectX 11", "Повышает стабильность фреймтайма и снижает сбои драйвера"),
                new LaunchArgItem("-high", "Высокий приоритет диспетчера потоков", "Защищает от фризов при рендеринге огромного открытого мира"),
                new LaunchArgItem("-fullscreen", "Аппаратный полноэкранный режим", "Прямой рендеринг без наложения оконного менеджера"),
                new LaunchArgItem("+fps_max 0", "Снятие лимита кадровой частоты", "Максимальная частота обновления на киберспортивных мониторах")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Эксклюзивный полный экран", "Наименьший инпутлаг в перестрелках"),
                new VideoSettingItem("Стриминг текстур по требованию", "Отключено", "Критично: предотвращает лаги сети и внезапные микростаттеры"),
                new VideoSettingItem("Разрешение текстур", "Низкое или Нормальное", "Оптимизация заполнения видеопамяти VRAM"),
                new VideoSettingItem("Разрешение карты теней", "Очень низкое", "Значительный прирост частоты кадров"),
                new VideoSettingItem("Размытие в движении (Motion Blur)", "Выключено (Мир и Оружие)", "Чистая картинка при быстром повороте камеры"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "Включено + Boost", "Снижение задержки от нажатия курка до выстрела"),
                new VideoSettingItem("Глубина резкости (DOF)", "Выключено", "Четкое обнаружение снайперов на дистанции")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("options.4.cod23.cst", @"Documents\Call of Duty\players\options.4.cod23.cst", "Настройка RendererWorkerCount (потоки CPU), масштабирование VRAM и памяти")
            }
        },

        // 7. PUBG: Battlegrounds
        new GameProfile
        {
            Id = "pubg",
            Title = "PUBG: Battlegrounds",
            ShortTitle = "PUBG",
            Genre = "Battle Royale",
            GenreCategory = GameGenreCategory.BattleRoyale,
            EngineBadge = "Unreal Engine 4",
            Launcher = "Steam",
            AccentColorHex = "#F39C12",
            IconGlyph = "\uE7FC",
            Description = "Тактическая хардкорная королевская битва на движке UE4 с огромными картами 8х8 км и выраженной зависимостью от CPU и RAM.",
            ExecutableNames = new[] { "TslGame.exe" },
            LaunchOptions = "-USEALLAVAILABLECORES -malloc=system -high -refresh 240",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-USEALLAVAILABLECORES", "Использование всех ядер процессора", "Ускорение параллельных вычислений физики и анимаций"),
                new LaunchArgItem("-malloc=system", "Системный аллокатор памяти Windows", "Оптимизирует выделение блоков памяти и снижает статтеры"),
                new LaunchArgItem("-high", "Высокий приоритет CPU в Windows", "Приоритетная обработка кадров над фоновыми процессами"),
                new LaunchArgItem("-refresh 240", "Форсирование частоты монитора", "Гарантирует максимальную развертку герцовки дисплея")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Полный экран (Fullscreen)", "Исключение буферизации оконного режима"),
                new VideoSettingItem("Масштаб отображения", "100%", "Резкость пикселей без артефактов масштабирования"),
                new VideoSettingItem("Сглаживание", "Среднее", "Устранение лесенок без замыливания движущихся силуэтов"),
                new VideoSettingItem("Постобработка", "Очень низкое", "Отключение блума и слепящих эффектов света"),
                new VideoSettingItem("Тени", "Очень низкое", "Легкое обнаружение противников в тени домов и скал"),
                new VideoSettingItem("Листва (Foliage)", "Очень низкое", "Минимальная плотность травы на дальней дистанции"),
                new VideoSettingItem("Вертикальная синхронизация", "Отключено", "Мгновенная реакция на управление прицелом")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("GameUserSettings.ini", @"%localappdata%\TslGame\Saved\Config\WindowsNoEditor\GameUserSettings.ini", "Файл соревновательных настроек разрешения, графики и частоты кадров")
            }
        },

        // 8. Tom Clancy's Rainbow Six Siege
        new GameProfile
        {
            Id = "r6s",
            Title = "Tom Clancy's Rainbow Six Siege",
            ShortTitle = "R6 Siege",
            Genre = "Тактический шутер",
            GenreCategory = GameGenreCategory.Shooter,
            EngineBadge = "AnvilNext 2.0",
            Launcher = "Ubisoft Connect / Steam",
            AccentColorHex = "#38BDF8",
            IconGlyph = "\uE7FC",
            Description = "Тактический соревновательный шутер с разрушаемым окружением, требующий моментального времени реакции на пиках.",
            ExecutableNames = new[] { "RainbowSix.exe", "RainbowSix_Vulkan.exe" },
            LaunchOptions = "-high -vulkan -con_enable 1",
            LaunchArgs = new[]
            {
                new LaunchArgItem("-high", "Высокий приоритет процесса в Windows", "Стабильный фреймрейт при разрушении стен и взрывах"),
                new LaunchArgItem("-vulkan", "Запуск через современный API Vulkan", "Снижает задержку процессора при вызовах отрисовки"),
                new LaunchArgItem("-con_enable 1", "Включение консоли разработчика", "Возможность мониторинга тикрейта и параметров пинга")
            },
            VideoSettings = new[]
            {
                new VideoSettingItem("Режим отображения", "Эксклюзивный полный экран", "Снижение задержки дисплея до аппаратного минимума"),
                new VideoSettingItem("Формат экрана (Aspect Ratio)", "16:9 или 4:3 / 3:2", "Растягивание моделей оперативников для более легкого попадания"),
                new VideoSettingItem("Угол обзора (FOV)", "84 – 90", "Широкий периферический обзор и контроль узких щелей"),
                new VideoSettingItem("Качество текстур", "Среднее", "Баланс четкости скинов и нагрузки на видеопамять"),
                new VideoSettingItem("Качество детализации (LOD)", "Ультра", "Сохраняет четкие контуры голов оперативников на дальних пиках"),
                new VideoSettingItem("Отражения и эффекты линз", "Отключено", "Чистый экран без бликов и вспышек"),
                new VideoSettingItem("NVIDIA Reflex Low Latency", "Включено + Boost", "Мгновенная регистрация клика при префаере")
            },
            ConfigFiles = new[]
            {
                new ConfigFileInfo("GameSettings.ini", @"Documents\My Games\Rainbow Six - Siege\<UserID>\GameSettings.ini", "Настройки прямого ввода мыши (Raw Input), кастомный FOV и аудио")
            }
        }
    };

    public static IReadOnlyList<GameProfile> GetAllProfiles() => _profiles;

    public static GameProfile? GetProfileById(string id)
    {
        return _profiles.Find(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
