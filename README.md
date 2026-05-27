# Задание 1. Анализ и планирование

### 1. Описание функциональности монолитного приложения

**Управление отоплением:**

- Пользователи могут удалённо включать/выключать отопление в доме
- Система поддерживает CRUD-операции с датчиками отопления через REST API (создание, просмотр, обновление, удаление)
- Для каждого датчика можно менять рабочий статус и текущее значение через отдельный endpoint обновления

**Мониторинг температуры:**

- Пользователи могут просматривать текущие показания по всем датчикам или по конкретному датчику
- Система поддерживает получение температуры по локации и по идентификатору датчика через внешний temperature-api
- При запросе списка/карточки температурных датчиков значения обновляются данными из внешнего сервиса в реальном времени

### 2. Анализ архитектуры монолитного приложения

Текущее приложение реализовано как монолитный backend-сервис на Go с HTTP API на Gin.

Основные особенности архитектуры:

- Язык и фреймворк: Go + Gin, формат взаимодействия с клиентами и интеграциями — синхронный REST/JSON.
- Хранение данных: PostgreSQL, доступ к данным через pgx/pgxpool, основная сущность в БД — таблица sensors.
- Структура кода: слоистый монолит с разделением на packages `handlers` (HTTP), `db` (работа с БД), `services` (интеграция с внешним API), `models` (доменные структуры).
- Внешние зависимости: отдельный temperature-api, из которого приложение получает актуальные значения температуры по location или sensor id.
- Логика взаимодействия компонентов: HTTP-запрос -> handler -> слой БД/внешний сервис -> формирование JSON-ответа.
- Развёртывание: приложение контейнеризовано (Dockerfile), для локального запуска используется docker-compose c сервисами приложения, PostgreSQL и temperature-api.
- Конфигурация: через переменные окружения (`DATABASE_URL`, `TEMPERATURE_API_URL`, `PORT`) с дефолтными значениями.
- Масштабирование: сервис можно горизонтально масштабировать на уровне контейнеров (несколько экземпляров API за балансировщиком), так как он в целом stateless.
- Ограничения масштабирования: остаются общие узкие места в PostgreSQL и внешнем temperature-api, а также отсутствуют кэширование и асинхронная обработка.
- Эксплуатационные характеристики: есть endpoint health-check и graceful shutdown, но отсутствуют автоматические тесты и расширенная наблюдаемость (метрики/трассировка).

### 3. Определение доменов и границы контекстов

По факту текущей реализации строгого разделения на bounded contexts нет: это один монолитный сервис и один общий слой данных.

Корректнее говорить о логических поддоменах:

**1. Управление датчиками (основной поддомен)**

- Что включает: CRUD по сущности Sensor (name, type, location, unit, status, value).
- Где реализовано: handlers + db + models внутри одного приложения.
- Граница: данные и операции хранятся в одной таблице sensors в PostgreSQL.

**2. Температурная телеметрия (поддерживающий поддомен)**

- Что включает: получение актуальной температуры из внешнего temperature-api по location или sensor id.
- Где реализовано: service-интеграция и вызовы из HTTP-обработчиков.
- Граница: источник данных внешний, но оркестрация выполняется внутри того же монолита.

**Важно про границы контекстов в текущем коде**

- В реализации есть смешение логики: обработчики сенсоров одновременно работают и с мастер-данными датчиков, и с внешней телеметрией.
- Отдельных автономных контекстов (с независимыми моделями, контрактами и деплоем) нет.

### **4. Проблемы монолитного решения**

- Смешение ответственности в обработчиках: в одном endpoint совмещены операции с мастер-данными датчиков и интеграция с внешней температурной телеметрией.
- Отсутствие технической изоляции контекстов: управление датчиками и температурная телеметрия используют общий кодовый артефакт и общий релизный цикл.
- Ограниченная масштабируемость по доменам: нельзя независимо масштабировать только слой интеграции с temperature-api или только CRUD-часть.
- Общие точки отказа: PostgreSQL и temperature-api влияют на доступность API, при деградации внешнего сервиса страдают пользовательские сценарии чтения.
- Нет автоматических тестов, что повышает риск регрессий при изменениях и усложняет безопасную декомпозицию монолита.
- Недостаточная наблюдаемость: отсутствуют метрики, трассировка и явный мониторинг цепочки вызовов между внутренней логикой и внешним API.

### 5. Визуализация контекста системы — диаграмма С4

[Warmhouse Context Diagram](apps/diagrams/context/Warmhouse_As_Is_Context.puml)

# Задание 2. Проектирование микросервисной архитектуры

**Диаграмма контейнеров (Containers)**

[Warmhouse Container Diagram](apps/diagrams/container/Warmhouse_Containers.puml)

**Диаграмма компонентов (Components)**

- [IAM Service Components](apps/diagrams/components/IAM_Service_Components.puml)
- [Module Catalog Service Components](apps/diagrams/components/Module_Catalog_Service_Components.puml)
- [Device Service Components](apps/diagrams/components/Device_Service_Components.puml)
- [Control Service Components](apps/diagrams/components/Control_Service_Components.puml)
- [Telemetry Service Components](apps/diagrams/components/Telemetry_Service_Components.puml)
- [Scenario Service Components](apps/diagrams/components/Scenario_Service_Components.puml)
- [Integration Hub Components](apps/diagrams/components/Integration_Hub_Components.puml)
- [Video Service Components](apps/diagrams/components/Video_Service_Components.puml)
- [Notification Service Components](apps/diagrams/components/Notification_Service_Components.puml)
- [Billing Service Components](apps/diagrams/components/Billing_Service_Components.puml)

**Диаграмма кода (Code)**

- [Device Service Code Diagram](apps/diagrams/code/Device_Service_Code.puml)
- [Control Service Code Diagram](apps/diagrams/code/Control_Service_Code.puml)
- [Scenario Service Code Diagram](apps/diagrams/code/Scenario_Service_Code.puml)

# Задание 3. Разработка ER-диаграммы

[Warmhouse ER Diagram](apps/diagrams/er/Warmhouse_ER.puml)

# Задание 4. Создание и документирование API

### 1. Тип API

Для межсервисного взаимодействия используется гибридный подход:

- Синхронный API: REST/JSON по HTTPS для запросов, где нужен быстрый ответ (проверка прав, чтение digital twin, создание команды).
- Асинхронный обмен: события через Event Bus для телеметрии, сценариев и статусов команд.

Почему это подходит для текущего ландшафта:

- REST упрощает контракты между сервисами и проверку через OpenAPI.
- Событийная модель снижает связность и лучше масштабируется для high-volume телеметрии.
- Комбинация закрывает оба класса задач: request/response и event-driven автоматизация.

### 2. Документация API

OpenAPI-контракт межсервисных endpoint'ов:

- [Internal Service Contracts (OpenAPI)](apps/docs/openapi/internal-service-contracts.yaml)

Спроектированные межсервисные endpoint'ы (5 шт.):

1. `POST /internal/v1/access/check` (IAM Service)

- Назначение: проверка прав пользователя/сервиса в пределах tenant.
- Request: `tenant_id`, `principal_id`, `principal_type`, `action`, `resource`, `required_roles[]`.
- Response: `allow`, `effective_roles[]`, `reason`.

2. `GET /internal/v1/devices/{deviceId}/twin` (Device Service)

- Назначение: получение digital twin устройства для сервисов Control/Scenario.
- Request: path `deviceId`, header `X-Tenant-Id`.
- Response: `status`, `capabilities[]`, `desired_state`, `reported_state`, `updated_at`.

3. `POST /internal/v1/commands` (Control Service)

- Назначение: создание команды на устройство.
- Request: header `X-Tenant-Id`, `Idempotency-Key`; body `device_id`, `action`, `payload`, `requested_by`, `source`.
- Response (`202`): `command_id`, `status`, `accepted_at`.

4. `GET /internal/v1/devices/{deviceId}/telemetry/latest` (Telemetry Service)

- Назначение: получение последних телеметрических значений по устройству.
- Request: path `deviceId`, header `X-Tenant-Id`, optional query `metric`.
- Response: список `metrics[]` с полями `name`, `value`, `unit`, `observed_at`.

5. `POST /internal/v1/scenarios/evaluate-trigger` (Scenario Service)

- Назначение: оценка входящего события и генерация списка действий.
- Request: header `X-Tenant-Id`; body `event_id`, `event_type`, `device_id`, `occurred_at`, `payload`.
- Response: `matched_scenarios[]`, `planned_actions[]`.

Общие правила контракта:

- Все internal endpoint'ы и tenant-aware требуют `X-Tenant-Id`.
- Для мутаций используется idempotency (`Idempotency-Key`).
- Ошибки возвращаются в едином формате: `code`, `message`, `correlation_id`.

# Задание 5. Работа с docker и docker-compose

Перейдите в apps.

Там находится приложение-монолит для работы с датчиками температуры. В README.md описано как запустить решение.

Вам нужно:

1. сделать простое приложение temperature-api на любом удобном для вас языке программирования, которое при запросе /temperature?location= будет отдавать рандомное значение температуры.

Locations - название комнаты, sensorId - идентификатор названия комнаты

```
	// If no location is provided, use a default based on sensor ID
	if location == "" {
		switch sensorID {
		case "1":
			location = "Living Room"
		case "2":
			location = "Bedroom"
		case "3":
			location = "Kitchen"
		default:
			location = "Unknown"
		}
	}

	// If no sensor ID is provided, generate one based on location
	if sensorID == "" {
		switch location {
		case "Living Room":
			sensorID = "1"
		case "Bedroom":
			sensorID = "2"
		case "Kitchen":
			sensorID = "3"
		default:
			sensorID = "0"
		}
	}
```

2. Приложение следует упаковать в Docker и добавить в docker-compose. Порт по умолчанию должен быть 8081

3. Кроме того для smart_home приложения требуется база данных - добавьте в docker-compose файл настройки для запуска postgres с указанием скрипта инициализации ./smart_home/init.sql

Для проверки можно использовать Postman коллекцию smarthome-api.postman_collection.json и вызвать:

- Create Sensor
- Get All Sensors

Должно при каждом вызове отображаться разное значение температуры

Ревьюер будет проверять точно так же.
