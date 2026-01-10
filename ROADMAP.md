# SemaphURL Development Roadmap

Этот файл содержит план развития проекта. Отмечайте выполненные задачи `[x]`.

---

## Phase 1: MVP Polish (Приоритет: Критический)

Цель: Превратить технический прототип в user-friendly продукт.

### 1.1 Onboarding & Quick Setup

- [ ] **Rule Templates** - встроенные шаблоны правил
  - [ ] Создать модель `RuleTemplate` с предустановленными правилами
  - [ ] Добавить окно первого запуска `FirstRunWindow.xaml`
  - [ ] Шаблоны: Work Sites, Social Media, YouTube, Dev Localhost, Documentation
  - [ ] Кнопка "Import Template" в основном окне настроек
  - [ ] Сохранять флаг `FirstRunCompleted` в config

- [ ] **Smart Rule Suggestions** - предложения создать правило
  - [ ] Анализ истории: группировка по домену + подсчёт
  - [ ] Логика: если домен открыт >5 раз за неделю и нет правила → предложить
  - [ ] Toast notification: "Создать правило для {domain}?"
  - [ ] Кнопка в notification → открывает диалог создания правила с предзаполненным паттерном

### 1.2 Browser Registration Improvement

- [ ] **Registration Wizard** - пошаговый мастер регистрации
  - [ ] Создать `RegistrationWizardWindow.xaml`
  - [ ] Шаг 1: Объяснение что делает приложение
  - [ ] Шаг 2: Кнопка регистрации + проверка результата
  - [ ] Шаг 3: Инструкция по выбору в Windows Settings (со скриншотом или GIF)
  - [ ] Шаг 4: Тест - открыть тестовый URL и проверить результат
  - [ ] Показывать wizard если приложение не зарегистрировано при запуске

### 1.3 Удаление/Упрощение лишнего

- [ ] **Убрать Clipboard auto-monitoring**
  - [ ] Удалить автоматический мониторинг буфера обмена
  - [ ] Оставить только hotkey `Ctrl+Shift+Space` для ручного открытия URL из clipboard
  - [ ] Убрать toast при каждом копировании URL
  - [ ] Удалить настройку `ShowNotifications` для clipboard (оставить только для routing)

- [ ] **Упростить URL History**
  - [ ] Уменьшить хранение с 30 дней до 7 дней
  - [ ] Добавить "Recent URLs" (топ-5) в контекстное меню трея
  - [ ] Оставить функцию "Create Rule from Domain" в истории

### 1.4 UX Improvements

- [ ] **Улучшить Hotkey Configuration**
  - [ ] Заменить текстовое поле на UI записи комбинации ("Press your shortcut...")
  - [ ] Создать `HotkeyRecorderControl.xaml` (user control)
  - [ ] Валидация: проверка конфликтов с системными hotkeys
  - [ ] Кнопка "Reset to Default"

- [ ] **Спрятать Browser Arguments**
  - [ ] Перенести `BrowserArgumentsTemplate` в expandable "Advanced" секцию
  - [ ] Добавить preset dropdown: "Default", "Incognito/Private", "New Window"
  - [ ] Автоподстановка правильных аргументов по выбранному browser

---

## Phase 2: Power User Features (Приоритет: Высокий)

Цель: Сделать приложение незаменимым для активных пользователей.

### 2.1 Rule Organization

- [ ] **Rule Groups / Folders**
  - [ ] Добавить модель `RuleGroup` (Id, Name, Color, Order)
  - [ ] Связь `RoutingRule.GroupId` (nullable)
  - [ ] UI: collapsible groups в списке правил
  - [ ] Drag-and-drop правил между группами
  - [ ] Bulk enable/disable по группе
  - [ ] Цветовая индикация группы (цветная полоска слева)

- [ ] **Rule Statistics**
  - [ ] Добавить поля в `RoutingRule`: `MatchCount`, `LastMatchedAt`
  - [ ] Отображать в UI: "Matched 15 times, last: 2h ago"
  - [ ] Сортировка правил по частоте использования

### 2.2 Profiles / Contexts

- [ ] **Multiple Profiles**
  - [ ] Создать модель `Profile` (Id, Name, Rules[], DefaultBrowser, IsActive)
  - [ ] UI переключения профилей в главном окне
  - [ ] Hotkey для быстрого переключения профиля
  - [ ] Иконка в трее показывает активный профиль
  - [ ] Предустановленные профили: "Work", "Personal", "Development"

- [ ] **Scheduled Profiles** (опционально)
  - [ ] Автопереключение по времени (Work: 9:00-18:00)
  - [ ] UI настройки расписания

### 2.3 Browser Integration

- [ ] **Browser-specific Presets**
  - [ ] Определение типа браузера по пути (Chrome, Firefox, Edge, etc.)
  - [ ] Presets для каждого браузера:
    - Chrome: `--incognito`, `--new-window`, `--profile-directory="Profile N"`
    - Firefox: `-private-window`, `-P "ProfileName"`
    - Edge: `-inprivate`, `--profile-directory`
  - [ ] UI: dropdown с presets вместо ручного ввода аргументов

- [ ] **Browser Profile Detection**
  - [ ] Чтение профилей Chrome из `%LOCALAPPDATA%\Google\Chrome\User Data`
  - [ ] Чтение профилей Firefox из `profiles.ini`
  - [ ] Выбор профиля браузера в правиле

### 2.4 Statistics & Analytics

- [ ] **Usage Dashboard**
  - [ ] Создать `StatisticsWindow.xaml`
  - [ ] Метрики: URLs routed today/week/month
  - [ ] Top 10 domains
  - [ ] Distribution by browser (pie chart)
  - [ ] Most triggered rules
  - [ ] Простые графики (можно LiveCharts или OxyPlot)

---

## Phase 3: Advanced Features (Приоритет: Средний)

Цель: Расширить возможности для power users.

### 3.1 URL Manipulation

- [ ] **URL Rewriting / Redirects**
  - [ ] Новый тип правила: `RedirectRule`
  - [ ] Pattern + Replacement (regex groups support)
  - [ ] Use cases: AMP removal, privacy redirects, legacy domain mapping
  - [ ] Примеры: `amp.google.com` → `google.com`, `twitter.com` → `nitter.net`

- [ ] **URL Preview Mode**
  - [ ] Модификатор Shift при клике → показать preview popup
  - [ ] Показать: URL, matched rule, target browser
  - [ ] Кнопки: Open, Copy URL, Edit URL, Cancel

### 3.2 Sync & Export

- [ ] **Improved Import/Export**
  - [ ] Export selected rules only
  - [ ] Import with merge strategy (skip duplicates, overwrite, rename)
  - [ ] Версионирование формата экспорта

- [ ] **Cloud Sync** (опционально)
  - [ ] Watch config file в cloud folder (OneDrive, Dropbox)
  - [ ] Конфликт резолюшн при изменениях с разных машин
  - [ ] Альтернатива: sync через GitHub Gist

### 3.3 CLI & Automation

- [ ] **Command Line Interface**
  - [ ] `--test-url "url"` - показать какое правило сработает
  - [ ] `--open "url" --browser "name"` - открыть URL в указанном браузере
  - [ ] `--export rules.json` - экспорт правил
  - [ ] `--import rules.json` - импорт правил
  - [ ] `--list-browsers` - список обнаруженных браузеров

### 3.4 Favorite Sites Enhancement

- [ ] **Favorite Sites Groups**
  - [ ] Группировка избранного: Work, Personal, Dev, etc.
  - [ ] Tabs или folders в окне Favorite Sites
  - [ ] Поиск/фильтр в избранном

- [ ] **App Launcher Extension** (опционально)
  - [ ] Запуск не только URLs, но и приложений
  - [ ] Интеграция с Windows Search

---

## Backlog (Низкий приоритет / Идеи)

- [ ] Темы оформления (custom accent colors)
- [ ] Portable mode (config рядом с exe)
- [ ] Локализация (English, другие языки)
- [ ] Интеграция с Firefox Multi-Account Containers
- [ ] Правила по времени суток (ночью → другой браузер)
- [ ] Интеграция с Windows Focus Assist
- [ ] Телеметрия для улучшения продукта (opt-in)
- [ ] Auto-update mechanism
- [ ] Community rule marketplace

---

## Технический долг

- [ ] Покрыть unit-тестами основные сервисы (RoutingService, ConfigurationService)
- [ ] Integration tests для browser launching
- [ ] Улучшить error handling и логирование
- [ ] Рефакторинг MainViewModel (разбить на части)
- [ ] Документация API сервисов

---

## Заметки для разработчика

### Архитектура
- Все сервисы регистрируются как singletons в `App.xaml.cs`
- ViewModels используют `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`)
- Конфигурация в `%APPDATA%\SemaphURL\config.json`
- Паттерн-матчинг вынесен в `PatternMatcher.cs` для тестируемости

### Стиль кода
- Использовать nullable reference types
- Async/await для I/O операций
- Интерфейсы для всех сервисов (для DI и тестирования)
- WPF-UI компоненты для консистентного Fluent Design

### Тестирование
```powershell
dotnet test tests/SemaphURL.Tests/SemaphURL.Tests.csproj
```
