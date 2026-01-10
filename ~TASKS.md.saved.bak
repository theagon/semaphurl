# SemaphURL - Задачи для разработчика

Каждая задача - атомарная единица работы. Отмечайте `[x]` при выполнении.

---

## Phase 1: MVP Polish

### 1.1 Rule Templates (Шаблоны правил)

#### Модель и данные
- [ ] Создать класс `RuleTemplate` в `Models/RuleTemplate.cs`
  - Свойства: `Id`, `Name`, `Description`, `Rules[]`, `IconName`
- [ ] Создать `RuleTemplateService` в `Services/RuleTemplateService.cs`
- [ ] Добавить встроенные шаблоны:
  - [ ] Work Sites (корпоративные домены)
  - [ ] Social Media (twitter, facebook, instagram, linkedin)
  - [ ] YouTube (youtube.com, youtu.be)
  - [ ] Dev Localhost (localhost, 127.0.0.1, порты 3000-9999)
  - [ ] Documentation (docs.*, developer.*, *documentation*)

#### UI первого запуска
- [ ] Добавить поле `FirstRunCompleted` в `AppConfig`
- [ ] Создать `FirstRunWindow.xaml` и `FirstRunViewModel.cs`
- [ ] Дизайн: приветствие + список шаблонов с чекбоксами
- [ ] Логика применения выбранных шаблонов
- [ ] Показывать окно при `FirstRunCompleted == false`

#### Импорт шаблонов
- [ ] Добавить кнопку "Import Template" в MainWindow
- [ ] Создать диалог выбора шаблона `TemplatePickerDialog.xaml`
- [ ] Логика merge правил (избежать дубликатов)

---

### 1.2 Smart Rule Suggestions (Умные предложения)

#### Анализ истории
- [ ] Добавить метод `GetDomainStatistics()` в `UrlHistoryService`
  - Группировка по домену, подсчёт за последние 7 дней
- [ ] Создать `RuleSuggestionService` в `Services/RuleSuggestionService.cs`
- [ ] Логика: домен > 5 раз за неделю + нет правила → кандидат

#### Уведомления
- [ ] Интегрировать с toast notifications (WPF-UI Snackbar)
- [ ] Текст: "Создать правило для {domain}?"
- [ ] Кнопка "Создать" → открывает диалог с предзаполненным паттерном
- [ ] Кнопка "Игнорировать" → добавить домен в ignore list
- [ ] Добавить `IgnoredSuggestionDomains` в `AppConfig`

---

### 1.3 Registration Wizard (Мастер регистрации)

#### Окно мастера
- [ ] Создать `RegistrationWizardWindow.xaml`
- [ ] Создать `RegistrationWizardViewModel.cs`
- [ ] Реализовать navigation между шагами (TabControl без заголовков)

#### Шаг 1: Введение
- [ ] Текст объяснения что делает приложение
- [ ] Иконка/иллюстрация

#### Шаг 2: Регистрация
- [ ] Кнопка "Зарегистрировать как браузер"
- [ ] Индикатор статуса (зарегистрирован/нет)
- [ ] Обработка ошибок (нет прав администратора)

#### Шаг 3: Инструкция Windows Settings
- [ ] Статичное изображение или GIF с инструкцией
- [ ] Кнопка "Открыть Windows Settings" (ms-settings:defaultapps)
- [ ] Текстовая инструкция

#### Шаг 4: Тест
- [ ] Кнопка "Проверить" → открыть тестовый URL
- [ ] Проверка результата (сообщение успеха)
- [ ] Кнопка "Готово"

#### Интеграция
- [ ] Показывать wizard если не зарегистрирован при запуске
- [ ] Добавить пункт меню "Registration Wizard" в настройках

---

### 1.4 Удаление Clipboard auto-monitoring ✅

- [x] Удалить автоматический мониторинг в `ClipboardService`
- [x] Оставить только метод для ручного получения URL
- [x] Убрать toast при копировании URL
- [x] Удалить настройку `ShowNotifications` для clipboard из `AppConfig`
- [x] Обновить UI настроек (убрать переключатель)
- [x] Проверить что hotkey `Ctrl+Shift+Space` работает

---

### 1.5 Упрощение URL History ✅

- [x] Изменить retention с 30 на 7 дней в `UrlHistoryService`
- [x] Добавить "Recent URLs" (топ-5) в контекстное меню трея
  - [x] Обновить tray menu в `App.xaml` + `TrayContextMenu_Opened` handler
  - [x] При клике → открыть URL в браузере по правилам
- [x] Оставить кнопку "Create Rule from Domain" в `UrlHistoryWindow`

---

### 1.6 Hotkey Configuration UI

#### HotkeyRecorderControl
- [ ] Создать `Controls/HotkeyRecorderControl.xaml`
- [ ] Создать `Controls/HotkeyRecorderControl.xaml.cs`
- [ ] Отображение текущей комбинации
- [ ] Режим записи: "Press your shortcut..."
- [ ] Захват клавиш (KeyDown event)
- [ ] Поддержка модификаторов (Ctrl, Alt, Shift, Win)

#### Интеграция
- [ ] Заменить TextBox на HotkeyRecorderControl в настройках
- [ ] Валидация: проверка конфликтов с системными hotkeys
- [ ] Список известных системных комбинаций
- [ ] Кнопка "Reset to Default"

---

### 1.7 Спрятать Browser Arguments

- [ ] Перенести `BrowserArgumentsTemplate` в expandable секцию
- [ ] Использовать Expander с заголовком "Advanced"
- [ ] Добавить dropdown с presets:
  - Default (пусто)
  - Incognito/Private
  - New Window
- [ ] Маппинг preset → аргументы для разных браузеров
- [ ] При выборе preset → заполнять текстовое поле

---

## Phase 2: Power User Features

### 2.1 Rule Groups

#### Модель
- [ ] Создать `RuleGroup` в `Models/RuleGroup.cs`
  - Свойства: `Id`, `Name`, `Color`, `Order`, `IsExpanded`
- [ ] Добавить `RuleGroups` список в `AppConfig`
- [ ] Добавить `GroupId` (nullable Guid) в `RoutingRule`

#### UI списка правил
- [ ] Переделать ItemsControl на TreeView или grouped ListView
- [ ] Collapsible группы
- [ ] Цветовая полоска слева для каждой группы
- [ ] Группа "Ungrouped" для правил без группы

#### Управление группами
- [ ] Диалог создания/редактирования группы
- [ ] Выбор цвета (ColorPicker)
- [ ] Drag-and-drop правил между группами
- [ ] Контекстное меню: Enable All / Disable All

---

### 2.2 Rule Statistics

- [ ] Добавить `MatchCount` (int) в `RoutingRule`
- [ ] Добавить `LastMatchedAt` (DateTime?) в `RoutingRule`
- [ ] Инкрементировать в `RoutingService.Route()` при совпадении
- [ ] Сохранять статистику при выходе
- [ ] UI: отображать "Matched 15 times, last: 2h ago"
- [ ] Опция сортировки по частоте использования

---

### 2.3 Profiles (Профили)

#### Модель
- [ ] Создать `Profile` в `Models/Profile.cs`
  - Свойства: `Id`, `Name`, `Rules[]`, `DefaultBrowserPath`, `IconName`
- [ ] Добавить `Profiles` список в `AppConfig`
- [ ] Добавить `ActiveProfileId` в `AppConfig`

#### UI управления профилями
- [ ] Dropdown выбора профиля в MainWindow
- [ ] Диалог создания/редактирования профиля
- [ ] Копирование правил между профилями
- [ ] Предустановленные: "Work", "Personal", "Development"

#### Интеграция
- [ ] Иконка в трее показывает активный профиль
- [ ] Hotkey для переключения профиля
- [ ] При переключении → перезагрузить правила в RoutingService

---

### 2.4 Browser-specific Presets

#### Определение типа браузера
- [ ] Добавить `BrowserType` enum (Chrome, Firefox, Edge, Brave, Opera, Unknown)
- [ ] Метод определения типа по пути exe
- [ ] Добавить `BrowserType` в `BrowserInfo`

#### Presets аргументов
- [ ] Создать `BrowserPresets` static class
- [ ] Chrome presets: `--incognito`, `--new-window`, `--profile-directory`
- [ ] Firefox presets: `-private-window`, `-P "ProfileName"`
- [ ] Edge presets: `-inprivate`, `--profile-directory`
- [ ] UI: dropdown с presets в настройках правила

---

### 2.5 Browser Profile Detection

- [ ] Chrome: читать профили из `%LOCALAPPDATA%\Google\Chrome\User Data`
- [ ] Firefox: парсить `profiles.ini`
- [ ] Edge: читать профили из `%LOCALAPPDATA%\Microsoft\Edge\User Data`
- [ ] Добавить `BrowserProfile` модель (Name, DirectoryName)
- [ ] UI: выбор профиля браузера в правиле
- [ ] Автоподстановка аргумента `--profile-directory`

---

### 2.6 Usage Dashboard (Статистика)

#### Окно статистики
- [ ] Создать `StatisticsWindow.xaml`
- [ ] Создать `StatisticsViewModel.cs`

#### Метрики
- [ ] URLs routed: today / this week / this month
- [ ] Top 10 доменов (таблица)
- [ ] Distribution by browser (pie chart)
- [ ] Most triggered rules (топ-5)

#### Графики
- [ ] Добавить NuGet: LiveCharts2 или OxyPlot
- [ ] График URLs по дням за последнюю неделю
- [ ] Pie chart распределения по браузерам

---

## Phase 3: Advanced Features

### 3.1 URL Rewriting

- [ ] Создать `RedirectRule` модель или расширить `RoutingRule`
- [ ] Добавить поля: `RewritePattern`, `RewriteReplacement`
- [ ] Поддержка regex groups ($1, $2, etc.)
- [ ] UI: переключатель "Rewrite URL before opening"
- [ ] Примеры шаблонов: AMP removal, twitter→nitter

---

### 3.2 URL Preview Mode

- [ ] Добавить настройку `EnableUrlPreview` в `AppConfig`
- [ ] Создать `UrlPreviewWindow.xaml` (popup)
- [ ] Отображать: URL, matched rule, target browser
- [ ] Кнопки: Open, Copy URL, Edit URL, Cancel
- [ ] Интеграция: Shift+click → показать preview

---

### 3.3 Improved Import/Export

- [ ] Export selected rules only (с чекбоксами)
- [ ] Import dialog с preview правил
- [ ] Merge strategy: Skip duplicates / Overwrite / Rename
- [ ] Добавить `FormatVersion` в export JSON

---

### 3.4 Command Line Interface

- [ ] `--test-url "url"` - показать какое правило сработает
- [ ] `--open "url" --browser "name"` - открыть в указанном браузере
- [ ] `--export rules.json` - экспорт правил
- [ ] `--import rules.json` - импорт правил
- [ ] `--list-browsers` - список браузеров
- [ ] Парсинг аргументов в `App.xaml.cs` или `Program.cs`

---

### 3.5 Favorite Sites Groups

- [ ] Добавить `Group` поле в `FavoriteSite`
- [ ] Tabs или визуальные разделители в `FavoriteSitesWindow`
- [ ] Drag-and-drop между группами
- [ ] Поиск/фильтр в избранном

---

## Технический долг

### Unit Tests
- [ ] Тесты для `RoutingService.Route()`
- [ ] Тесты для `ConfigurationService` (load/save)
- [ ] Тесты для `RuleSuggestionService` (когда будет создан)
- [ ] Integration tests для browser launching (mock Process)

### Code Quality
- [ ] Рефакторинг `MainViewModel` - вынести логику в отдельные VM
- [ ] Улучшить error handling (централизованный обработчик)
- [ ] Добавить structured logging (Serilog)
- [ ] XML документация для public API сервисов

---

## Быстрый старт для разработчика

```powershell
# Клонировать и собрать
git clone <repo>
cd semaphurl
dotnet build src/SemaphURL.csproj

# Запустить тесты
dotnet test tests/SemaphURL.Tests/SemaphURL.Tests.csproj

# Запустить приложение
dotnet run --project src/SemaphURL.csproj
```

## Приоритет выполнения

1. **Критический (Phase 1)**: 1.4, 1.5, 1.6, 1.7 → 1.3 → 1.1, 1.2
2. **Высокий (Phase 2)**: 2.2, 2.4 → 2.1 → 2.3, 2.5, 2.6
3. **Средний (Phase 3)**: по желанию
4. **Технический долг**: параллельно с фичами
