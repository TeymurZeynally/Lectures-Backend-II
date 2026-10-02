# Лекция 09 — Apache Airflow

Граф обработки видеоролика из лекции (DAG `video_processing`), описанный
кодом на Python, **UploadService** на C#, который запускает граф на каждую
загрузку ролика, и Airflow 3, развёрнутый в Docker из тех же компонентов, что
на схеме архитектуры: **API Server**, **DAG Processor**, три **Scheduler'а**
с LocalExecutor и **Metadata DB** в Postgres. Код графа лежит в
`dags/video_processing.py` — эта папка и есть DAG bundle.

## Граф

```
                     ┌─► extract_audio ─► recognize_speech ─► create_subtitles ─┐
                     │                                                          │
validate_format ─────┼─► transcode [1080p, 720p, 480p] ─► package_hls ──────────┼─► publish ─► notify_subscribers
                     │                                                          │
                     └─► extract_cover_frames ─► choose_cover ──────────────────┘
```

Полезная нагрузка — `print` в лог задачи и `time.sleep`. Каждая задача
печатает имя хоста, на котором выполняется, — так видно, какой Scheduler её
запустил. Перекодирование с вероятностью 30% падает.

## Концепции

- **Граф — это код** — `@dag` описывает граф, `@task` — вершину.
  Зависимости не перечисляются отдельно: они следуют из того, какой результат
  какой задаче передан. `create_subtitles(recognize_speech(extract_audio(video_id)))`
  — это цепочка из трёх вершин.
- **Передача данных (XCom)** — значение, которое вернула задача, сохраняется
  в базе и передаётся следующей. Поэтому задачи обмениваются только
  небольшими данными: путями к файлам, а не самими файлами.
- **Размножение задачи по списку (dynamic task mapping)** —
  `transcode.partial(video_id=...).expand(quality=[...])` создаёт три
  экземпляра одной вершины во время выполнения. `package_hls` получает список
  их результатов и начинается только после завершения всех трёх.
- **Параллельные ветки** — вершины, между которыми нет пути по рёбрам,
  выполняются одновременно: после `validate_format` сразу стартуют извлечение
  аудио, три перекодирования и извлечение кадров.
- **Повтор отдельной вершины** — `retries=3`, `retry_delay=5 секунд`: упавшее
  перекодирование перезапускается само, остальные вершины не трогаются.
  Любую вершину можно перезапустить и вручную.
- **Запуск по событию** — `schedule=None`: граф не запускается по расписанию,
  а только вручную или через REST API (как при появлении нового ролика).
  Параметры ролика передаются в `conf` и доступны задачам как `params`.
- **Компоненты** (`docker-compose.yml`):
  - **DAG Processor** читает `.py`-файлы из `dags/`, строит граф и сохраняет
    его структуру и исходный код в базу;
  - **Scheduler** по структуре из базы решает, какие задачи готовы, и
    выполняет их в дочерних процессах (LocalExecutor). Код задачи читает из
    `dags/`, статус сообщает через API Server;
  - **API Server** — веб-интерфейс и REST API;
  - **Metadata DB** — Postgres: графы, запуски, состояния задач, XCom.
- **Несколько Scheduler'ов** — три экземпляра (`replicas: 3`) работают одновременно и
  согласуются блокировками строк в базе: каждую задачу запускает один из них.
  Если Scheduler упал, его задачи перестают отправлять heartbeat, и через
  20 секунд (`TASK_INSTANCE_HEARTBEAT_TIMEOUT`) другой Scheduler запускает их
  заново.

## 1. Запуск Airflow

```bash
docker compose up -d
```

Первый старт занимает около минуты: `airflow-init` создаёт схему базы и
пользователя. Интерфейс: [http://localhost:8090](http://localhost:8090)
(логин `airflow` / `airflow`).

Metadata DB открыта наружу на `localhost:5433` (база `airflow`, логин
`airflow` / `airflow`, порт не 5432, чтобы не конфликтовать с Postgres
из примеров Quartz и Hangfire). Что там посмотреть:

```sql
select dag_id, run_id, state, conf from dag_run order by start_date desc limit 5;
select task_id, map_index, state, try_number, hostname from task_instance order by start_date desc limit 15;
select task_id, map_index, key, value from xcom order by timestamp desc limit 10;
select dag_id, source_code from dag_code;
```

## 2. Запуск графа

Граф не запускается сам: `schedule=None` — только по триггеру. Другие
варианты расписания (cron, интервал, по событию) перечислены комментарием в
`dags/video_processing.py`.

Через **UploadService** — та же страница, что в примере с Hangfire, но
каждая загрузка создаёт запуск графа через REST API Airflow:

```bash
cd Lecture09.Background.Airflow.UploadService
dotnet run
```

Страница: [http://localhost:5081](http://localhost:5081) — "Загрузить",
"Загрузить 10" и ссылка на запуск каждого
ролика в Airflow UI. Запуск называется `video-<id ролика>`, параметры ролика
передаются в `conf`.

Из интерфейса Airflow: `Dags` → `video_processing` → `Trigger`, в форме можно
поменять `video_id` и `title`.

Через REST API:

```bash
TOKEN=$(curl -s -X POST localhost:8090/auth/token -H 'Content-Type: application/json' \
  -d '{"username": "airflow", "password": "airflow"}' | python3 -c 'import sys, json; print(json.load(sys.stdin)["access_token"])')

curl -X POST localhost:8090/api/v2/dags/video_processing/dagRuns \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"logical_date": null, "conf": {"video_id": "3f2a9c1e", "title": "Путешествие на Байкал"}}'
```

Десять роликов сразу:

```bash
for i in $(seq 1 10); do
  curl -s -o /dev/null -X POST localhost:8090/api/v2/dags/video_processing/dagRuns \
    -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    -d "{\"logical_date\": null, \"conf\": {\"video_id\": \"video-$i\", \"title\": \"Обзор нового ноутбука\"}}"
done
```

## Остановка

```bash
docker compose down
```
