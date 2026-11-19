# awesome-event-service

Учебный ASP.NET Core Minimal API на .NET 10 для управления событиями. События хранятся в памяти и сбрасываются при перезапуске API.

## Структура проекта

- `src/EventApi` — проект API, точка входа `Program.cs`, настройки запуска и примеры HTTP-запросов.
- `src/EventApi/Events` — маршруты `/events`, сервис, модель события и результаты записи.
- `src/EventApi/Events/Contracts` — типы запросов и ответов API.
- `src/EventApi/Http` — форматирование HTTP-ошибок.

## Сборка и запуск в Docker

Для запуска проекта используется Docker. Инструкция по запуску, в зависимости от системы:

- **Windows.** Используется режим Linux-контейнеров и консоль PowerShell. [Docker Desktop для Windows](https://docs.docker.com/desktop/setup/install/windows-install/)
- **Ubuntu.** Запустить Docker Engine и открыть терминал Bash. Если для доступа к Docker Engine нужны права администратора, использовать `sudo docker` в командах ниже. [Docker Engine для Ubuntu](https://docs.docker.com/engine/install/ubuntu/)
- **macOS.** Использовать установщик для Intel или Apple Silicon и открыть терминал. [Docker Desktop для macOS](https://docs.docker.com/desktop/setup/install/mac-install/)

Перед запуском - перейти в корень репозитория и проверить командой `docker version`, что доступен сервер Docker с Linux. Все команды выполняется в PowerShell, Bash или Zsh (в зависимости от операционной системы). Используется образ из [конфигурации Dev Container](.devcontainer/devcontainer.json), подключает исходники в `/workspace` и публикует порт `5247` на хосте.

```sh
docker run --rm -it --mount "type=bind,source=$PWD,target=/workspace" --workdir /workspace --publish 127.0.0.1:5247:5247 --entrypoint bash mcr.microsoft.com/devcontainers/dotnet:1-10.0
```

В открывшейся оболочке контейнера выполнить команду для сборки проект и запуска API в окружении `Development`, в котором доступны Swagger UI и OpenAPI. `dotnet build` также восстановит зависимости NuGet.

```bash
dotnet build src/EventApi/EventApi.csproj
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/EventApi/EventApi.csproj --no-build --no-launch-profile --urls http://0.0.0.0:5247
```

При работе в VS Code, можно открыть проект командой **Dev Containers: Reopen in Container**, выполнить те же команды сборки и запуска во встроенном терминале и пробросить порт `5247` на вкладке **Ports**.

Чтобы остановить API, нажать `Ctrl+C`. Если контейнер запущен командой `docker run`, ввести в его оболочке `exit`.

## API

Базовый адрес — `http://localhost:5247`. Параметр `{id}` — идентификатор события в формате GUID. При создании API возвращает событие с новым `id` и указывает его адрес в заголовке `Location`.

| Метод и путь | Операция | Ответы |
| --- | --- | --- |
| `GET /events` | Получить список событий | `200 OK` |
| `POST /events` | Создать событие | `201 Created`, `400 Bad Request`, `415 Unsupported Media Type` |
| `GET /events/{id}` | Получить событие | `200 OK`, `404 Not Found` |
| `PUT /events/{id}` | Обновить событие | `200 OK`, `400 Bad Request`, `404 Not Found`, `415 Unsupported Media Type` |
| `DELETE /events/{id}` | Удалить событие | `204 No Content`, `404 Not Found` |

Для `POST` и `PUT` отправляется объект с заголовком `Content-Type: application/json`. Требуется указывать `title` хотя бы с одним непробельным символом, даты `startAt` и `endAt` с `Z` или смещением UTC и, при необходимости, `description`. Время окончания события должно быть позже времени начала. В ответах даты возвращаются в UTC. `PUT` целиком заменяет данные существующего события, сохраняя его `id`.

```json
{
  "title": "C# meetup",
  "description": "Встреча разработчиков C#",
  "startAt": "2026-12-01T18:00:00Z",
  "endAt": "2026-12-01T19:00:00Z"
}
```

## Ошибки API

API возвращает ошибки в формате Problem Details (`application/problem+json`). Поля `type`, `title` и `status` описывают ошибку, `instance` содержит путь запроса, а `traceId` помогает найти запрос в журналах.

- `400 Bad Request` возвращается при пустом теле, `null`, некорректном JSON, несовместимых типах полей и ошибках валидации. Для ошибок валидации `errors` содержит сообщения с ключами из JSON, например `title` и `endAt`.
- `404 Not Found` возвращается для неизвестного маршрута, идентификатора в неверном формате и отсутствующего события. Поле `detail` различает отсутствующий маршрут и отсутствующее событие. При `PUT` проверка запроса выполняется перед поиском события.
- `405 Method Not Allowed` возвращается для неподдерживаемого метода на известном маршруте. Заголовок `Allow` перечисляет доступные методы.
- `415 Unsupported Media Type` возвращается, если у непустого тела отсутствует заголовок `Content-Type`, указан неподдерживаемый тип или неизвестная кодировка.
- `500 Internal Server Error` возвращается при необработанном исключении. Подробности исключения записываются в журнал приложения.

## Проверка через Swagger UI

После запуска приложения отрыть [Swagger UI](http://localhost:5247/swagger) в браузере на хосте. На Windows, Ubuntu и macOS адрес и шаги проверки одинаковые. Описание маршрутов доступно в [документе OpenAPI](http://localhost:5247/openapi/v1.json).

1. Выбрать `GET /events`, нажать **Try it out**, затем **Execute**. После первого запуска ожидается `200 OK` и пустой массив `[]`.
2. В `POST /events` нажать **Try it out**, вставь JSON из примера и нажми **Execute**. Ожидается `201 Created`. Скопируй `id` из тела ответа.
3. В `GET /events/{id}` нажать **Try it out**, вставь `id` и выполни запрос. Ожидается `200 OK` с созданным событием.
4. В `PUT /events/{id}` использовать тот же `id` и пример JSON с изменённым `title`. Ожидается `200 OK` с обновлённым событием.
5. В `DELETE /events/{id}` использовать тот же `id`. Ожидается `204 No Content`. Повторить `GET /events/{id}` и проверить, что получен `404 Not Found`.
6. Повторить `POST /events` с телом `{}`. Ожидается `400 Bad Request` с ошибками обязательных полей.

Те же запросы и дополнительные примеры валидации есть в [EventApi.http](src/EventApi/EventApi.http).
